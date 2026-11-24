namespace VolleyballTeam.Domain.Entities;

public class Guardian : TenantModel
{
    public int PlayerId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Relationship { get; set; }
    public DateTime? UnsubscribedAt { get; set; }

    // Lets the email footer's unsubscribe link work without signing in.
    public string UnsubscribeToken { get; set; } = Guid.NewGuid().ToString("N");

    public Player? Player { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
