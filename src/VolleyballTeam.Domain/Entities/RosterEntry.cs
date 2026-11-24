using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class RosterEntry : TenantModel
{
    public int TeamSeasonId { get; set; }
    public int PlayerId { get; set; }
    public int? JerseyNumber { get; set; }
    public Position? PrimaryPosition { get; set; }
    public Position? SecondaryPosition { get; set; }
    public DateOnly JoinedOn { get; set; }

    // Leaving keeps the entry so past rosters stay complete.
    public DateOnly? LeftOn { get; set; }

    public TeamSeason? TeamSeason { get; set; }
    public Player? Player { get; set; }

    public bool IsActive => LeftOn is null;
}
