using Microsoft.AspNetCore.Identity;

namespace VolleyballTeam.Domain.Entities;

public class AppUser : IdentityUser<int>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Blocks sign-in everywhere; accounts are deactivated rather than deleted so history keeps its people.
    public DateTime? DeactivatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive => DeactivatedAt is null;
}
