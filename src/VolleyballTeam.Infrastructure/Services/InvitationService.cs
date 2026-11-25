using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public record InviteRequest(string Email, string? FirstName, string? LastName, ClubRole ClubRole, int? TeamSeasonId, StaffRole? StaffRole);

public enum InviteOutcome
{
    Invited,
    AddedExistingMember,
}

public record InviteResult(InviteOutcome Outcome, string Email, string? Token, string? TeamName);

public record InvitationView(
    int Id,
    string Email,
    string? FirstName,
    string? LastName,
    ClubRole ClubRole,
    string? TeamName,
    string? SeasonName,
    StaffRole? StaffRole,
    DateTime ExpiresAt,
    bool IsExpired,
    bool IsAccepted,
    bool AccountExists);

public record PendingInvitation(int Id, string Email, ClubRole ClubRole, string? TeamName, StaffRole? StaffRole, DateTime ExpiresAt, bool IsExpired);

public record AcceptResult(bool Succeeded, int? UserId, IReadOnlyList<string> Errors)
{
    public static AcceptResult Failed(params string[] errors) => new(false, null, errors);
}

public class InvitationService(AppDbContext db, UserManager<AppUser> users, TimeProvider clock)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public async Task<InviteResult> CreateAsync(InviteRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(email, out _))
        {
            throw new BusinessRuleException("Enter a valid email address.");
        }

        if (request.ClubRole == ClubRole.Owner)
        {
            throw new BusinessRuleException("Invite people as a member or admin; the club has one owner.");
        }

        var team = request.TeamSeasonId is int teamSeasonId ? await LoadTeamForStaffAsync(teamSeasonId, request.StaffRole, cancellationToken) : null;

        var normalizedEmail = email.ToUpperInvariant();
        var existingUser = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        var membership = existingUser is null
            ? null
            : await db.ClubMemberships.SingleOrDefaultAsync(m => m.UserId == existingUser.Id, cancellationToken);

        if (existingUser is not null && membership is not null)
        {
            if (!membership.IsActive || !existingUser.IsActive)
            {
                throw new BusinessRuleException($"{email} is deactivated. Reactivate them from Users instead.");
            }

            if (team is null)
            {
                throw new BusinessRuleException($"{email} is already a member of this club.");
            }

            await AddStaffAsync(team, existingUser.Id, request.StaffRole!.Value, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new InviteResult(InviteOutcome.AddedExistingMember, email, null, team.DisplayName);
        }

        var superseded = await db.Invitations.Where(i => i.Email == email && i.AcceptedAt == null).ToListAsync(cancellationToken);
        db.Invitations.RemoveRange(superseded);

        var token = NewToken();
        db.Invitations.Add(new Invitation
        {
            Email = email,
            FirstName = TeamService.Blank(request.FirstName),
            LastName = TeamService.Blank(request.LastName),
            TokenHash = Hash(token),
            ExpiresAt = Now() + Lifetime,
            ClubRole = request.ClubRole,
            TeamSeasonId = team?.Id,
            StaffRole = team is null ? null : request.StaffRole,
        });

        await db.SaveChangesAsync(cancellationToken);
        return new InviteResult(InviteOutcome.Invited, email, token, team?.DisplayName);
    }

    public async Task<InvitationView?> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = Hash(token);
        var invitation = await db.Invitations
            .Where(i => i.TokenHash == hash)
            .Select(i => new
            {
                i.Id,
                i.Email,
                i.FirstName,
                i.LastName,
                i.ClubRole,
                TeamName = i.TeamSeason == null ? null : i.TeamSeason.DisplayName,
                SeasonName = i.TeamSeason == null ? null : i.TeamSeason.Season!.Name,
                i.StaffRole,
                i.ExpiresAt,
                i.AcceptedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        var normalizedEmail = invitation.Email.ToUpperInvariant();
        var accountExists = await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        return new InvitationView(
            invitation.Id, invitation.Email, invitation.FirstName, invitation.LastName, invitation.ClubRole, invitation.TeamName, invitation.SeasonName,
            invitation.StaffRole, invitation.ExpiresAt, invitation.ExpiresAt <= Now(), invitation.AcceptedAt is not null, accountExists);
    }

    public async Task<AcceptResult> AcceptAsNewUserAsync(string token, string firstName, string lastName, string password, CancellationToken cancellationToken = default)
    {
        var invitation = await LoadUsableAsync(token, cancellationToken);
        if (invitation is null)
        {
            return AcceptResult.Failed("This invitation has expired or was already used. Ask for a new one.");
        }

        var normalizedEmail = invitation.Email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return AcceptResult.Failed("An account with this email already exists. Sign in to accept the invitation.");
        }

        return await InTransactionAsync(async () =>
        {
            var user = new AppUser
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                EmailConfirmed = true,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
            };

            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                return new AcceptResult(false, null, created.Errors.Select(e => e.Description).ToList());
            }

            await CompleteAsync(invitation, user.Id, cancellationToken);
            return new AcceptResult(true, user.Id, []);
        }, cancellationToken);
    }

    public async Task<AcceptResult> AcceptAsExistingUserAsync(string token, int userId, CancellationToken cancellationToken = default)
    {
        var invitation = await LoadUsableAsync(token, cancellationToken);
        if (invitation is null)
        {
            return AcceptResult.Failed("This invitation has expired or was already used. Ask for a new one.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return AcceptResult.Failed($"This invitation was sent to {invitation.Email}. Sign in with that account to accept it.");
        }

        if (!user.IsActive)
        {
            return AcceptResult.Failed("This account is deactivated.");
        }

        return await InTransactionAsync(async () =>
        {
            await CompleteAsync(invitation, user.Id, cancellationToken);
            return new AcceptResult(true, user.Id, []);
        }, cancellationToken);
    }

    public Task<List<PendingInvitation>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = Now();
        return db.Invitations
            .Where(i => i.AcceptedAt == null)
            .OrderBy(i => i.Email)
            .Select(i => new PendingInvitation(
                i.Id, i.Email, i.ClubRole, i.TeamSeason == null ? null : i.TeamSeason.DisplayName, i.StaffRole, i.ExpiresAt, i.ExpiresAt <= now))
            .ToListAsync(cancellationToken);
    }

    public async Task<InviteResult> RenewAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.Include(i => i.TeamSeason)
            .SingleOrDefaultAsync(i => i.Id == invitationId && i.AcceptedAt == null, cancellationToken)
            ?? throw new BusinessRuleException("That invitation wasn't found.");

        var token = NewToken();
        invitation.TokenHash = Hash(token);
        invitation.ExpiresAt = Now() + Lifetime;
        await db.SaveChangesAsync(cancellationToken);
        return new InviteResult(InviteOutcome.Invited, invitation.Email, token, invitation.TeamSeason?.DisplayName);
    }

    public async Task RevokeAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.SingleOrDefaultAsync(i => i.Id == invitationId && i.AcceptedAt == null, cancellationToken)
            ?? throw new BusinessRuleException("That invitation wasn't found.");

        db.Invitations.Remove(invitation);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CompleteAsync(Invitation invitation, int userId, CancellationToken cancellationToken)
    {
        var membership = await db.ClubMemberships.SingleOrDefaultAsync(m => m.UserId == userId, cancellationToken);
        if (membership is null)
        {
            db.ClubMemberships.Add(new ClubMembership { UserId = userId, Role = invitation.ClubRole });
        }
        else if (invitation.ClubRole == ClubRole.Admin && membership.Role == ClubRole.Member)
        {
            membership.Role = ClubRole.Admin;
        }

        if (invitation.TeamSeasonId is int teamSeasonId && invitation.StaffRole is StaffRole role)
        {
            var team = await db.TeamSeasons.Include(t => t.Season).SingleOrDefaultAsync(t => t.Id == teamSeasonId, cancellationToken);
            if (team is not null && !team.Season!.IsArchived
                && !await db.TeamSeasonStaff.AnyAsync(s => s.TeamSeasonId == teamSeasonId && s.UserId == userId, cancellationToken))
            {
                db.TeamSeasonStaff.Add(new TeamSeasonStaff { TeamSeasonId = teamSeasonId, UserId = userId, Role = role });
            }
        }

        invitation.AcceptedAt = Now();
        invitation.AcceptedUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TeamSeason> LoadTeamForStaffAsync(int teamSeasonId, StaffRole? staffRole, CancellationToken cancellationToken)
    {
        if (staffRole is null)
        {
            throw new BusinessRuleException("Choose a team role.");
        }

        var team = await db.TeamSeasons.Include(t => t.Season).SingleOrDefaultAsync(t => t.Id == teamSeasonId, cancellationToken)
            ?? throw new BusinessRuleException("That team wasn't found.");

        if (team.Season!.IsArchived)
        {
            throw new BusinessRuleException("Staff can't be added to an archived season.");
        }

        return team;
    }

    private async Task AddStaffAsync(TeamSeason team, int userId, StaffRole role, CancellationToken cancellationToken)
    {
        if (await db.TeamSeasonStaff.AnyAsync(s => s.TeamSeasonId == team.Id && s.UserId == userId, cancellationToken))
        {
            throw new BusinessRuleException($"That person is already on {team.DisplayName}'s staff.");
        }

        db.TeamSeasonStaff.Add(new TeamSeasonStaff { TeamSeasonId = team.Id, UserId = userId, Role = role });
    }

    private async Task<Invitation?> LoadUsableAsync(string token, CancellationToken cancellationToken)
    {
        var hash = Hash(token);
        var now = Now();
        return await db.Invitations.SingleOrDefaultAsync(i => i.TokenHash == hash && i.AcceptedAt == null && i.ExpiresAt > now, cancellationToken);
    }

    private async Task<AcceptResult> InTransactionAsync(Func<Task<AcceptResult>> work, CancellationToken cancellationToken)
    {
        // The retrying execution strategy only allows explicit transactions when it runs them itself.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var result = await work();
            if (result.Succeeded)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        });
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    internal static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim().ToLowerInvariant()))).ToLowerInvariant();
}
