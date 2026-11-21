using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class TeamMembership : TenantModel
{
    public int UserId { get; set; }
    public int TeamId { get; set; }
    public TeamRole Role { get; set; }

    public Team? Team { get; set; }
    public AppUser? User { get; set; }
}
