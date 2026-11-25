using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record TeamSeasonCard(
    int Id, string DisplayName, string SeasonName, SeasonStatus SeasonStatus, string? AgeGroup, string? Level, StaffRole? StaffRole, AccessLevel Access);

public record StaffMember(int StaffId, int UserId, string FullName, string? Email, StaffRole Role);

public record TeamSeasonDetails(
    int Id,
    int TeamId,
    string TeamName,
    string DisplayName,
    string? AgeGroup,
    string? Level,
    int SeasonId,
    string SeasonName,
    SeasonStatus SeasonStatus,
    string? RolledOverFromName,
    IReadOnlyList<StaffMember> Staff);

public record TeamRow(int Id, string Name, int? LatestTeamSeasonId, string? LatestDisplayName, string? LatestSeasonName, int SeasonCount);

public record MemberOption(int UserId, string FullName);

public class TeamService(AppDbContext db, TeamSeasonAccessService access)
{
    public async Task<List<TeamSeasonCard>> GetTeamSeasonsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var clubRole = await access.GetActiveClubRoleAsync(userId, cancellationToken);
        if (clubRole is null)
        {
            return [];
        }

        var isAdmin = clubRole is ClubRole.Owner or ClubRole.Admin;

        var rows = await db.TeamSeasons
            .Where(t => isAdmin || t.Staff.Any(s => s.UserId == userId
                && (t.Season!.Status != SeasonStatus.Archived || s.Role == StaffRole.HeadCoach)))
            .OrderByDescending(t => t.Season!.Name)
            .ThenBy(t => t.DisplayName)
            .Select(t => new
            {
                t.Id,
                t.DisplayName,
                SeasonName = t.Season!.Name,
                t.Season.Status,
                t.AgeGroup,
                t.Level,
                StaffRole = t.Staff.Where(s => s.UserId == userId).Select(s => (StaffRole?)s.Role).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new TeamSeasonCard(
                r.Id, r.DisplayName, r.SeasonName, r.Status, r.AgeGroup, r.Level, r.StaffRole,
                TeamSeasonAccess.Decide(clubRole, r.Status, r.StaffRole).Level))
            .Where(c => c.Access != AccessLevel.None)
            .ToList();
    }

