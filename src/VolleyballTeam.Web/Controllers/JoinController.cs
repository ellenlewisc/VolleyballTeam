using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.Security;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Route("{club:clubslug}/join/{token}")]
public class JoinController(
    TenantContext tenant,
    InvitationService invitations,
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn) : ClubScopedController(tenant)
{
    [AllowAnonymous]
    [HttpGet("")]
    public async Task<IActionResult> Index(string token, CancellationToken cancellationToken) =>
        View(await PageAsync(token, new JoinRegisterViewModel(), cancellationToken));

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Register(
        string token, [Bind(Prefix = nameof(JoinPageViewModel.Register))] JoinRegisterViewModel form, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index), new { club = ClubSlug, token });
        }

        if (ModelState.IsValid)
        {
            var result = await invitations.AcceptAsNewUserAsync(token, form.FirstName, form.LastName, form.Password, cancellationToken);
            if (result.Succeeded)
            {
                var user = await users.FindByIdAsync(result.UserId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                await signIn.SignInAsync(user!, isPersistent: false);
                Flash($"Welcome to {CurrentClub.Name}!");
                return RedirectToAction(nameof(ClubController.Index), "Club", new { club = ClubSlug });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        return View(nameof(Index), await PageAsync(token, form, cancellationToken));
    }

    [HttpPost("accept")]
    [Authorize]
    public async Task<IActionResult> Accept(string token, CancellationToken cancellationToken)
    {
        var result = await invitations.AcceptAsExistingUserAsync(token, User.GetUserId(), cancellationToken);
        if (!result.Succeeded)
        {
            FlashFailure(string.Join(" ", result.Errors));
            return RedirectToAction(nameof(Index), new { club = ClubSlug, token });
        }

        Flash($"Welcome to {CurrentClub.Name}!");
        return RedirectToAction(nameof(ClubController.Index), "Club", new { club = ClubSlug });
    }

    private async Task<JoinPageViewModel> PageAsync(string token, JoinRegisterViewModel form, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetAsync(token, cancellationToken);
        if (invitation is not null && string.IsNullOrEmpty(form.FirstName))
        {
            form.FirstName = invitation.FirstName ?? "";
            form.LastName = invitation.LastName ?? "";
        }

        return new JoinPageViewModel
        {
            Invitation = invitation,
            Token = token,
            ClubName = CurrentClub.Name,
            SignedInEmail = User.Identity?.IsAuthenticated == true ? (await users.GetUserAsync(User))?.Email : null,
            Register = form,
        };
    }
}
