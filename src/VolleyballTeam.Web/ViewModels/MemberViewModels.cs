using System.ComponentModel.DataAnnotations;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Services;

namespace VolleyballTeam.Web.ViewModels;

public record MembersPageViewModel(IReadOnlyList<PendingInvitation> Pending);

public record MemberPageViewModel(MemberDetails Member, bool IsSelf);

public record TeamOption(int Id, string Name);

public class InviteFormViewModel
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    public UserRole Role { get; set; } = UserRole.Coach;

    [Display(Name = "Team")]
    public int? TeamSeasonId { get; set; }

    [Display(Name = "Coaching role")]
    public StaffRole CoachingRole { get; set; } = Domain.Enums.StaffRole.AssistantCoach;

    public bool NeedsTeam => Role == UserRole.Coach;

    public ClubRole ClubRole => Role == UserRole.Admin ? ClubRole.Admin : ClubRole.Member;

    public StaffRole? StaffRole => NeedsTeam ? CoachingRole : null;
}

// What people see as a user's role; stored as the club role plus any team staff assignments.
public enum UserRole
{
    Member,
    Coach,
    Admin,
}

public record InvitePageViewModel(InviteFormViewModel Form, IReadOnlyList<TeamOption> Teams);

public class StaffInviteViewModel
{
    public TeamSeasonDetails? Team { get; set; }
    public IReadOnlyList<StaffRole> AllowedRoles { get; set; } = [];

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    [Display(Name = "Team role")]
    public StaffRole Role { get; set; } = StaffRole.AssistantCoach;
}

public class JoinRegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 10)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class JoinPageViewModel
{
    public InvitationView? Invitation { get; set; }
    public string Token { get; set; } = "";
    public string ClubName { get; set; } = "";
    public string? SignedInEmail { get; set; }
    public JoinRegisterViewModel Register { get; set; } = new();

    public bool IsUsable => Invitation is { IsExpired: false, IsAccepted: false };
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 10)]
    [Display(Name = "New password")]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}
