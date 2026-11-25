using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record GuardianRow(int Id, string FirstName, string LastName, string Email, string? Relationship, bool Unsubscribed);

public record PlayerHistoryRow(int TeamSeasonId, string TeamSeasonName, string SeasonName, int? JerseyNumber, DateOnly JoinedOn, DateOnly? LeftOn);

public record PlayerDetails(int Id, string FirstName, string LastName, int? GradYear, IReadOnlyList<GuardianRow> Guardians, IReadOnlyList<PlayerHistoryRow> History);

public record PlayerDirectoryRow(int Id, string Name, int? GradYear, string CurrentTeams, int GuardianCount, int? RosterEntryId);

// VisibleTeamSeasonIds is null for club admins (every player); otherwise players are limited to those team seasons.
public record PlayerDirectoryQuery(
    string? Search, string? SortBy, bool Descending, int Skip, int Take, int? TeamSeasonId, IReadOnlyCollection<int>? VisibleTeamSeasonIds);

public record PagedResult<T>(int Total, int Filtered, IReadOnlyList<T> Rows);

public record GuardianInput(string FirstName, string LastName, string Email, string? Relationship);

public class PlayerService(AppDbContext db)
{
    public const string SortByName = "name";
    public const string SortByGradYear = "gradYear";

    public async Task<PagedResult<PlayerDirectoryRow>> QueryDirectoryAsync(PlayerDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var all = db.Players.AsQueryable();
        if (query.VisibleTeamSeasonIds is { } visible)
        {
            all = all.Where(p => p.RosterEntries.Any(r => visible.Contains(r.TeamSeasonId)));
        }

        var filtered = all;
        var teamSeasonId = query.TeamSeasonId;
        if (teamSeasonId is not null)
        {
            // An archived team's roster is history, so it includes players who left; a current team shows who's on it now.
            filtered = filtered.Where(p => p.RosterEntries.Any(r => r.TeamSeasonId == teamSeasonId
                && (r.LeftOn == null || r.TeamSeason!.Season!.Status == SeasonStatus.Archived)));
        }

        var (search, sortBy, descending, skip, take) = (query.Search, query.SortBy, query.Descending, query.Skip, query.Take);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(p => (p.FirstName + " " + p.LastName).Contains(term)
                || p.Guardians.Any(g => g.Email.Contains(term) || (g.FirstName + " " + g.LastName).Contains(term)));
        }

        filtered = (sortBy, descending) switch
        {
            (SortByGradYear, false) => filtered.OrderBy(p => p.GradYear).ThenBy(p => p.LastName),
            (SortByGradYear, true) => filtered.OrderByDescending(p => p.GradYear).ThenBy(p => p.LastName),
            (_, true) => filtered.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName),
            _ => filtered.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
        };

        var rows = await filtered
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 100))
            .Select(p => new
            {
                p.Id,
                Name = p.LastName + ", " + p.FirstName,
                p.GradYear,
                Teams = p.RosterEntries
                    .Where(r => r.LeftOn == null && r.TeamSeason!.Season!.Status != SeasonStatus.Archived)
                    .Select(r => r.TeamSeason!.DisplayName)
                    .ToList(),
                GuardianCount = p.Guardians.Count,
                RosterEntryId = p.RosterEntries
                    .Where(r => r.TeamSeasonId == teamSeasonId)
                    .OrderBy(r => r.LeftOn != null)
                    .ThenByDescending(r => r.JoinedOn)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PlayerDirectoryRow>(
            await all.CountAsync(cancellationToken),
            await filtered.CountAsync(cancellationToken),
            rows.Select(r => new PlayerDirectoryRow(r.Id, r.Name, r.GradYear, string.Join(", ", r.Teams), r.GuardianCount, r.RosterEntryId)).ToList());
    }

    public async Task<PlayerDetails?> GetPlayerAsync(int playerId, CancellationToken cancellationToken = default)
    {
        var player = await db.Players.SingleOrDefaultAsync(p => p.Id == playerId, cancellationToken);
        if (player is null)
        {
            return null;
        }

        var guardians = await GetGuardiansAsync(playerId, cancellationToken);
        var history = await db.RosterEntries
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.TeamSeason!.Season!.Name)
            .ThenByDescending(r => r.JoinedOn)
            .Select(r => new PlayerHistoryRow(r.TeamSeasonId, r.TeamSeason!.DisplayName, r.TeamSeason.Season!.Name, r.JerseyNumber, r.JoinedOn, r.LeftOn))
            .ToListAsync(cancellationToken);

        return new PlayerDetails(player.Id, player.FirstName, player.LastName, player.GradYear, guardians, history);
    }

    public Task<List<GuardianRow>> GetGuardiansAsync(int playerId, CancellationToken cancellationToken = default) =>
        db.Guardians
            .Where(g => g.PlayerId == playerId)
            .OrderBy(g => g.LastName)
            .ThenBy(g => g.FirstName)
            .Select(g => new GuardianRow(g.Id, g.FirstName, g.LastName, g.Email, g.Relationship, g.UnsubscribedAt != null))
            .ToListAsync(cancellationToken);

    public async Task<int> CreatePlayerAsync(string firstName, string lastName, int? gradYear, CancellationToken cancellationToken = default)
    {
        var player = new Player { FirstName = firstName.Trim(), LastName = lastName.Trim(), GradYear = gradYear };
        db.Players.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        return player.Id;
    }

    public async Task UpdatePlayerAsync(int playerId, string firstName, string lastName, int? gradYear, CancellationToken cancellationToken = default)
    {
        var player = await db.Players.SingleOrDefaultAsync(p => p.Id == playerId, cancellationToken)
            ?? throw new BusinessRuleException("That player wasn't found.");

        player.FirstName = firstName.Trim();
        player.LastName = lastName.Trim();
        player.GradYear = gradYear;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddGuardianAsync(int playerId, GuardianInput input, CancellationToken cancellationToken = default)
    {
        if (!await db.Players.AnyAsync(p => p.Id == playerId, cancellationToken))
        {
            throw new BusinessRuleException("That player wasn't found.");
        }

        var email = NormalizeEmail(input.Email);
        if (await db.Guardians.AnyAsync(g => g.PlayerId == playerId && g.Email == email, cancellationToken))
        {
            throw new BusinessRuleException($"{email} is already a guardian for this player.");
        }

        db.Guardians.Add(new Guardian
        {
            PlayerId = playerId,
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Email = email,
            Relationship = TeamService.Blank(input.Relationship),
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveGuardianAsync(int playerId, int guardianId, CancellationToken cancellationToken = default)
    {
        var guardian = await db.Guardians.SingleOrDefaultAsync(g => g.Id == guardianId && g.PlayerId == playerId, cancellationToken)
            ?? throw new BusinessRuleException("That guardian wasn't found.");

        db.Guardians.Remove(guardian);
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
