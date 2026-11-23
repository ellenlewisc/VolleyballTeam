using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubAdmin)]
[Route("{club:clubslug}/admin/teams")]
public class TeamsController(TenantContext tenant, TeamService teams, SeasonService seasons) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new TeamsPageViewModel(await teams.GetTeamsAsync(cancellationToken)));

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new TeamCreateViewModel { Seasons = await OpenSeasonsAsync(cancellationToken) };
        model.SeasonId = model.Seasons.FirstOrDefault(s => s.Status == SeasonStatus.Active)?.Id ?? (model.Seasons.Count > 0 ? model.Seasons[0].Id : null);
        return View(model);
    }

    [HttpPost("new")]
    public async Task<IActionResult> Create(TeamCreateViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var teamSeasonId = await teams.CreateTeamAsync(model.Name, model.SeasonId!.Value, model.DisplayName, model.AgeGroup, model.Level, cancellationToken);
                Flash("Team created.");
                return RedirectToAction(nameof(TeamSeasonsController.Show), "TeamSeasons", new { club = ClubSlug, id = teamSeasonId });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        model.Seasons = await OpenSeasonsAsync(cancellationToken);
        return View(model);
    }

    private async Task<IReadOnlyList<SeasonRow>> OpenSeasonsAsync(CancellationToken cancellationToken) =>
        (await seasons.GetSeasonsAsync(cancellationToken)).Where(s => s.Status != SeasonStatus.Archived).ToList();
}
