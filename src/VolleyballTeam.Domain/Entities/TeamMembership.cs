using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

// Phase 0 table, mapped only so its data survives until the cleanup migration; use TeamSeasonStaff.
public class TeamMembership : TenantModel
{
    public int UserId { get; set; }
    public int TeamId { get; set; }
    public TeamRole Role { get; set; }
}
