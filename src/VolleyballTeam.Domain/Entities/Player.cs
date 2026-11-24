namespace VolleyballTeam.Domain.Entities;

// Deliberately minimal: players are minors, so no birthdates, addresses, phones or medical details (ADR 0013).
public class Player : TenantModel
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int? GradYear { get; set; }

    public ICollection<Guardian> Guardians { get; set; } = [];
    public ICollection<RosterEntry> RosterEntries { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
