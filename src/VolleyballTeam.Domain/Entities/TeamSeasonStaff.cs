using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class TeamSeasonStaff : TenantModel
{
    public int TeamSeasonId { get; set; }
    public int UserId { get; set; }
    public StaffRole Role { get; set; }

    public TeamSeason? TeamSeason { get; set; }
    public AppUser? User { get; set; }
}