    public async Task<TeamSeasonDetails?> GetTeamSeasonAsync(int teamSeasonId, CancellationToken cancellationToken = default)
    {
        var details = await db.TeamSeasons
            .Where(t => t.Id == teamSeasonId)
            .Select(t => new
            {
                t.Id,
                t.TeamId,
                TeamName = t.Team!.Name,
                t.DisplayName,
                t.AgeGroup,
                t.Level,
                t.SeasonId,
                SeasonName = t.Season!.Name,
                SeasonStatus = t.Season.Status,
                RolledOverFromName = t.RolledOverFrom == null ? null : t.RolledOverFrom.DisplayName,
                RolledOverFromSeason = t.RolledOverFrom == null ? null : t.RolledOverFrom.Season!.Name,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (details is null)
        {
            return null;
        }

        var staff = await db.TeamSeasonStaff
            .Where(s => s.TeamSeasonId == teamSeasonId)
            .OrderBy(s => s.Role)
            .ThenBy(s => s.User!.LastName)
            .Select(s => new StaffMember(s.Id, s.UserId, s.User!.FirstName + " " + s.User.LastName, s.User.Email, s.Role))
            .ToListAsync(cancellationToken);

        return new TeamSeasonDetails(
            details.Id,
            details.TeamId,
            details.TeamName,
            details.DisplayName,
            details.AgeGroup,
            details.Level,
            details.SeasonId,
            details.SeasonName,
            details.SeasonStatus,
            details.RolledOverFromName is null ? null : $"{details.RolledOverFromName} ({details.RolledOverFromSeason})",
            staff);
    }

    public async Task UpdateTeamSeasonAsync(int teamSeasonId, string displayName, string? ageGroup, string? level, CancellationToken cancellationToken = default)
    {
        var teamSeason = await LoadEditableAsync(teamSeasonId, cancellationToken);
        displayName = displayName.Trim();

        if (await db.TeamSeasons.AnyAsync(t => t.SeasonId == teamSeason.SeasonId && t.Id != teamSeasonId && t.DisplayName == displayName, cancellationToken))
        {
            throw new BusinessRuleException($"Another team in this season is already called \"{displayName}\".");
        }

        teamSeason.DisplayName = displayName;
        teamSeason.AgeGroup = Blank(ageGroup);
        teamSeason.Level = Blank(level);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStaffAsync(int teamSeasonId, int userId, StaffRole role, CancellationToken cancellationToken = default)
    {
        await LoadEditableAsync(teamSeasonId, cancellationToken);

        if (!await db.ClubMemberships.AnyAsync(m => m.UserId == userId && m.DeactivatedAt == null, cancellationToken))
        {
            throw new BusinessRuleException("Only active club members can be added as staff.");
        }

        if (await db.TeamSeasonStaff.AnyAsync(s => s.TeamSeasonId == teamSeasonId && s.UserId == userId, cancellationToken))
        {
            throw new BusinessRuleException("That person is already on this team's staff.");
        }

        db.TeamSeasonStaff.Add(new TeamSeasonStaff { TeamSeasonId = teamSeasonId, UserId = userId, Role = role });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveStaffAsync(int teamSeasonId, int staffId, CancellationToken cancellationToken = default)
    {
        await LoadEditableAsync(teamSeasonId, cancellationToken);

        var staff = await db.TeamSeasonStaff.SingleOrDefaultAsync(s => s.Id == staffId && s.TeamSeasonId == teamSeasonId, cancellationToken)
            ?? throw new BusinessRuleException("That staff member wasn't found.");

        db.TeamSeasonStaff.Remove(staff);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<MemberOption>> GetStaffCandidatesAsync(int teamSeasonId, CancellationToken cancellationToken = default) =>
        db.ClubMemberships
            .Where(m => m.DeactivatedAt == null && m.User!.DeactivatedAt == null
                && !db.TeamSeasonStaff.Any(s => s.TeamSeasonId == teamSeasonId && s.UserId == m.UserId))
            .OrderBy(m => m.User!.LastName)
            .ThenBy(m => m.User!.FirstName)
            .Select(m => new MemberOption(m.UserId, m.User!.FirstName + " " + m.User.LastName))
            .ToListAsync(cancellationToken);

    public Task<List<TeamRow>> GetTeamsAsync(CancellationToken cancellationToken = default) =>
        db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamRow(
                t.Id,
                t.Name,
                t.Seasons.OrderByDescending(s => s.Season!.Name).Select(s => (int?)s.Id).FirstOrDefault(),
                t.Seasons.OrderByDescending(s => s.Season!.Name).Select(s => s.DisplayName).FirstOrDefault(),
                t.Seasons.OrderByDescending(s => s.Season!.Name).Select(s => s.Season!.Name).FirstOrDefault(),
                t.Seasons.Count))
            .ToListAsync(cancellationToken);

    public async Task<int> CreateTeamAsync(string name, int seasonId, string? displayName, string? ageGroup, string? level, CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        displayName = Blank(displayName) ?? name;

        var season = await db.Seasons.SingleOrDefaultAsync(s => s.Id == seasonId, cancellationToken)
            ?? throw new BusinessRuleException("Choose a season.");

        if (season.IsArchived)
        {
            throw new BusinessRuleException("Teams can't be added to an archived season.");
        }

        if (await db.Teams.AnyAsync(t => t.Name == name, cancellationToken))
        {
            throw new BusinessRuleException($"A team called \"{name}\" already exists. Roll it over into the season instead.");
        }

        if (await db.TeamSeasons.AnyAsync(t => t.SeasonId == seasonId && t.DisplayName == displayName, cancellationToken))
        {
            throw new BusinessRuleException($"Another team in {season.Name} is already called \"{displayName}\".");
        }

        var teamSeason = new TeamSeason
        {
            Team = new Team { Name = name },
            SeasonId = seasonId,
            DisplayName = displayName,
            AgeGroup = Blank(ageGroup),
            Level = Blank(level),
        };

        db.TeamSeasons.Add(teamSeason);
        await db.SaveChangesAsync(cancellationToken);
        return teamSeason.Id;
    }

    private async Task<TeamSeason> LoadEditableAsync(int teamSeasonId, CancellationToken cancellationToken)
    {
        var teamSeason = await db.TeamSeasons.Include(t => t.Season).SingleOrDefaultAsync(t => t.Id == teamSeasonId, cancellationToken)
            ?? throw new BusinessRuleException("That team wasn't found.");

        if (teamSeason.Season!.IsArchived)
        {
            throw new BusinessRuleException("This season is archived and read-only. An admin can reopen it.");
        }

        return teamSeason;
    }

    internal static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
