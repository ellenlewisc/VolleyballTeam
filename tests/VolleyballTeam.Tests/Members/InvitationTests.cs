using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Members;

[Collection(SharedDatabaseDefinition.Name)]
public class InvitationTests(DatabaseFixture fixture)
{
    private const string Password = "Serve-Receive-2026!";

    [Fact]
    public async Task New_person_accepts_and_becomes_a_member_and_staff()
    {
        var email = NewEmail();
        using var scope = AlphaScope();
        var invitations = scope.ServiceProvider.GetRequiredService<InvitationService>();

        var invite = await invitations.CreateAsync(new InviteRequest(email, "Sam", "Lee", ClubRole.Member, fixture.Alpha.TeamSeasonId, StaffRole.AssistantCoach));
        var result = await invitations.AcceptAsNewUserAsync(invite.Token!, "Sam", "Lee", Password);

        Assert.True(result.Succeeded, string.Join(" ", result.Errors));
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.ClubMemberships.AnyAsync(m => m.UserId == result.UserId && m.Role == ClubRole.Member));
        Assert.True(await db.TeamSeasonStaff.AnyAsync(s => s.UserId == result.UserId && s.TeamSeasonId == fixture.Alpha.TeamSeasonId && s.Role == StaffRole.AssistantCoach));
        Assert.True((await db.Users.SingleAsync(u => u.Id == result.UserId)).EmailConfirmed);
    }

    [Fact]
    public async Task An_invitation_link_works_only_once()
    {
        using var scope = AlphaScope();
        var invitations = scope.ServiceProvider.GetRequiredService<InvitationService>();
        var invite = await invitations.CreateAsync(new InviteRequest(NewEmail(), null, null, ClubRole.Member, null, null));
        await invitations.AcceptAsNewUserAsync(invite.Token!, "One", "Time", Password);

        var second = await invitations.AcceptAsNewUserAsync(invite.Token!, "Two", "Times", Password);

        Assert.False(second.Succeeded);
    }

    [Fact]
    public async Task Expired_and_revoked_invitations_cannot_be_accepted()
    {
        using var scope = AlphaScope();
        var invitations = scope.ServiceProvider.GetRequiredService<InvitationService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expired = await invitations.CreateAsync(new InviteRequest(NewEmail(), null, null, ClubRole.Member, null, null));
        var row = await db.Invitations.SingleAsync(i => i.Email == expired.Email);
        row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var revoked = await invitations.CreateAsync(new InviteRequest(NewEmail(), null, null, ClubRole.Member, null, null));
        await invitations.RevokeAsync((await db.Invitations.SingleAsync(i => i.Email == revoked.Email)).Id);

        Assert.False((await invitations.AcceptAsNewUserAsync(expired.Token!, "Too", "Late", Password)).Succeeded);
        Assert.False((await invitations.AcceptAsNewUserAsync(revoked.Token!, "Not", "Wanted", Password)).Succeeded);
    }

    [Fact]
    public async Task Inviting_a_current_member_to_a_team_adds_them_without_an_email()
    {
        var memberId = await fixture.AddMemberAsync(fixture.Alpha.Club);
        using var scope = AlphaScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = (await db.Users.SingleAsync(u => u.Id == memberId)).Email!;

        var result = await scope.ServiceProvider.GetRequiredService<InvitationService>()
            .CreateAsync(new InviteRequest(email, null, null, ClubRole.Member, fixture.Alpha.TeamSeasonId, StaffRole.TeamManager));

        Assert.Equal(InviteOutcome.AddedExistingMember, result.Outcome);
        Assert.Null(result.Token);
        Assert.True(await db.TeamSeasonStaff.AnyAsync(s => s.UserId == memberId && s.Role == StaffRole.TeamManager));
    }

    [Fact]
    public async Task Accepting_with_a_different_account_is_refused()
    {
        var otherUser = await fixture.AddMemberAsync(fixture.Alpha.Club);
        using var scope = AlphaScope();
        var invitations = scope.ServiceProvider.GetRequiredService<InvitationService>();
        var invite = await invitations.CreateAsync(new InviteRequest(NewEmail(), null, null, ClubRole.Admin, null, null));

        var result = await invitations.AcceptAsExistingUserAsync(invite.Token!, otherUser);

        Assert.False(result.Succeeded);
        Assert.Contains(invite.Email, result.Errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tokens_are_stored_only_as_hashes()
    {
        using var scope = AlphaScope();
        var invite = await scope.ServiceProvider.GetRequiredService<InvitationService>()
            .CreateAsync(new InviteRequest(NewEmail(), null, null, ClubRole.Member, null, null));
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await db.Invitations.SingleAsync(i => i.Email == invite.Email);

        Assert.NotEqual(invite.Token, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task Admins_cannot_change_themselves_or_the_owner()
    {
        var admin = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Admin);
        var owner = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Owner);
        using var scope = AlphaScope();
        var members = scope.ServiceProvider.GetRequiredService<MemberService>();

        await Assert.ThrowsAsync<BusinessRuleException>(() => members.ChangeRoleAsync(admin, admin, ClubRole.Member));
        await Assert.ThrowsAsync<BusinessRuleException>(() => members.SetActiveAsync(admin, owner, active: false));
    }

    private IServiceScope AlphaScope()
    {
        var scope = fixture.Web.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().SetClub(fixture.Alpha.Club);
        return scope;
    }

    private static string NewEmail() => $"invitee-{Guid.NewGuid():N}@alpha.test";
}
