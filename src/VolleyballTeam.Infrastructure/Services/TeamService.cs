using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record TeamSummary(int Id, string Name, string Season, string? AgeGroup, string? Level, TeamRole? Role);

public class TeamService(AppDbContext db)
{
    public async Task<List<TeamSummary>> GetTeamsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var clubRole = await db.ClubMemberships
            .Where(m => m.UserId == userId)
            .Select(m => (ClubRole?)m.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (clubRole is null)
        {
            return [];
        }

        var seesAllTeams = clubRole is ClubRole.Owner or ClubRole.Admin;

        return await db.Teams
            .Where(t => seesAllTeams || t.Memberships.Any(m => m.UserId == userId))
            .OrderByDescending(t => t.Season)
            .ThenBy(t => t.Name)
            .Select(t => new TeamSummary(
                t.Id,
                t.Name,
                t.Season,
                t.AgeGroup,
                t.Level,
                t.Memberships.Where(m => m.UserId == userId).Select(m => (TeamRole?)m.Role).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}
