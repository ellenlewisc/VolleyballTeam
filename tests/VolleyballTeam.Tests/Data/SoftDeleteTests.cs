using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Data;

[Collection(SharedDatabaseDefinition.Name)]
public class SoftDeleteTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Removing_a_row_keeps_it_with_a_deleted_timestamp()
    {
        var userId = await fixture.AddMemberAsync(fixture.Alpha.Club, fixture.Alpha.TeamId);

        await using (var db = fixture.CreateContextFor(fixture.Alpha.Club))
        {
            var membership = await db.TeamMemberships.SingleAsync(m => m.UserId == userId);
            db.TeamMemberships.Remove(membership);
            await db.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContextFor(fixture.Alpha.Club);
        Assert.False(await verify.TeamMemberships.AnyAsync(m => m.UserId == userId));

        var kept = await verify.TeamMemberships
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .SingleAsync(m => m.UserId == userId);
        Assert.NotNull(kept.DeletedAt);
    }

    [Fact]
    public async Task Ignoring_the_soft_delete_filter_keeps_the_tenant_filter()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);

        var teams = await db.Teams.IgnoreQueryFilters([QueryFilters.SoftDelete]).ToListAsync();

        Assert.All(teams, t => Assert.Equal(fixture.Alpha.Club.Id, t.ClubId));
    }

    [Fact]
    public async Task A_removed_membership_can_be_added_again()
    {
        var userId = await fixture.AddMemberAsync(fixture.Alpha.Club, fixture.Alpha.TeamId);

        await using (var db = fixture.CreateContextFor(fixture.Alpha.Club))
        {
            db.TeamMemberships.Remove(await db.TeamMemberships.SingleAsync(m => m.UserId == userId));
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContextFor(fixture.Alpha.Club))
        {
            db.TeamMemberships.Add(new TeamMembership { UserId = userId, TeamId = fixture.Alpha.TeamId, Role = TeamRole.Parent });
            await db.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContextFor(fixture.Alpha.Club);
        Assert.Equal(1, await verify.TeamMemberships.CountAsync(m => m.UserId == userId));
    }

    [Fact]
    public async Task A_removed_team_disappears_from_queries()
    {
        var team = new Team { Name = $"Removed {Guid.NewGuid():N}", Season = "2026-27" };
        await using (var db = fixture.CreateContextFor(fixture.Alpha.Club))
        {
            db.Teams.Add(team);
            await db.SaveChangesAsync();
            db.Teams.Remove(team);
            await db.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContextFor(fixture.Alpha.Club);
        Assert.Null(await verify.Teams.SingleOrDefaultAsync(t => t.Id == team.Id));
    }
}
