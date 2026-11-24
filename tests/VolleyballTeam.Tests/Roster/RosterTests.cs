using System.Text;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Roster;

[Collection(SharedDatabaseDefinition.Name)]
public class RosterTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task New_player_joins_the_roster()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var roster = Roster(db);

        await roster.AddNewPlayerAsync(teamSeasonId, "Maya", "Chen", 2028, new RosterPlacement(7, Position.Setter, null));

        var row = Assert.Single(await roster.GetRosterAsync(teamSeasonId, includeLeft: false));
        Assert.Equal("Maya", row.FirstName);
        Assert.Equal(7, row.JerseyNumber);
        Assert.Equal(Position.Setter, row.PrimaryPosition);
    }

    [Fact]
    public async Task Jersey_numbers_are_unique_among_current_players()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var roster = Roster(db);
        await roster.AddNewPlayerAsync(teamSeasonId, "Maya", "Chen", null, new RosterPlacement(7, null, null));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            roster.AddNewPlayerAsync(teamSeasonId, "Ava", "Singh", null, new RosterPlacement(7, null, null)));

        Assert.Contains("Maya Chen", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Leaving_keeps_the_entry_and_frees_the_number()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var roster = Roster(db);
        var entryId = await roster.AddNewPlayerAsync(teamSeasonId, "Maya", "Chen", null, new RosterPlacement(7, null, null));

        await roster.LeaveAsync(teamSeasonId, entryId);
        await roster.AddNewPlayerAsync(teamSeasonId, "Ava", "Singh", null, new RosterPlacement(7, null, null));

        Assert.Single(await roster.GetRosterAsync(teamSeasonId, includeLeft: false));
        var all = await roster.GetRosterAsync(teamSeasonId, includeLeft: true);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, r => r.FirstName == "Maya" && r.LeftOn is not null);
    }

    [Fact]
    public async Task Archived_rosters_cannot_change()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Archived);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Roster(db).AddNewPlayerAsync(teamSeasonId, "Late", "Addition", null, new RosterPlacement(null, null, null)));
    }

    [Fact]
    public async Task Rollover_carries_the_ticked_players_with_their_numbers()
    {
        var sourceId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var roster = Roster(db);
        await roster.AddNewPlayerAsync(sourceId, "Maya", "Chen", null, new RosterPlacement(7, Position.Setter, null));
        await roster.AddNewPlayerAsync(sourceId, "Ava", "Singh", null, new RosterPlacement(9, null, null));
        var maya = await db.RosterEntries.Where(r => r.TeamSeasonId == sourceId && r.Player!.FirstName == "Maya").Select(r => r.PlayerId).SingleAsync();

        var target = new Season { Name = $"T{Guid.NewGuid():N}"[..20], Status = SeasonStatus.Upcoming };
        db.Seasons.Add(target);
        await db.SaveChangesAsync();

        var seasons = new SeasonService(db, roster);
        await seasons.RolloverAsync(target.Id, [new RolloverTeam(sourceId, "17U Next", "17U", null, [], [maya])]);

        var next = await db.TeamSeasons.SingleAsync(t => t.SeasonId == target.Id);
        var carried = Assert.Single(await roster.GetRosterAsync(next.Id, includeLeft: false));
        Assert.Equal("Maya", carried.FirstName);
        Assert.Equal(7, carried.JerseyNumber);
        Assert.Equal(Position.Setter, carried.PrimaryPosition);
    }

    [Fact]
    public async Task Csv_export_round_trips_through_import_into_another_team()
    {
        var sourceId = await NewTeamSeasonAsync(SeasonStatus.Active);
        var targetId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var roster = Roster(db);
        var players = new PlayerService(db);
        var csv = new RosterCsvService(db, roster);
        var lastName = $"Chen{Guid.NewGuid():N}"[..20];
        var entryId = await roster.AddNewPlayerAsync(sourceId, "Maya", lastName, 2028, new RosterPlacement(7, Position.Setter, Position.Libero));
        var entry = await roster.GetEntryAsync(sourceId, entryId);
        await players.AddGuardianAsync(entry!.PlayerId, new GuardianInput("Lin", "Chen", "Lin.Chen@Example.test", "Mother"));

        var text = Encoding.UTF8.GetString(await csv.ExportAsync(sourceId));
        var preview = await csv.PreviewAsync(targetId, text);
        var result = await csv.ImportAsync(targetId, text);

        Assert.Equal(ImportAction.ExistingPlayer, Assert.Single(preview.Rows).Action);
        Assert.Equal(1, result.Added);
        var imported = Assert.Single(await roster.GetRosterAsync(targetId, includeLeft: false));
        Assert.Equal(entry.PlayerId, imported.PlayerId);
        Assert.Equal(Position.Libero, imported.SecondaryPosition);
        Assert.Single(await players.GetGuardiansAsync(entry.PlayerId));
    }

    [Fact]
    public async Task Csv_preview_reports_problems_per_row()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var csv = new RosterCsvService(db, Roster(db));
        var family = $"Import{Guid.NewGuid():N}"[..20];
        var text = $"""
            First Name,Last Name,Jersey Number,Primary Position,Guardian1FirstName,Guardian1LastName,Guardian1Email
            Ava,{family},4,Outside Hitter,Raj,{family},raj@example.test
            Zoe,{family},4,Libero,,,
            ,{family},,,,,
            Ivy,{family},,Quarterback,Kim,{family},not-an-email
            """;

        var preview = await csv.PreviewAsync(teamSeasonId, text);

        Assert.Equal(4, preview.Rows.Count);
        Assert.Equal(ImportAction.NewPlayer, preview.Rows[0].Action);
        Assert.Equal(Position.OutsideHitter, preview.Rows[0].PrimaryPosition);
        Assert.Contains(preview.Rows[1].Errors, e => e.Contains("also used on line", StringComparison.Ordinal));
        Assert.Contains(preview.Rows[2].Errors, e => e.Contains("required", StringComparison.Ordinal));
        Assert.Equal(2, preview.Rows[3].Errors.Count);
        Assert.Equal(1, preview.ValidCount);
    }

    [Fact]
    public async Task Csv_export_escapes_values_that_spreadsheets_would_run_as_formulas()
    {
        var teamSeasonId = await NewTeamSeasonAsync(SeasonStatus.Active);
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var roster = Roster(db);
        await roster.AddNewPlayerAsync(teamSeasonId, "=HYPERLINK(\"http://evil.test\")", "Smith", null, new RosterPlacement(null, null, null));

        var text = Encoding.UTF8.GetString(await new RosterCsvService(db, roster).ExportAsync(teamSeasonId));

        Assert.DoesNotContain("\n=HYPERLINK", text.Replace("\"", "", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("'=HYPERLINK", text, StringComparison.Ordinal);
    }

    private static RosterService Roster(AppDbContext db) => new(db, TimeProvider.System);

    private async Task<int> NewTeamSeasonAsync(SeasonStatus status)
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var teamSeason = new TeamSeason
        {
            Team = new Team { Name = $"Roster {suffix}" },
            Season = new Season { Name = $"R{suffix}", Status = status },
            DisplayName = $"Roster {suffix}",
        };
        db.TeamSeasons.Add(teamSeason);
        await db.SaveChangesAsync();
        return teamSeason.Id;
    }
}
