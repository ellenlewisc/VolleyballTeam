using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Schedule;

[Collection(SharedDatabaseDefinition.Name)]
public class SeasonEditTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Season_name_and_dates_can_be_edited()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var seasons = Seasons(db);
        var id = await seasons.CreateSeasonAsync(UniqueName(), null, null);
        var name = UniqueName();

        await seasons.UpdateSeasonAsync(id, name, new DateOnly(2027, 9, 1), new DateOnly(2028, 6, 30));

        var season = await seasons.GetSeasonAsync(id);
        Assert.Equal(name, season!.Name);
        Assert.Equal(new DateOnly(2027, 9, 1), season.StartsOn);
    }

    [Fact]
    public async Task Season_names_stay_unique_and_archived_seasons_are_read_only()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var seasons = Seasons(db);
        var taken = UniqueName();
        await seasons.CreateSeasonAsync(taken, null, null);
        var id = await seasons.CreateSeasonAsync(UniqueName(), null, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => seasons.UpdateSeasonAsync(id, taken, null, null));

        await seasons.SetStatusAsync(id, SeasonStatus.Active);
        await seasons.SetStatusAsync(id, SeasonStatus.Archived);
        await Assert.ThrowsAsync<BusinessRuleException>(() => seasons.UpdateSeasonAsync(id, UniqueName(), null, null));
    }

    private static SeasonService Seasons(AppDbContext db) => new(db, new RosterService(db, TimeProvider.System));

    private static string UniqueName() => $"E{Guid.NewGuid():N}"[..20];
}
