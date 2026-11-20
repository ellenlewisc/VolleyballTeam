namespace VolleyballTeam.Domain.Entities;

public class Team : TenantModel
{
    public required string Name { get; set; }
    public required string Season { get; set; }
    public string? AgeGroup { get; set; }
    public string? Level { get; set; }

    public Club? Club { get; set; }
    public ICollection<TeamMembership> Memberships { get; set; } = [];
}
