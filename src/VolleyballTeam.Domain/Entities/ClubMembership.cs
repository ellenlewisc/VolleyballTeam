using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class ClubMembership : TenantModel
{
    public int UserId { get; set; }
    public ClubRole Role { get; set; }

    // Inactive in this club only; the account and other clubs are unaffected.
    public DateTime? DeactivatedAt { get; set; }

    public Club? Club { get; set; }
    public AppUser? User { get; set; }

    public bool IsActive => DeactivatedAt is null;
}
