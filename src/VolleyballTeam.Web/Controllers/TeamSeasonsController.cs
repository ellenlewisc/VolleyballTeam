using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubMember)]
[Route("{club:clubslug}/teams/{id:int}")]
public class TeamSeasonsController(
    TenantContext tenant,
    TeamService teams,
    TeamSeasonAccessService access,
    IAuthorizationService authorization) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public async Task<IActionResult> Show(int id, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.View))
        {
            return Forbid();
        }

        return await ShowPageAsync(id, new AddStaffViewModel(), cancellationToken);
    }

    [HttpGet("edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        var team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        return View(new TeamSeasonEditViewModel
        {
            Id = team.Id,
            SeasonName = team.SeasonName,
            DisplayName = team.DisplayName,
            AgeGroup = team.AgeGroup,
            Level = team.Level,
        });
    }

    [HttpPost("edit")]
    public async Task<IActionResult> Edit(int id, TeamSeasonEditViewModel model, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await teams.UpdateTeamSeasonAsync(id, model.DisplayName, model.AgeGroup, model.Level, cancellationToken);
                Flash("Team details saved.");
                return RedirectToAction(nameof(Show), new { club = ClubSlug, id });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(model);
    }

    [HttpPost("staff")]
    [Authorize(Policy = Policies.ClubAdmin)]
    public async Task<IActionResult> AddStaff(int id, [Bind(Prefix = "AddStaff")] AddStaffViewModel model, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await teams.AddStaffAsync(id, model.UserId!.Value, model.Role, cancellationToken);
                Flash("Staff member added.");
                return RedirectToAction(nameof(Show), new { club = ClubSlug, id });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return await ShowPageAsync(id, model, cancellationToken);
    }

    [HttpPost("staff/{staffId:int}/remove")]
    [Authorize(Policy = Policies.ClubAdmin)]
    public async Task<IActionResult> RemoveStaff(int id, int staffId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        try
        {
            await teams.RemoveStaffAsync(id, staffId, cancellationToken);
            Flash("Staff member removed.");
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Show), new { club = ClubSlug, id });
    }

    private async Task<IActionResult> ShowPageAsync(int id, AddStaffViewModel addStaff, CancellationToken cancellationToken)
    {
        var team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        var userAccess = await access.GetAsync(User.GetUserId(), id, cancellationToken);
        var candidates = userAccess.IsClubAdmin && userAccess.CanEdit
            ? await teams.GetStaffCandidatesAsync(id, cancellationToken)
            : [];

        return View(nameof(Show), new TeamSeasonPageViewModel(team, userAccess, candidates, addStaff));
    }

    private async Task<bool> IsAllowedAsync(int id, TeamSeasonRequirement requirement) =>
        (await authorization.AuthorizeAsync(User, new TeamSeasonResource(id), requirement)).Succeeded;
}
