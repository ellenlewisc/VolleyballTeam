using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Access;

public class TeamSeasonAccessService(AppDbContext db)
{
    public Task<ClubRole?> GetActiveClubRoleAsync(int userId, CancellationToken cancellationToken = default) =>
        db.ClubMemberships
            .Where(m => m.UserId == userId && m.DeactivatedAt == null && m.User!.DeactivatedAt == null)
            .Select(m => (ClubRole?)m.Role)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TeamSeasonAccess> GetAsync(int userId, int teamSeasonId, CancellationToken cancellationToken = default)
    {
        var target = await db.TeamSeasons
            .Where(t => t.Id == teamSeasonId)
            .Select(t => new
            {
                t.Season!.Status,
                StaffRole = t.Staff.Where(s => s.UserId == userId).Select(s => (StaffRole?)s.Role).FirstOrDefault(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (target is null)
        {
            return TeamSeasonAccess.Denied;
        }

        var clubRole = await GetActiveClubRoleAsync(userId, cancellationToken);
        return TeamSeasonAccess.Decide(clubRole, target.Status, target.StaffRole);
    }
}
