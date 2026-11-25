using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record MemberRow(int UserId, string Name, string Email, ClubRole Role, bool IsActive, string CurrentTeams);

public record MemberAssignment(int TeamSeasonId, string TeamName, string SeasonName, SeasonStatus SeasonStatus, StaffRole Role);

public record MemberDetails(
    int UserId, string FirstName, string LastName, string Email, ClubRole Role, bool IsMembershipActive, bool IsAccountActive, IReadOnlyList<MemberAssignment> Assignments)
{
    public bool IsActive => IsMembershipActive && IsAccountActive;
}

public class MemberService(AppDbContext db, MemberStatusService status)
{
    public const string SortByName = "name";
    public const string SortByEmail = "email";
    public const string SortByRole = "role";

    public async Task<PagedResult<MemberRow>> QueryAsync(
        string? search, string? sortBy, bool descending, int skip, int take, CancellationToken cancellationToken = default)
    {
        var all = db.ClubMemberships.AsQueryable();
        var filtered = all;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(m => (m.User!.FirstName + " " + m.User.LastName).Contains(term) || m.User.Email!.Contains(term));
        }

        filtered = (sortBy, descending) switch
        {
            (SortByEmail, false) => filtered.OrderBy(m => m.User!.Email),
            (SortByEmail, true) => filtered.OrderByDescending(m => m.User!.Email),
            (SortByRole, false) => filtered.OrderBy(m => m.Role).ThenBy(m => m.User!.LastName),
            (SortByRole, true) => filtered.OrderByDescending(m => m.Role).ThenBy(m => m.User!.LastName),
            (_, true) => filtered.OrderByDescending(m => m.User!.LastName).ThenByDescending(m => m.User!.FirstName),
            _ => filtered.OrderBy(m => m.User!.LastName).ThenBy(m => m.User!.FirstName),
        };

        var rows = await filtered
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 100))
            .Select(m => new
            {
                m.UserId,
                Name = m.User!.LastName + ", " + m.User.FirstName,
                Email = m.User.Email ?? "",
                m.Role,
                IsActive = m.DeactivatedAt == null && m.User.DeactivatedAt == null,
                Teams = db.TeamSeasonStaff
                    .Where(s => s.UserId == m.UserId && s.TeamSeason!.Season!.Status != SeasonStatus.Archived)
                    .Select(s => s.TeamSeason!.DisplayName)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<MemberRow>(
            await all.CountAsync(cancellationToken),
            await filtered.CountAsync(cancellationToken),
            rows.Select(r => new MemberRow(r.UserId, r.Name, r.Email, r.Role, r.IsActive, string.Join(", ", r.Teams))).ToList());
    }

    public async Task<MemberDetails?> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        var member = await db.ClubMemberships
            .Where(m => m.UserId == userId)
            .Select(m => new
            {
                m.UserId,
                m.User!.FirstName,
                m.User.LastName,
                Email = m.User.Email ?? "",
                m.Role,
                MembershipActive = m.DeactivatedAt == null,
                AccountActive = m.User.DeactivatedAt == null,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            return null;
        }

        var assignments = await db.TeamSeasonStaff
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.TeamSeason!.Season!.Name)
            .ThenBy(s => s.TeamSeason!.DisplayName)
            .Select(s => new MemberAssignment(s.TeamSeasonId, s.TeamSeason!.DisplayName, s.TeamSeason.Season!.Name, s.TeamSeason.Season.Status, s.Role))
            .ToListAsync(cancellationToken);

        return new MemberDetails(
            member.UserId, member.FirstName, member.LastName, member.Email, member.Role, member.MembershipActive, member.AccountActive, assignments);
    }

    public async Task<string> GetDisplayNameAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.Users.Where(u => u.Id == userId).Select(u => u.FirstName + " " + u.LastName).SingleOrDefaultAsync(cancellationToken)
            ?? "A club admin";

    public async Task ChangeRoleAsync(int actingUserId, int userId, ClubRole role, CancellationToken cancellationToken = default)
    {
        var membership = await LoadChangeableAsync(actingUserId, userId, cancellationToken);

        if (role == ClubRole.Owner)
        {
            throw new BusinessRuleException("The club has one owner; make this person an admin instead.");
        }

        membership.Role = role;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(int actingUserId, int userId, bool active, CancellationToken cancellationToken = default)
    {
        await LoadChangeableAsync(actingUserId, userId, cancellationToken);
        await status.SetClubMembershipActiveAsync(userId, active, cancellationToken);
    }

    private async Task<Domain.Entities.ClubMembership> LoadChangeableAsync(int actingUserId, int userId, CancellationToken cancellationToken)
    {
        if (actingUserId == userId)
        {
            throw new BusinessRuleException("You can't change your own role or status; ask another admin.");
        }

        var membership = await db.ClubMemberships.SingleOrDefaultAsync(m => m.UserId == userId, cancellationToken)
            ?? throw new BusinessRuleException("That member wasn't found.");

        if (membership.Role == ClubRole.Owner)
        {
            throw new BusinessRuleException("The club owner can't be changed or deactivated.");
        }

        return membership;
    }
}
