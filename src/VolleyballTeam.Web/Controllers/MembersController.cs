using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.Email;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubAdmin)]
[Route("{club:clubslug}/admin/users")]
public class MembersController(
    TenantContext tenant,
    MemberService members,
    InvitationService invitations,
    TeamService teams,
    AppEmails emails) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new MembersPageViewModel(await invitations.GetPendingAsync(cancellationToken)));

    [HttpPost("data")]
    public async Task<IActionResult> Data(
        int draw,
        int start,
        int length,
        [FromForm(Name = "search[value]")] string? search,
        [FromForm(Name = "order[0][name]")] string? orderBy,
        [FromForm(Name = "order[0][dir]")] string? orderDirection,
        CancellationToken cancellationToken)
    {
        var page = await members.QueryAsync(
            search, orderBy, string.Equals(orderDirection, "desc", StringComparison.OrdinalIgnoreCase), start, length, cancellationToken);

        return Json(new
        {
            draw,
            recordsTotal = page.Total,
            recordsFiltered = page.Filtered,
            data = page.Rows.Select(r => new
            {
                r.UserId,
                r.Name,
                r.Email,
                role = DisplayRole(r.Role, r.CurrentTeams.Length > 0),
                status = r.IsActive ? "Active" : "Deactivated",
                r.CurrentTeams,
                url = Url.Action(nameof(Show), new { club = ClubSlug, userId = r.UserId }),
            }),
        });
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> Show(int userId, CancellationToken cancellationToken)
    {
        var member = await members.GetAsync(userId, cancellationToken);
        return member is null
            ? NotFound()
            : View(new MemberPageViewModel(member, member.UserId == User.GetUserId()));
    }

    [HttpPost("{userId:int}/role")]
    public async Task<IActionResult> ChangeRole(int userId, ClubRole role, CancellationToken cancellationToken)
    {
        try
        {
            await members.ChangeRoleAsync(User.GetUserId(), userId, role, cancellationToken);
            Flash(role == ClubRole.Admin ? "They are now an admin." : "Admin access removed.");
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Show), new { club = ClubSlug, userId });
    }

    [HttpPost("{userId:int}/deactivate")]
    public Task<IActionResult> Deactivate(int userId, CancellationToken cancellationToken) =>
        SetActiveAsync(userId, false, "Member deactivated. They can no longer open this club.", cancellationToken);

    [HttpPost("{userId:int}/reactivate")]
    public Task<IActionResult> Reactivate(int userId, CancellationToken cancellationToken) =>
        SetActiveAsync(userId, true, "Member reactivated.", cancellationToken);

    [HttpGet("invite")]
    public async Task<IActionResult> Invite(CancellationToken cancellationToken) =>
        View(await InvitePageAsync(new InviteFormViewModel(), cancellationToken));

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([Bind(Prefix = nameof(InvitePageViewModel.Form))] InviteFormViewModel form, CancellationToken cancellationToken)
    {
        if (form.NeedsTeam && form.TeamSeasonId is null)
        {
            ModelState.AddModelError("Form.TeamSeasonId", "Choose which team.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var teamSeasonId = form.NeedsTeam ? form.TeamSeasonId : null;
                var request = new InviteRequest(form.Email, form.FirstName, form.LastName, form.ClubRole, teamSeasonId, form.StaffRole);
                var result = await invitations.CreateAsync(request, cancellationToken);
                await ReportInviteAsync(result, form.ClubRole, form.StaffRole, cancellationToken);
                return RedirectToAction(nameof(Index), new { club = ClubSlug });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(await InvitePageAsync(form, cancellationToken));
    }

    [HttpPost("invitations/{invitationId:int}/resend")]
    public async Task<IActionResult> Resend(int invitationId, CancellationToken cancellationToken)
    {
        try
        {
            var pending = (await invitations.GetPendingAsync(cancellationToken)).SingleOrDefault(i => i.Id == invitationId)
                ?? throw new BusinessRuleException("That invitation wasn't found.");
            var result = await invitations.RenewAsync(invitationId, cancellationToken);
            await ReportInviteAsync(result, pending.ClubRole, pending.StaffRole, cancellationToken);
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { club = ClubSlug });
    }

    [HttpPost("invitations/{invitationId:int}/revoke")]
    public async Task<IActionResult> Revoke(int invitationId, CancellationToken cancellationToken)
    {
        try
        {
            await invitations.RevokeAsync(invitationId, cancellationToken);
            Flash("Invitation cancelled. Its link no longer works.");
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { club = ClubSlug });
    }

    private async Task ReportInviteAsync(InviteResult result, ClubRole clubRole, StaffRole? staffRole, CancellationToken cancellationToken)
    {
        if (result.Outcome == InviteOutcome.AddedExistingMember)
        {
            Flash($"{result.Email} is already a member, so they were added to {result.TeamName} straight away.");
            return;
        }

        var acceptUrl = Url.Action(nameof(JoinController.Index), "Join", new { club = ClubSlug, token = result.Token }, Request.Scheme)!;
        var sent = await emails.SendInvitationAsync(
            CurrentClub, result.Email, await members.GetDisplayNameAsync(User.GetUserId(), cancellationToken), clubRole, result.TeamName, staffRole, acceptUrl, cancellationToken);

        if (sent)
        {
            Flash($"Invitation sent to {result.Email}.");
        }
        else
        {
            FlashUnsentInvitation(result.Email, acceptUrl);
        }
    }

    private async Task<IActionResult> SetActiveAsync(int userId, bool active, string message, CancellationToken cancellationToken)
    {
        try
        {
            await members.SetActiveAsync(User.GetUserId(), userId, active, cancellationToken);
            Flash(message);
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Show), new { club = ClubSlug, userId });
    }

    internal static string DisplayRole(ClubRole role, bool coachesATeam) =>
        role == ClubRole.Member && coachesATeam ? nameof(UserRole.Coach) : role.ToString();

    private async Task<InvitePageViewModel> InvitePageAsync(InviteFormViewModel form, CancellationToken cancellationToken)
    {
        var options = (await teams.GetTeamSeasonsForUserAsync(User.GetUserId(), cancellationToken))
            .Where(t => t.SeasonStatus != SeasonStatus.Archived)
            .Select(t => new TeamOption(t.Id, $"{t.DisplayName} ({t.SeasonName})"))
            .ToList();
        return new InvitePageViewModel(form, options);
    }
}
