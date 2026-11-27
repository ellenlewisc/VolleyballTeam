using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubAdmin)]
[Route("{club:clubslug}/admin/seasons")]
public class SeasonsController(TenantContext tenant, SeasonService seasons) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new SeasonsPageViewModel(await seasons.GetSeasonsAsync(cancellationToken)));

    [HttpGet("new")]
    public IActionResult Create() => View("Form", new SeasonFormViewModel());

    [HttpPost("new")]
    public async Task<IActionResult> Create(SeasonFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = null;
        if (ModelState.IsValid)
        {
            try
            {
                await seasons.CreateSeasonAsync(model.Name, model.StartsOn, model.EndsOn, cancellationToken);
                Flash($"Season {model.Name.Trim()} created. Roll teams into it when you're ready.");
                return RedirectToAction(nameof(Index), new { club = ClubSlug });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View("Form", model);
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var season = await seasons.GetSeasonAsync(id, cancellationToken);
        if (season is null)
        {
            return NotFound();
        }

        return View("Form", new SeasonFormViewModel { Id = season.Id, Name = season.Name, StartsOn = season.StartsOn, EndsOn = season.EndsOn });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, SeasonFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await seasons.UpdateSeasonAsync(id, model.Name, model.StartsOn, model.EndsOn, cancellationToken);
                Flash($"Season {model.Name.Trim()} saved.");
                return RedirectToAction(nameof(Index), new { club = ClubSlug });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View("Form", model);
    }

    [HttpPost("{id:int}/start")]
    public Task<IActionResult> Start(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, SeasonStatus.Active, "Season started.", cancellationToken);

    [HttpPost("{id:int}/archive")]
    public Task<IActionResult> Archive(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, SeasonStatus.Archived, "Season archived. Its teams are now read-only.", cancellationToken);

    [HttpPost("{id:int}/reopen")]
    public Task<IActionResult> Reopen(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, SeasonStatus.Active, "Season reopened for editing. Archive it again when you're done.", cancellationToken);

    [HttpGet("{id:int}/rollover")]
    public async Task<IActionResult> Rollover(int id, int? from, CancellationToken cancellationToken)
    {
        var plan = await seasons.GetRolloverAsync(id, from, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        return View(new RolloverViewModel
        {
            Plan = plan,
            SourceSeasonId = plan.SourceSeasonId,
            Rows = plan.Candidates.Select(c => new RolloverRowInput
            {
                SourceTeamSeasonId = c.SourceTeamSeasonId,
                Include = !c.AlreadyInTarget,
                DisplayName = c.DisplayName,
                AgeGroup = c.AgeGroup,
                Level = c.Level,
                StaffUserIds = c.Staff.Select(s => s.UserId).ToList(),
                PlayerIds = c.Players.Select(p => p.PlayerId).ToList(),
            }).ToList(),
        });
    }

    [HttpPost("{id:int}/rollover")]
    public async Task<IActionResult> Rollover(int id, RolloverViewModel model, CancellationToken cancellationToken)
    {
        var chosen = model.Rows
            .Where(r => r.Include)
            .Select(r => new RolloverTeam(r.SourceTeamSeasonId, r.DisplayName ?? "", r.AgeGroup, r.Level, r.StaffUserIds, r.PlayerIds))
            .ToList();

        if (chosen.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Tick at least one team to roll over.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var count = await seasons.RolloverAsync(id, chosen, cancellationToken);
                Flash(count == 1 ? "1 team rolled over." : $"{count} teams rolled over.");
                return RedirectToAction(nameof(Index), new { club = ClubSlug });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        model.Plan = await seasons.GetRolloverAsync(id, model.SourceSeasonId, cancellationToken);
        return model.Plan is null ? NotFound() : View(model);
    }

    private async Task<IActionResult> ChangeStatusAsync(int id, SeasonStatus status, string message, CancellationToken cancellationToken)
    {
        try
        {
            await seasons.SetStatusAsync(id, status, cancellationToken);
            Flash(message);
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { club = ClubSlug });
    }
}
