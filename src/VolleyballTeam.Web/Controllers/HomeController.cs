using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[AllowAnonymous]
public class HomeController(UserClubService userClubs) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View();
        }

        var clubs = await userClubs.GetClubsForUserAsync(User.GetUserId(), cancellationToken);

        if (clubs.Count == 1)
        {
            return RedirectToAction(nameof(ClubController.Index), "Club", new { club = clubs[0].Slug });
        }

        return View("ChooseClub", new ChooseClubViewModel(clubs));
    }

    [Route("error/{statusCode:int?}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        Response.StatusCode = statusCode ?? StatusCodes.Status500InternalServerError;
        return View(new ErrorViewModel(Response.StatusCode, Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }
}
