using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubMember)]
[Route("{club:clubslug}")]
public class ClubController(TenantContext tenant, TeamService teams) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var club = tenant.Club ?? throw new TenantViolationException("Club route reached without a resolved club.");
        var userTeams = await teams.GetTeamsForUserAsync(User.GetUserId(), cancellationToken);

        return View(new ClubDashboardViewModel(club.Name, userTeams));
    }
}
