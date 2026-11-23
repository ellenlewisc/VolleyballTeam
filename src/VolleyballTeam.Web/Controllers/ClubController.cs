using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubMember)]
[Route("{club:clubslug}")]
public class ClubController(TenantContext tenant, TeamService teams, IAuthorizationService authorization) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cards = await teams.GetTeamSeasonsForUserAsync(User.GetUserId(), cancellationToken);
        var isAdmin = (await authorization.AuthorizeAsync(User, Policies.ClubAdmin)).Succeeded;

        return View(new ClubDashboardViewModel(
            CurrentClub.Name,
            cards.Where(c => c.SeasonStatus != SeasonStatus.Archived).ToList(),
            cards.Where(c => c.SeasonStatus == SeasonStatus.Archived).ToList(),
            isAdmin));
    }
}
