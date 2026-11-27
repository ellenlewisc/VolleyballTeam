using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure.Services;

public record EventInput(
    EventType Type, string? Title, DateTime StartsLocal, DateTime EndsLocal, string? Location, string? Opponent, string? Notes);

public record EventRow(
    int Id, EventType Type, string? Title, DateTime StartsLocal, DateTime EndsLocal, string? Location, string? Opponent, string? Notes, bool IsCancelled);

public class ScheduleService(AppDbContext db, TenantContext tenant, TimeProvider clock)
{
    public const int MaxOccurrences = 52;
    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(14);

    public async Task<List<EventRow>> GetEventsAsync(int teamSeasonId, bool past, int? take = null, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var forTeam = db.TeamEvents.AsNoTracking().Where(e => e.TeamSeasonId == teamSeasonId);

        // An event stays upcoming until it ends, so today's practice is still listed while it's running.
        IQueryable<TeamEvent> query = past
            ? forTeam.Where(e => e.EndUtc < now).OrderByDescending(e => e.StartUtc)
            : forTeam.Where(e => e.EndUtc >= now).OrderBy(e => e.StartUtc);

        if (take is int limit)
        {
            query = query.Take(limit);
        }

        var zone = Zone();
        return (await query.ToListAsync(cancellationToken)).Select(e => ToRow(e, zone)).ToList();
    }

    public async Task<EventRow?> GetEventAsync(int teamSeasonId, int eventId, CancellationToken cancellationToken = default)
    {
        var teamEvent = await db.TeamEvents.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == eventId && e.TeamSeasonId == teamSeasonId, cancellationToken);

        return teamEvent is null ? null : ToRow(teamEvent, Zone());
    }

    public async Task<int> CreateAsync(int teamSeasonId, EventInput input, DateOnly? repeatWeeklyUntil, CancellationToken cancellationToken = default)
    {
        await db.EnsureTeamSeasonEditableAsync(teamSeasonId, cancellationToken);
        Validate(input);

        var duration = input.EndsLocal - input.StartsLocal;
        var zone = Zone();
        var starts = Occurrences(input.StartsLocal, repeatWeeklyUntil);

        foreach (var start in starts)
        {
            var teamEvent = new TeamEvent { TeamSeasonId = teamSeasonId };
            Apply(teamEvent, input with { StartsLocal = start, EndsLocal = start + duration }, zone);
            db.TeamEvents.Add(teamEvent);
        }

        await db.SaveChangesAsync(cancellationToken);
        return starts.Count;
    }

    public async Task UpdateAsync(int teamSeasonId, int eventId, EventInput input, CancellationToken cancellationToken = default)
    {
        await db.EnsureTeamSeasonEditableAsync(teamSeasonId, cancellationToken);
        Validate(input);

        var teamEvent = await LoadAsync(teamSeasonId, eventId, cancellationToken);
        Apply(teamEvent, input, Zone());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetCancelledAsync(int teamSeasonId, int eventId, bool cancelled, CancellationToken cancellationToken = default)
    {
        await db.EnsureTeamSeasonEditableAsync(teamSeasonId, cancellationToken);

        var teamEvent = await LoadAsync(teamSeasonId, eventId, cancellationToken);
        teamEvent.CancelledAt = cancelled ? clock.GetUtcNow().UtcDateTime : null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int teamSeasonId, int eventId, CancellationToken cancellationToken = default)
    {
        await db.EnsureTeamSeasonEditableAsync(teamSeasonId, cancellationToken);

        db.TeamEvents.Remove(await LoadAsync(teamSeasonId, eventId, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(EventInput input)
    {
        if (input.EndsLocal <= input.StartsLocal)
        {
            throw new BusinessRuleException("The event has to end after it starts.");
        }

        if (input.EndsLocal - input.StartsLocal > MaxDuration)
        {
            throw new BusinessRuleException("An event can't last more than two weeks. Check the dates.");
        }
    }

    private static List<DateTime> Occurrences(DateTime firstStart, DateOnly? repeatWeeklyUntil)
    {
        if (repeatWeeklyUntil is not DateOnly until)
        {
            return [firstStart];
        }

        if (until < DateOnly.FromDateTime(firstStart))
        {
            throw new BusinessRuleException("The repeat end date is before the first event.");
        }

        var starts = new List<DateTime>();
        for (var start = firstStart; DateOnly.FromDateTime(start) <= until; start = start.AddDays(7))
        {
            if (starts.Count == MaxOccurrences)
            {
                throw new BusinessRuleException($"That would create more than {MaxOccurrences} events. Choose an earlier end date.");
            }

            starts.Add(start);
        }

        return starts;
    }

    private static void Apply(TeamEvent teamEvent, EventInput input, TimeZoneInfo zone)
    {
        teamEvent.Type = input.Type;
        teamEvent.Title = TeamService.Blank(input.Title);
        teamEvent.StartUtc = ClubTime.ToUtc(input.StartsLocal, zone);
        teamEvent.EndUtc = ClubTime.ToUtc(input.EndsLocal, zone);
        teamEvent.Location = TeamService.Blank(input.Location);
        teamEvent.Opponent = input.Type == EventType.Game ? TeamService.Blank(input.Opponent) : null;
        teamEvent.Notes = TeamService.Blank(input.Notes);
    }

    private static EventRow ToRow(TeamEvent e, TimeZoneInfo zone) => new(
        e.Id, e.Type, e.Title, ClubTime.ToLocal(e.StartUtc, zone), ClubTime.ToLocal(e.EndUtc, zone), e.Location, e.Opponent, e.Notes, e.IsCancelled);

    private async Task<TeamEvent> LoadAsync(int teamSeasonId, int eventId, CancellationToken cancellationToken) =>
        await db.TeamEvents.SingleOrDefaultAsync(e => e.Id == eventId && e.TeamSeasonId == teamSeasonId, cancellationToken)
            ?? throw new BusinessRuleException("That event wasn't found.");

    private TimeZoneInfo Zone() => ClubTime.Zone(tenant.Club?.TimeZoneId);
}
