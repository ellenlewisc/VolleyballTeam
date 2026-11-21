using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Data;

[Collection(SharedDatabaseDefinition.Name)]
public class AuditTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Adding_a_row_records_who_created_it()
    {
        var team = await AddTeamAsync(fixture.Alpha.CoachUserId);

        Assert.Equal(fixture.Alpha.CoachUserId, team.CreatedById);
        Assert.Equal(fixture.Alpha.CoachUserId, team.UpdatedById);
        Assert.NotEqual(default, team.CreatedAt);
    }

    [Fact]
    public async Task Editing_a_row_records_who_updated_it_and_keeps_the_creator()
    {
        var team = await AddTeamAsync(fixture.Alpha.CoachUserId);
        var editorId = await fixture.AddMemberAsync(fixture.Alpha.Club);

        await using (var db = fixture.CreateContextFor(fixture.Alpha.Club, editorId))
        {
            var loaded = await db.Teams.SingleAsync(t => t.Id == team.Id);
            loaded.Level = "Gold";
            loaded.CreatedById = editorId;
            await db.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContextFor(fixture.Alpha.Club);
        var saved = await verify.Teams.SingleAsync(t => t.Id == team.Id);
        Assert.Equal(fixture.Alpha.CoachUserId, saved.CreatedById);
        Assert.Equal(editorId, saved.UpdatedById);
        Assert.True(saved.UpdatedAt >= saved.CreatedAt);
    }

    [Fact]
    public async Task Removing_a_row_records_who_deleted_it()
    {
        var team = await AddTeamAsync(fixture.Alpha.CoachUserId);
        var removerId = await fixture.AddMemberAsync(fixture.Alpha.Club);

        await using (var db = fixture.CreateContextFor(fixture.Alpha.Club, removerId))
        {
            db.Teams.Remove(await db.Teams.SingleAsync(t => t.Id == team.Id));
            await db.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContextFor(fixture.Alpha.Club);
        var deleted = await verify.Teams.IgnoreQueryFilters([QueryFilters.SoftDelete]).SingleAsync(t => t.Id == team.Id);
        Assert.Equal(removerId, deleted.DeletedById);
        Assert.NotNull(deleted.DeletedAt);
    }

    [Fact]
    public async Task System_changes_have_no_user()
    {
        var team = await AddTeamAsync(actingUserId: null);

        Assert.Null(team.CreatedById);
    }

    private async Task<Team> AddTeamAsync(int? actingUserId)
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club, actingUserId);
        var team = new Team { Name = $"Audit {Guid.NewGuid():N}", Season = "2026-27" };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }
}
