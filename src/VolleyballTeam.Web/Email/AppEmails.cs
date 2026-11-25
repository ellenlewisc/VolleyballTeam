using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Email;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Email;

public record EmailBranding(string ClubName, string PrimaryColor);

public record InvitationEmailModel(EmailBranding Branding, string InviterName, string RoleDescription, string AcceptUrl, int ValidDays);

public record PasswordResetEmailModel(EmailBranding Branding, string FirstName, string ResetUrl);

public class AppEmails(RazorViewRenderer renderer, EmailSender sender)
{
    public async Task<bool> SendInvitationAsync(
        Club club, string email, string inviterName, ClubRole clubRole, string? teamName, StaffRole? staffRole, string acceptUrl, CancellationToken cancellationToken)
    {
        var role = teamName is not null && staffRole is not null
            ? $"{StaffRoleNames.Describe(staffRole)} for {teamName}"
            : clubRole == ClubRole.Admin ? "a club admin" : "a club member";
        var model = new InvitationEmailModel(Brand(club), inviterName, role, acceptUrl, 7);

        var html = await renderer.RenderAsync("/Views/Emails/Invitation.cshtml", model);
        var text = $"{inviterName} invited you to join {club.Name} on Volleyball Team Manager as {role}.\n\n"
            + $"Accept the invitation (valid for 7 days):\n{acceptUrl}\n\nIf you weren't expecting this, you can ignore this email.";

        return await sender.SendAsync(new EmailMessage(email, $"You're invited to {club.Name}", html, text), cancellationToken);
    }

    public async Task<bool> SendPasswordResetAsync(Club? club, string email, string firstName, string resetUrl, CancellationToken cancellationToken)
    {
        var branding = club is null ? new EmailBranding("Volleyball Team Manager", "#1d4ed8") : Brand(club);
        var html = await renderer.RenderAsync("/Views/Emails/PasswordReset.cshtml", new PasswordResetEmailModel(branding, firstName, resetUrl));
        var text = $"Hi {firstName},\n\nSomeone asked to reset your Volleyball Team Manager password. If it was you, choose a new one here:\n{resetUrl}\n\n"
            + "If it wasn't you, ignore this email; your password hasn't changed.";

        return await sender.SendAsync(new EmailMessage(email, "Reset your password", html, text), cancellationToken);
    }

    private static EmailBranding Brand(Club club) => new(club.Name, club.PrimaryColor);
}
