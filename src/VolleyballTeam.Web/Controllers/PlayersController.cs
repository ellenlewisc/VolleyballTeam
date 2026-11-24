using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubAdmin)]
[Route("{club:clubslug}/admin/players")]
public class PlayersController(TenantContext tenant, PlayerService players) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public IActionResult Index() => View();

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
        var page = await players.QueryDirectoryAsync(
            search, orderBy, string.Equals(orderDirection, "desc", StringComparison.OrdinalIgnoreCase), start, length, cancellationToken);

        return Json(new
        {
            draw,
            recordsTotal = page.Total,
            recordsFiltered = page.Filtered,
            data = page.Rows.Select(r => new
            {
                r.Id,
                r.Name,
                r.GradYear,
                r.CurrentTeams,
                r.GuardianCount,
                url = Url.Action(nameof(Show), new { club = ClubSlug, playerId = r.Id }),
            }),
        });
    }

    [HttpGet("{playerId:int}")]
    public Task<IActionResult> Show(int playerId, CancellationToken cancellationToken) =>
        PageAsync(playerId, null, new GuardianFormViewModel(), cancellationToken);

    [HttpPost("{playerId:int}")]
    public async Task<IActionResult> Show(int playerId, [Bind(Prefix = nameof(PlayerPageViewModel.Edit))] PlayerEditInput input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await players.UpdatePlayerAsync(playerId, input.FirstName, input.LastName, input.GradYear, cancellationToken);
                Flash("Player saved.");
                return RedirectToAction(nameof(Show), new { club = ClubSlug, playerId });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return await PageAsync(playerId, input, new GuardianFormViewModel(), cancellationToken);
    }

    [HttpPost("{playerId:int}/guardians")]
    public async Task<IActionResult> AddGuardian(
        int playerId, [Bind(Prefix = nameof(GuardiansPanelViewModel.NewGuardian))] GuardianFormViewModel guardian, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await players.AddGuardianAsync(playerId, guardian.ToInput(), cancellationToken);
                Flash("Guardian added.");
                return RedirectToAction(nameof(Show), new { club = ClubSlug, playerId });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError("NewGuardian.Email", ex.Message);
            }
        }

        return await PageAsync(playerId, null, guardian, cancellationToken);
    }

    [HttpPost("{playerId:int}/guardians/{guardianId:int}/remove")]
    public async Task<IActionResult> RemoveGuardian(int playerId, int guardianId, CancellationToken cancellationToken)
    {
        try
        {
            await players.RemoveGuardianAsync(playerId, guardianId, cancellationToken);
            Flash("Guardian removed.");
        }
        catch (BusinessRuleException ex)
        {
            FlashFailure(ex.Message);
        }

        return RedirectToAction(nameof(Show), new { club = ClubSlug, playerId });
    }

    private async Task<IActionResult> PageAsync(int playerId, PlayerEditInput? input, GuardianFormViewModel newGuardian, CancellationToken cancellationToken)
    {
        var player = await players.GetPlayerAsync(playerId, cancellationToken);
        if (player is null)
        {
            return NotFound();
        }

        return View(nameof(Show), new PlayerPageViewModel
        {
            Player = player,
            Edit = input ?? new PlayerEditInput { FirstName = player.FirstName, LastName = player.LastName, GradYear = player.GradYear },
            Guardians = new GuardiansPanelViewModel(
                player.Guardians
                    .Select(g => new GuardianListItem(g, Url.Action(nameof(RemoveGuardian), new { club = ClubSlug, playerId, guardianId = g.Id })))
                    .ToList(),
                Url.Action(nameof(AddGuardian), new { club = ClubSlug, playerId }),
                newGuardian),
        });
    }
}
