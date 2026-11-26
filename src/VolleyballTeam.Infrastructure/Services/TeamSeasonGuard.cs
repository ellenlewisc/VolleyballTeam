using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

internal static class TeamSeasonGuard
{
    public static async Task EnsureTeamSeasonEditableAsync(this AppDbContext db, int teamSeasonId, CancellationToken cancellationToken)
    {
        var status = await db.TeamSeasons
            .Where(t => t.Id == teamSeasonId)
            .Select(t => (SeasonStatus?)t.Season!.Status)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException("That team wasn't found.");

        if (status == SeasonStatus.Archived)
        {
            throw new BusinessRuleException("This season is archived and read-only. An admin can reopen it.");
        }
    }
}
