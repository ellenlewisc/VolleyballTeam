using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class Invitation : TenantModel
{
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Only a SHA-256 hash is stored, so a database leak doesn't expose usable invitation links.
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public int? AcceptedUserId { get; set; }

    public ClubRole ClubRole { get; set; } = ClubRole.Member;
    public int? TeamSeasonId { get; set; }
    public StaffRole? StaffRole { get; set; }

    public TeamSeason? TeamSeason { get; set; }
}
