using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubMember)]
[Route("{club:clubslug}/teams/{id:int}/roster")]
public class RosterController(
    TenantContext tenant,
    TeamService teams,
    RosterService roster,
    RosterCsvService csv,
    PlayerService players,
    TeamSeasonAccessService access,
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
        var rows = await roster.GetRosterAsync(id, past, cancellationToken);
        return View(new RosterPageViewModel(team, userAccess, rows, past));
    }

    [HttpGet("add")]
    public async Task<IActionResult> Add(int id, string? q, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        return await AddPageAsync(id, q, new PlayerNameInput(), cancellationToken);
    }

    [HttpPost("add/existing")]
    public async Task<IActionResult> AddExistingPlayer(int id, int playerId, PlacementInput placement, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            FlashFailure("Check the jersey number (0–99).");
            return RedirectToAction(nameof(Add), new { club = ClubSlug, id });
        }

        try
        {
            await roster.AddExistingPlayerAsync(id, playerId, placement.ToPlacement(), cancellationToken);
            Flash("Player added to the roster.");
            return RedirectToAction(nameof(Index), new { club = ClubSlug, id });
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
            return RedirectToAction(nameof(Add), new { club = ClubSlug, id });
        }
    }

    [HttpPost("add/new")]
    public async Task<IActionResult> AddNewPlayer(int id, [Bind(Prefix = nameof(RosterAddViewModel.NewPlayer))] PlayerNameInput input, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var entryId = await roster.AddNewPlayerAsync(id, input.FirstName, input.LastName, input.GradYear, input.ToPlacement(), cancellationToken);
                Flash($"{input.FirstName.Trim()} {input.LastName.Trim()} added. Add their guardians below.");
                return RedirectToAction(nameof(Entry), new { club = ClubSlug, id, entryId });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return await AddPageAsync(id, null, input, cancellationToken);
    }

    [HttpGet("{entryId:int}")]
    public async Task<IActionResult> Entry(int id, int entryId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        return await EntryPageAsync(id, entryId, null, new GuardianFormViewModel(), cancellationToken);
    }

    [HttpPost("{entryId:int}")]
    public async Task<IActionResult> Entry(int id, int entryId, [Bind(Prefix = nameof(RosterEntryViewModel.Player))] PlayerNameInput input, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await roster.UpdateEntryAsync(id, entryId, input.FirstName, input.LastName, input.GradYear, input.ToPlacement(), cancellationToken);
                Flash("Player saved.");
                return RedirectToAction(nameof(Index), new { club = ClubSlug, id });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return await EntryPageAsync(id, entryId, input, new GuardianFormViewModel(), cancellationToken);
    }

    [HttpPost("{entryId:int}/leave")]
    public async Task<IActionResult> Leave(int id, int entryId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        try
        {
            await roster.LeaveAsync(id, entryId, cancellationToken);
            Flash("Player removed from the current roster. They stay in the team's history.");
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { club = ClubSlug, id });
    }

    [HttpPost("{entryId:int}/guardians")]
    public async Task<IActionResult> AddGuardian(
        int id, int entryId, [Bind(Prefix = nameof(GuardiansPanelViewModel.NewGuardian))] GuardianFormViewModel guardian, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        var entry = await roster.GetEntryAsync(id, entryId, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await roster.EnsureEditableAsync(id, cancellationToken);
                await players.AddGuardianAsync(entry.PlayerId, guardian.ToInput(), cancellationToken);
                Flash("Guardian added.");
                return RedirectToAction(nameof(Entry), new { club = ClubSlug, id, entryId });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError("NewGuardian.Email", ex.Message);
            }
        }

        return await EntryPageAsync(id, entryId, null, guardian, cancellationToken);
    }

    [HttpPost("{entryId:int}/guardians/{guardianId:int}/remove")]
    public async Task<IActionResult> RemoveGuardian(int id, int entryId, int guardianId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        var entry = await roster.GetEntryAsync(id, entryId, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        try
        {
            await roster.EnsureEditableAsync(id, cancellationToken);
            await players.RemoveGuardianAsync(entry.PlayerId, guardianId, cancellationToken);
            Flash("Guardian removed.");
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Entry), new { club = ClubSlug, id, entryId });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(int id, CancellationToken cancellationToken)
    {
        var userAccess = await access.GetAsync(User.GetUserId(), id, cancellationToken);

        // The export includes guardian emails, so read-only history access isn't enough unless you're a club admin.
        if (!userAccess.CanEdit && !(userAccess.IsClubAdmin && userAccess.CanView))
        {
            return Forbid();
        }

        var team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        var bytes = await csv.ExportAsync(id, cancellationToken);
        var fileName = $"{team.DisplayName} {team.SeasonName} roster.csv".Replace('/', '-');
        return File(bytes, "text/csv", fileName);
    }

    [HttpGet("import")]
    public async Task<IActionResult> Import(int id, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        var team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        return team is null ? NotFound() : View(new RosterImportViewModel { Team = team });
    }

    [HttpPost("import")]
    [RequestSizeLimit(RosterCsvService.MaxBytes + 16 * 1024)]
    public async Task<IActionResult> Import(int id, IFormFile? file, CancellationToken cancellationToken)
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

        var model = new RosterImportViewModel { Team = team };

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose a CSV file.");
            return View(model);
        }

        if (file.Length > RosterCsvService.MaxBytes)
        {
            ModelState.AddModelError(string.Empty, "That file is too large for a roster.");
            return View(model);
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var text = await reader.ReadToEndAsync(cancellationToken);

        try
        {
            model.Preview = await csv.PreviewAsync(id, text, cancellationToken);
            model.CsvText = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(model);
    }

    [HttpPost("import/confirm")]
    [RequestSizeLimit(RosterCsvService.MaxBytes * 2)]
    public async Task<IActionResult> ConfirmImport(int id, string csvText, CancellationToken cancellationToken)
    {
        if (!await IsAllowedAsync(id, TeamSeasonRequirement.Edit))
        {
            return Forbid();
        }

        string text;
        try
        {
            text = Encoding.UTF8.GetString(Convert.FromBase64String(csvText));
        }
        catch (FormatException)
        {
            FlashFailure("The import expired. Upload the file again.");
            return RedirectToAction(nameof(Import), new { club = ClubSlug, id });
        }

        try
        {
            var result = await csv.ImportAsync(id, text, cancellationToken);
            Flash($"Import finished: {result.Added} added, {result.Updated} updated, {result.Skipped} skipped.");
            return RedirectToAction(nameof(Index), new { club = ClubSlug, id });
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
            return RedirectToAction(nameof(Import), new { club = ClubSlug, id });
        }
    }

    private async Task<IActionResult> AddPageAsync(int id, string? q, PlayerNameInput input, CancellationToken cancellationToken)
    {
        var team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        return View(nameof(Add), new RosterAddViewModel
        {
            Team = team,
            Search = q,
            Matches = await roster.FindPlayersToAddAsync(id, q, cancellationToken),
            NewPlayer = input,
        });
    }

    private async Task<IActionResult> EntryPageAsync(
        int id, int entryId, PlayerNameInput? input, GuardianFormViewModel newGuardian, CancellationToken cancellationToken)
    {
        var team = await teams.GetTeamSeasonAsync(id, cancellationToken);
        var entry = await roster.GetEntryAsync(id, entryId, cancellationToken);
        if (team is null || entry is null)
        {
            return NotFound();
        }

        var guardians = await players.GetGuardiansAsync(entry.PlayerId, cancellationToken);

        return View(nameof(Entry), new RosterEntryViewModel
        {
            Team = team,
            Entry = entry,
            Player = input ?? new PlayerNameInput
            {
                FirstName = entry.FirstName,
                LastName = entry.LastName,
                GradYear = entry.GradYear,
                JerseyNumber = entry.JerseyNumber,
                PrimaryPosition = entry.PrimaryPosition,
                SecondaryPosition = entry.SecondaryPosition,
            },
            Guardians = new GuardiansPanelViewModel(
                guardians
                    .Select(g => new GuardianListItem(g, Url.Action(nameof(RemoveGuardian), new { club = ClubSlug, id, entryId, guardianId = g.Id })))
                    .ToList(),
                Url.Action(nameof(AddGuardian), new { club = ClubSlug, id, entryId }),
                newGuardian),
        });
    }

    private async Task<bool> IsAllowedAsync(int id, TeamSeasonRequirement requirement) =>
        (await authorization.AuthorizeAsync(User, new TeamSeasonResource(id), requirement)).Succeeded;
}
