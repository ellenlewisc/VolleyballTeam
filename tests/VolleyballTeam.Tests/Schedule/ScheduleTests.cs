using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Schedule;

[Collection(SharedDatabaseDefinition.Name)]
public class ScheduleTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset October1 = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Times_are_entered_in_club_time_and_stored_as_utc()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var scope = Open("America/Toronto");
        var (db, schedule) = (scope.Db, scope.Schedule(October1));

        await schedule.CreateAsync(teamSeasonId, Practice(new DateTime(2027, 1, 12, 18, 0, 0)), repeatWeeklyUntil: null);

        var stored = await db.TeamEvents.SingleAsync(e => e.TeamSeasonId == teamSeasonId);
        Assert.Equal(new DateTime(2027, 1, 12, 23, 0, 0), stored.StartUtc);
        var row = Assert.Single(await schedule.GetEventsAsync(teamSeasonId, past: false));
        Assert.Equal(new DateTime(2027, 1, 12, 18, 0, 0), row.StartsLocal);
        Assert.Equal(new DateTime(2027, 1, 12, 20, 0, 0), row.EndsLocal);
    }

    [Fact]
    public async Task Weekly_repeats_keep_the_same_local_time_across_daylight_saving()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var scope = Open("America/Toronto");
        var (db, schedule) = (scope.Db, scope.Schedule(October1));

        var count = await schedule.CreateAsync(teamSeasonId, Practice(new DateTime(2026, 10, 20, 18, 0, 0)), new DateOnly(2026, 11, 10));

        Assert.Equal(4, count);
        var rows = await schedule.GetEventsAsync(teamSeasonId, past: false);
        Assert.All(rows, r => Assert.Equal(18, r.StartsLocal.Hour));
        var utcHours = await db.TeamEvents.Where(e => e.TeamSeasonId == teamSeasonId).OrderBy(e => e.StartUtc).Select(e => e.StartUtc.Hour).ToListAsync();
        Assert.Equal([22, 22, 23, 23], utcHours);
    }

    [Fact]
    public async Task An_event_must_end_after_it_starts()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var scope = Open();
        var db = scope.Db;
        var start = new DateTime(2027, 1, 12, 18, 0, 0);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            scope.Schedule(October1).CreateAsync(teamSeasonId, Practice(start) with { EndsLocal = start }, null));
    }

    [Fact]
    public async Task Repeating_is_capped()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var scope = Open();
        var db = scope.Db;

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            scope.Schedule(October1).CreateAsync(teamSeasonId, Practice(new DateTime(2026, 10, 20, 18, 0, 0)), new DateOnly(2028, 1, 1)));
        Assert.False(await db.TeamEvents.AnyAsync(e => e.TeamSeasonId == teamSeasonId));
    }

    [Fact]
    public async Task Cancelled_events_stay_listed_and_deleted_events_do_not()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var scope = Open();
        var db = scope.Db;
        var schedule = scope.Schedule(October1);
        await schedule.CreateAsync(teamSeasonId, Practice(new DateTime(2026, 10, 20, 18, 0, 0)), new DateOnly(2026, 10, 27));
        var rows = await schedule.GetEventsAsync(teamSeasonId, past: false);

        await schedule.SetCancelledAsync(teamSeasonId, rows[0].Id, cancelled: true);
        await schedule.DeleteAsync(teamSeasonId, rows[1].Id);

        var remaining = Assert.Single(await schedule.GetEventsAsync(teamSeasonId, past: false));
        Assert.Equal(rows[0].Id, remaining.Id);
        Assert.True(remaining.IsCancelled);
        Assert.True(await db.TeamEvents.IgnoreQueryFilters([QueryFilters.SoftDelete]).AnyAsync(e => e.Id == rows[1].Id && e.DeletedAt != null));
    }

    [Fact]
    public async Task An_event_stays_upcoming_until_it_ends()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var scope = Open();
        var db = scope.Db;
        await scope.Schedule(October1).CreateAsync(teamSeasonId, Practice(new DateTime(2026, 10, 20, 18, 0, 0)), null);

        var during = scope.Schedule(new DateTimeOffset(2026, 10, 20, 19, 0, 0, TimeSpan.Zero));
        var after = scope.Schedule(new DateTimeOffset(2026, 10, 20, 21, 0, 0, TimeSpan.Zero));

        Assert.Single(await during.GetEventsAsync(teamSeasonId, past: false));
        Assert.Empty(await after.GetEventsAsync(teamSeasonId, past: false));
        Assert.Single(await after.GetEventsAsync(teamSeasonId, past: true));
    }

    [Fact]
    public async Task Archived_schedules_cannot_change()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Archived);
        await using var scope = Open();
        var db = scope.Db;

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            scope.Schedule(October1).CreateAsync(teamSeasonId, Practice(new DateTime(2026, 10, 20, 18, 0, 0)), null));
    }

    [Fact]
    public async Task Opponent_is_kept_only_for_games()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var scope = Open();
        var db = scope.Db;
        var schedule = scope.Schedule(October1);
        var start = new DateTime(2026, 10, 24, 10, 0, 0);

        await schedule.CreateAsync(teamSeasonId, Practice(start) with { Type = EventType.Game, Opponent = " Rivals " }, null);
        await schedule.CreateAsync(teamSeasonId, Practice(start.AddDays(1)) with { Opponent = "Ignored" }, null);

        var rows = await schedule.GetEventsAsync(teamSeasonId, past: false);
        Assert.Equal("Rivals", rows[0].Opponent);
        Assert.Null(rows[1].Opponent);
    }

    private static EventInput Practice(DateTime startsLocal) =>
        new(EventType.Practice, null, startsLocal, startsLocal.AddHours(2), "Main gym", null, null);

    // The tenant's club carries the time zone, so tests can try one without changing the shared club row.
    private ClubScope Open(string timeZoneId = "UTC")
    {
        var club = fixture.Alpha.Club;
        var tenant = new TenantContext();
        tenant.SetClub(new Club { Id = club.Id, Name = club.Name, Slug = club.Slug, TimeZoneId = timeZoneId });
        return new ClubScope(fixture.CreateContext(tenant), tenant);
    }

    private sealed record ClubScope(AppDbContext Db, TenantContext Tenant) : IAsyncDisposable
    {
        public ScheduleService Schedule(DateTimeOffset now) => new(Db, Tenant, new FixedClock(now));

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private async Task<int> NewTeamSeasonAsync(SeasonStatus status)
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var teamSeason = new TeamSeason
        {
            Team = new Team { Name = $"Schedule {suffix}" },
            Season = new Season { Name = $"S{suffix}", Status = status },
            DisplayName = $"Schedule {suffix}",
        };
        db.TeamSeasons.Add(teamSeason);
        await db.SaveChangesAsync();
        return teamSeason.Id;
    }
}
