using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record RosterRow(
    int EntryId, int PlayerId, string FirstName, string LastName, int? GradYear, int? JerseyNumber,
    Position? PrimaryPosition, Position? SecondaryPosition, DateOnly JoinedOn, DateOnly? LeftOn, int GuardianCount);

public record RosterEntryDetails(
    int EntryId, int TeamSeasonId, int PlayerId, string FirstName, string LastName, int? GradYear, int? JerseyNumber,
    Position? PrimaryPosition, Position? SecondaryPosition, DateOnly JoinedOn, DateOnly? LeftOn);

public record RosterPlacement(int? JerseyNumber, Position? PrimaryPosition, Position? SecondaryPosition);

public record PlayerMatch(int PlayerId, string FullName, int? GradYear, string? CurrentTeams);

public class RosterService(AppDbContext db, TimeProvider clock)
{
    public Task<List<RosterRow>> GetRosterAsync(int teamSeasonId, bool includeLeft, CancellationToken cancellationToken = default) =>
        db.RosterEntries
            .Where(r => r.TeamSeasonId == teamSeasonId && (includeLeft || r.LeftOn == null))
            .OrderBy(r => r.LeftOn != null)
            .ThenBy(r => r.JerseyNumber == null)
            .ThenBy(r => r.JerseyNumber)
            .ThenBy(r => r.Player!.LastName)
            .Select(r => new RosterRow(
                r.Id, r.PlayerId, r.Player!.FirstName, r.Player.LastName, r.Player.GradYear, r.JerseyNumber,
                r.PrimaryPosition, r.SecondaryPosition, r.JoinedOn, r.LeftOn, r.Player.Guardians.Count))
            .ToListAsync(cancellationToken);

    public Task<RosterEntryDetails?> GetEntryAsync(int teamSeasonId, int entryId, CancellationToken cancellationToken = default) =>
        db.RosterEntries
            .Where(r => r.Id == entryId && r.TeamSeasonId == teamSeasonId)
            .Select(r => new RosterEntryDetails(
                r.Id, r.TeamSeasonId, r.PlayerId, r.Player!.FirstName, r.Player.LastName, r.Player.GradYear, r.JerseyNumber,
                r.PrimaryPosition, r.SecondaryPosition, r.JoinedOn, r.LeftOn))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<List<PlayerMatch>> FindPlayersToAddAsync(int teamSeasonId, string? search, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return [];
        }

        var term = search.Trim();
        var rows = await db.Players
            .Where(p => (p.FirstName + " " + p.LastName).Contains(term)
                && !p.RosterEntries.Any(r => r.TeamSeasonId == teamSeasonId && r.LeftOn == null))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(20)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.GradYear,
                Teams = p.RosterEntries
                    .Where(r => r.LeftOn == null && r.TeamSeason!.Season!.Status != SeasonStatus.Archived)
                    .Select(r => r.TeamSeason!.DisplayName)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new PlayerMatch(r.Id, $"{r.FirstName} {r.LastName}", r.GradYear, r.Teams.Count == 0 ? null : string.Join(", ", r.Teams)))
            .ToList();
    }

    public async Task<int> AddExistingPlayerAsync(int teamSeasonId, int playerId, RosterPlacement placement, CancellationToken cancellationToken = default)
    {
        await EnsureEditableAsync(teamSeasonId, cancellationToken);

        if (!await db.Players.AnyAsync(p => p.Id == playerId, cancellationToken))
        {
            throw new BusinessRuleException("That player wasn't found.");
        }

        if (await db.RosterEntries.AnyAsync(r => r.TeamSeasonId == teamSeasonId && r.PlayerId == playerId && r.LeftOn == null, cancellationToken))
        {
            throw new BusinessRuleException("That player is already on this roster.");
        }

        await EnsureJerseyFreeAsync(teamSeasonId, placement.JerseyNumber, exceptEntryId: null, cancellationToken);

        var entry = NewEntry(teamSeasonId, placement);
        entry.PlayerId = playerId;
        db.RosterEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }

    public async Task<int> AddNewPlayerAsync(
        int teamSeasonId, string firstName, string lastName, int? gradYear, RosterPlacement placement, CancellationToken cancellationToken = default)
    {
        await EnsureEditableAsync(teamSeasonId, cancellationToken);
        await EnsureJerseyFreeAsync(teamSeasonId, placement.JerseyNumber, exceptEntryId: null, cancellationToken);

        var entry = NewEntry(teamSeasonId, placement);
        entry.Player = new Player { FirstName = firstName.Trim(), LastName = lastName.Trim(), GradYear = gradYear };
        db.RosterEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }

    public async Task UpdateEntryAsync(
        int teamSeasonId, int entryId, string firstName, string lastName, int? gradYear, RosterPlacement placement, CancellationToken cancellationToken = default)
    {
        await EnsureEditableAsync(teamSeasonId, cancellationToken);

        var entry = await db.RosterEntries.Include(r => r.Player)
            .SingleOrDefaultAsync(r => r.Id == entryId && r.TeamSeasonId == teamSeasonId, cancellationToken)
            ?? throw new BusinessRuleException("That roster entry wasn't found.");

        if (entry.IsActive)
        {
            await EnsureJerseyFreeAsync(teamSeasonId, placement.JerseyNumber, entryId, cancellationToken);
        }

        entry.JerseyNumber = placement.JerseyNumber;
        entry.PrimaryPosition = placement.PrimaryPosition;
        entry.SecondaryPosition = placement.SecondaryPosition;
        entry.Player!.FirstName = firstName.Trim();
        entry.Player.LastName = lastName.Trim();
        entry.Player.GradYear = gradYear;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LeaveAsync(int teamSeasonId, int entryId, CancellationToken cancellationToken = default)
    {
        await EnsureEditableAsync(teamSeasonId, cancellationToken);

        var entry = await db.RosterEntries.SingleOrDefaultAsync(r => r.Id == entryId && r.TeamSeasonId == teamSeasonId, cancellationToken)
            ?? throw new BusinessRuleException("That roster entry wasn't found.");

        if (!entry.IsActive)
        {
            throw new BusinessRuleException("That player has already left this team.");
        }

        entry.LeftOn = Today();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureEditableAsync(int teamSeasonId, CancellationToken cancellationToken)
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

    internal RosterEntry NewEntry(int teamSeasonId, RosterPlacement placement) => new()
    {
        TeamSeasonId = teamSeasonId,
        JerseyNumber = placement.JerseyNumber,
        PrimaryPosition = placement.PrimaryPosition,
        SecondaryPosition = placement.SecondaryPosition,
        JoinedOn = Today(),
    };

    private async Task EnsureJerseyFreeAsync(int teamSeasonId, int? jerseyNumber, int? exceptEntryId, CancellationToken cancellationToken)
    {
        if (jerseyNumber is null)
        {
            return;
        }

        var holder = await db.RosterEntries
            .Where(r => r.TeamSeasonId == teamSeasonId && r.LeftOn == null && r.JerseyNumber == jerseyNumber && r.Id != exceptEntryId)
            .Select(r => r.Player!.FirstName + " " + r.Player.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        if (holder is not null)
        {
            throw new BusinessRuleException($"Number {jerseyNumber} is already worn by {holder}.");
        }
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
