using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record SeasonRow(int Id, string Name, DateOnly? StartsOn, DateOnly? EndsOn, SeasonStatus Status, int TeamCount);

public record RolloverStaff(int UserId, string FullName, StaffRole Role);

public record RolloverPlayer(int PlayerId, string FullName, int? JerseyNumber);

public record RolloverCandidate(
    int SourceTeamSeasonId, string TeamName, string DisplayName, string? AgeGroup, string? Level, bool AlreadyInTarget, IReadOnlyList<RolloverStaff> Staff, IReadOnlyList<RolloverPlayer> Players);

public record RolloverView(SeasonRow Target, IReadOnlyList<SeasonRow> Sources, int? SourceSeasonId, IReadOnlyList<RolloverCandidate> Candidates);

public record RolloverTeam(int SourceTeamSeasonId, string DisplayName, string? AgeGroup, string? Level, IReadOnlyCollection<int> StaffUserIds, IReadOnlyCollection<int> PlayerIds);

public class SeasonService(AppDbContext db, RosterService roster)
{
    public Task<List<SeasonRow>> GetSeasonsAsync(CancellationToken cancellationToken = default) =>
        db.Seasons
            .OrderByDescending(s => s.Name)
            .Select(s => new SeasonRow(s.Id, s.Name, s.StartsOn, s.EndsOn, s.Status, s.TeamSeasons.Count))
            .ToListAsync(cancellationToken);

    public async Task<int> CreateSeasonAsync(string name, DateOnly? startsOn, DateOnly? endsOn, CancellationToken cancellationToken = default)
    {
        name = name.Trim();

        if (startsOn is not null && endsOn is not null && endsOn < startsOn)
        {
            throw new BusinessRuleException("The season must end after it starts.");
        }

        if (await db.Seasons.AnyAsync(s => s.Name == name, cancellationToken))
        {
            throw new BusinessRuleException($"A season called \"{name}\" already exists.");
        }

        var season = new Season { Name = name, StartsOn = startsOn, EndsOn = endsOn, Status = SeasonStatus.Upcoming };
        db.Seasons.Add(season);
        await db.SaveChangesAsync(cancellationToken);
        return season.Id;
    }

    public async Task SetStatusAsync(int seasonId, SeasonStatus target, CancellationToken cancellationToken = default)
    {
        var season = await db.Seasons.SingleOrDefaultAsync(s => s.Id == seasonId, cancellationToken)
            ?? throw new BusinessRuleException("That season wasn't found.");

        var allowed = (season.Status, target) switch
        {
            (SeasonStatus.Upcoming, SeasonStatus.Active) => true,
            (SeasonStatus.Active, SeasonStatus.Archived) => true,
            (SeasonStatus.Archived, SeasonStatus.Active) => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new BusinessRuleException($"A season that is {season.Status} can't be changed to {target}.");
        }

        season.Status = target;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RolloverView?> GetRolloverAsync(int targetSeasonId, int? sourceSeasonId, CancellationToken cancellationToken = default)
    {
        var seasons = await GetSeasonsAsync(cancellationToken);
        var target = seasons.SingleOrDefault(s => s.Id == targetSeasonId);
        if (target is null)
        {
            return null;
        }

        var sources = seasons.Where(s => s.Id != targetSeasonId).ToList();
        sourceSeasonId ??= sources.FirstOrDefault(s => string.CompareOrdinal(s.Name, target.Name) < 0)?.Id ?? sources.FirstOrDefault()?.Id;

        if (sourceSeasonId is null || sources.All(s => s.Id != sourceSeasonId))
        {
            return new RolloverView(target, sources, null, []);
        }

        var teamsInTarget = await db.TeamSeasons
            .Where(t => t.SeasonId == targetSeasonId)
            .Select(t => t.TeamId)
            .ToListAsync(cancellationToken);

        var candidates = await db.TeamSeasons
            .Where(t => t.SeasonId == sourceSeasonId)
            .OrderBy(t => t.DisplayName)
            .Select(t => new
            {
                t.Id,
                t.TeamId,
                TeamName = t.Team!.Name,
                t.DisplayName,
                t.AgeGroup,
                t.Level,
                Staff = t.Staff
                    .OrderBy(s => s.Role)
                    .Select(s => new RolloverStaff(s.UserId, s.User!.FirstName + " " + s.User.LastName, s.Role))
                    .ToList(),
                Players = t.Roster
                    .Where(r => r.LeftOn == null)
                    .OrderBy(r => r.Player!.LastName)
                    .Select(r => new RolloverPlayer(r.PlayerId, r.Player!.FirstName + " " + r.Player.LastName, r.JerseyNumber))
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return new RolloverView(
            target,
            sources,
            sourceSeasonId,
            candidates
                .Select(c => new RolloverCandidate(c.Id, c.TeamName, c.DisplayName, c.AgeGroup, c.Level, teamsInTarget.Contains(c.TeamId), c.Staff, c.Players))
                .ToList());
    }

    public async Task<int> RolloverAsync(int targetSeasonId, IReadOnlyCollection<RolloverTeam> teams, CancellationToken cancellationToken = default)
    {
        var target = await db.Seasons.SingleOrDefaultAsync(s => s.Id == targetSeasonId, cancellationToken)
            ?? throw new BusinessRuleException("That season wasn't found.");

        if (target.IsArchived)
        {
            throw new BusinessRuleException("Teams can't be rolled into an archived season.");
        }

        var sourceIds = teams.Select(t => t.SourceTeamSeasonId).ToList();
        var sources = await db.TeamSeasons
            .Include(t => t.Staff)
            .Include(t => t.Roster.Where(r => r.LeftOn == null))
            .Where(t => sourceIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var takenTeamIds = await db.TeamSeasons.Where(t => t.SeasonId == targetSeasonId).Select(t => t.TeamId).ToListAsync(cancellationToken);
        var takenNames = await db.TeamSeasons.Where(t => t.SeasonId == targetSeasonId).Select(t => t.DisplayName).ToListAsync(cancellationToken);
        var names = new HashSet<string>(takenNames, StringComparer.OrdinalIgnoreCase);

        foreach (var request in teams)
        {
            if (!sources.TryGetValue(request.SourceTeamSeasonId, out var source))
            {
                throw new BusinessRuleException("One of the selected teams wasn't found.");
            }

            if (takenTeamIds.Contains(source.TeamId))
            {
                throw new BusinessRuleException($"{source.DisplayName} is already in {target.Name}.");
            }

            var displayName = TeamService.Blank(request.DisplayName) ?? source.DisplayName;
            if (!names.Add(displayName))
            {
                throw new BusinessRuleException($"Two teams in {target.Name} would both be called \"{displayName}\".");
            }

            db.TeamSeasons.Add(new TeamSeason
            {
                TeamId = source.TeamId,
                SeasonId = targetSeasonId,
                DisplayName = displayName,
                AgeGroup = TeamService.Blank(request.AgeGroup),
                Level = TeamService.Blank(request.Level),
                RolledOverFromId = source.Id,
                Staff = source.Staff
                    .Where(s => request.StaffUserIds.Contains(s.UserId))
                    .Select(s => new TeamSeasonStaff { UserId = s.UserId, Role = s.Role })
                    .ToList(),
                Roster = source.Roster
                    .Where(r => request.PlayerIds.Contains(r.PlayerId))
                    .Select(r =>
                    {
                        var entry = roster.NewEntry(0, new RosterPlacement(r.JerseyNumber, r.PrimaryPosition, r.SecondaryPosition));
                        entry.PlayerId = r.PlayerId;
                        return entry;
                    })
                    .ToList(),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return teams.Count;
    }
}
