using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class ClubMembership : TenantModel
{
    public int UserId { get; set; }
    public ClubRole Role { get; set; }

    public Club? Club { get; set; }
    public AppUser? User { get; set; }
}
