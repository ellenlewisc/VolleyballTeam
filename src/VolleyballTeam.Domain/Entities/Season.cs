using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class Season : TenantModel
{
    public required string Name { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public SeasonStatus Status { get; set; } = SeasonStatus.Upcoming;

    public ICollection<TeamSeason> TeamSeasons { get; set; } = [];

    public bool IsArchived => Status == SeasonStatus.Archived;
}
