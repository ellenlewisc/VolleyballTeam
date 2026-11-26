using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Domain.Entities;

public class TeamEvent : TenantModel
{
    public int TeamSeasonId { get; set; }
    public EventType Type { get; set; }
    public string? Title { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Location { get; set; }
    public string? Opponent { get; set; }
    public string? Notes { get; set; }

    // Cancelled events stay on the schedule so families can see what changed.
    public DateTime? CancelledAt { get; set; }

    public TeamSeason? TeamSeason { get; set; }

    public bool IsCancelled => CancelledAt is not null;
}
