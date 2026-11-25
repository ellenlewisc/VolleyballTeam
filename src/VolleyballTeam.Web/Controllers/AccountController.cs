using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Web.Email;
using VolleyballTeam.Web.Security;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Route("account")]
public class AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> users, AppEmails emails) : Controller
{
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction(nameof(HomeController.Index), "Home");
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "This account is locked after too many failed attempts. Try again in a few minutes."
            : "Invalid email or password.");

        return View(model);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await signInManager.SignOutAsync();
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await users.FindByEmailAsync(model.Email);

        // Same response whether or not the account exists, so the form can't be used to discover who has one.
        if (user is { IsActive: true, EmailConfirmed: true })
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token }, Request.Scheme)!;
            await emails.SendPasswordResetAsync(null, user.Email!, user.FirstName, url, cancellationToken);
        }

        return View("ForgotPasswordSent");
    }

    [AllowAnonymous]
    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string? email, string? token) =>
        string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)
            ? RedirectToAction(nameof(ForgotPassword))
            : View(new ResetPasswordViewModel { Email = email, Token = token });

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await users.FindByEmailAsync(model.Email);
        if (user is { IsActive: true })
        {
            var result = await users.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData[ClubScopedController.FlashMessage] = "Your password was changed. Sign in with the new one.";
                return RedirectToAction(nameof(Login));
            }

            if (result.Errors.All(e => e.Code != "InvalidToken"))
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        ModelState.AddModelError(string.Empty, "This reset link has expired or was already used. Request a new one.");
        return View(model);
    }
}
