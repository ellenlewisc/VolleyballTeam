namespace VolleyballTeam.Domain.Entities;

public class Team : TenantModel
{
    public required string Name { get; set; }

    public Club? Club { get; set; }
    public ICollection<TeamSeason> Seasons { get; set; } = [];
}
