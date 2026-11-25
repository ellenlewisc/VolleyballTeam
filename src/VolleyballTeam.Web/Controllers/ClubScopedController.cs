using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Web.Controllers;

public abstract class ClubScopedController(TenantContext tenant) : Controller
{
    public const string FlashMessage = "FlashMessage";
    public const string FlashError = "FlashError";

    protected Club CurrentClub => tenant.Club ?? throw new TenantViolationException("Club route reached without a resolved club.");

    protected string ClubSlug => CurrentClub.Slug;

    protected void Flash(string message) => TempData[FlashMessage] = message;

    protected void FlashFailure(string message) => TempData[FlashError] = message;

    protected void FlashUnsentInvitation(string email, string acceptUrl)
    {
        // Locally there's no email service, so show the link to test the join flow; never in production.
        var developmentLink = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
            ? $" Local testing link: {acceptUrl}"
            : "";

        FlashFailure($"The invitation for {email} was saved, but the email couldn't be sent. Use Resend from Users once email is working.{developmentLink}");
    }
}
