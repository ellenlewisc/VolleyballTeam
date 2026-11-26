namespace VolleyballTeam.Domain.Entities;

public class TeamSeason : TenantModel
{
    public int TeamId { get; set; }
    public int SeasonId { get; set; }
    public required string DisplayName { get; set; }
    public string? AgeGroup { get; set; }
    public string? Level { get; set; }
    public int? RolledOverFromId { get; set; }

    public Team? Team { get; set; }
    public Season? Season { get; set; }
    public TeamSeason? RolledOverFrom { get; set; }
    public ICollection<TeamSeasonStaff> Staff { get; set; } = [];
    public ICollection<RosterEntry> Roster { get; set; } = [];
    public ICollection<TeamEvent> Events { get; set; } = [];
}
