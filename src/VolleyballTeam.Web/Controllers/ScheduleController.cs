using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubMember)]
[Route("{club:clubslug}/teams/{id:int}/schedule")]
public class ScheduleController(
    TenantContext tenant,
    TeamService teams,
    ScheduleService schedule,
    TeamSeasonAccessService access,
    TimeProvider clock,
    IAuthorizationService authorization) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int id, bool past, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.View))
        {
            return Forbid();
        }

        var team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        var userAccess = await access.GetAsync(User.GetUserId(), id, cancellationToken);
        var events = await schedule.GetEventsAsync(id, past, cancellationToken: cancellationToken);
        return View(new SchedulePageViewModel(team, userAccess, events, past));
    }

    [HttpGet("new")]
    public async Task<IActionResult> Create(int id, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        var tomorrow = ClubTime.ToLocal(clock.GetUtcNow().UtcDateTime, ClubTime.Zone(CurrentClub.TimeZoneId)).Date.AddDays(1);
        return await FormAsync(id, new EventFormViewModel { StartsAt = tomorrow.AddHours(18), EndsAt = tomorrow.AddHours(20) }, cancellationToken);
    }

    [HttpPost("new")]
    public async Task<IActionResult> Create(int id, EventFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        model.EventId = null;
        if (ModelState.IsValid)
        {
            try
            {
                var count = await schedule.CreateAsync(id, model.ToInput(), model.RepeatUntil, cancellationToken);
                Flash(count == 1 ? "Event added." : $"{count} events added.");
                return RedirectToAction(nameof(Index), new { club = ClubSlug, id });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return await FormAsync(id, model, cancellationToken);
    }

    [HttpGet("{eventId:int}")]
    public async Task<IActionResult> Edit(int id, int eventId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        var row = await schedule.GetEventAsync(id, eventId, cancellationToken);
        return row is null ? NotFound() : await FormAsync(id, EventFormViewModel.From(row), cancellationToken);
    }

    [HttpPost("{eventId:int}")]
    public async Task<IActionResult> Edit(int id, int eventId, EventFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        model.EventId = eventId;
        model.RepeatUntil = null;
        if (ModelState.IsValid)
        {
            try
            {
                await schedule.UpdateAsync(id, eventId, model.ToInput(), cancellationToken);
                Flash("Event saved.");
                return RedirectToAction(nameof(Index), new { club = ClubSlug, id });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return await FormAsync(id, model, cancellationToken);
    }

    [HttpPost("{eventId:int}/cancel")]
    public Task<IActionResult> Cancel(int id, int eventId, CancellationToken cancellationToken) =>
        ChangeAsync(id, () => schedule.SetCancelledAsync(id, eventId, cancelled: true, cancellationToken), "Event marked as cancelled. It stays on the schedule.");

    [HttpPost("{eventId:int}/reinstate")]
    public Task<IActionResult> Reinstate(int id, int eventId, CancellationToken cancellationToken) =>
        ChangeAsync(id, () => schedule.SetCancelledAsync(id, eventId, cancelled: false, cancellationToken), "Event is back on.");

    [HttpPost("{eventId:int}/delete")]
    public Task<IActionResult> Delete(int id, int eventId, CancellationToken cancellationToken) =>
        ChangeAsync(id, () => schedule.DeleteAsync(id, eventId, cancellationToken), "Event deleted.");

    private async Task<IActionResult> ChangeAsync(int id, Func<Task> change, string message)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        try
        {
            await change();
            Flash(message);
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { club = ClubSlug, id });
    }

    private async Task<IActionResult> FormAsync(int id, EventFormViewModel model, CancellationToken cancellationToken)
    {
        model.Team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        return model.Team is null ? NotFound() : View("Form", model);
    }

    private async Task<bool> IsAllowedAsync(int id, TeamSeasonRequirement requirement) =>
        (await authorization.AuthorizeAsync(User, new TeamSeasonResource(id), requirement)).Succeeded;
}
