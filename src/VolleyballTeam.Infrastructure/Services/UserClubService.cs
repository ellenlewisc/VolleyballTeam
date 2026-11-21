using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record UserClubSummary(string Slug, string Name, ClubRole Role);

public class UserClubService(AppDbContext db)
{
    // Runs before a tenant is chosen, so it looks across clubs but only at this user's memberships.
    public Task<List<UserClubSummary>> GetClubsForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.ClubMemberships
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .Where(m => m.UserId == userId && m.DeactivatedAt == null && m.Club!.DeletedAt == null)
            .OrderBy(m => m.Club!.Name)
            .Select(m => new UserClubSummary(m.Club!.Slug, m.Club.Name, m.Role))
            .ToListAsync(cancellationToken);
}
