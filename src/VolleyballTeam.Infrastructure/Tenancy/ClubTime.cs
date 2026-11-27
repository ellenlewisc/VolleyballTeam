namespace VolleyballTeam.Infrastructure.Tenancy;

public record TimeZoneChoice(string Id, string Label);

public static class ClubTime
{
    public static readonly IReadOnlyList<TimeZoneChoice> Choices =
    [
        new("America/St_Johns", "Newfoundland (St. John's)"),
        new("America/Halifax", "Atlantic (Halifax)"),
        new("America/Toronto", "Eastern (Toronto)"),
        new("America/New_York", "Eastern (New York)"),
        new("America/Winnipeg", "Central (Winnipeg)"),
        new("America/Chicago", "Central (Chicago)"),
        new("America/Regina", "Saskatchewan (Regina)"),
        new("America/Edmonton", "Mountain (Edmonton)"),
        new("America/Denver", "Mountain (Denver)"),
        new("America/Phoenix", "Arizona (Phoenix)"),
        new("America/Vancouver", "Pacific (Vancouver)"),
        new("America/Los_Angeles", "Pacific (Los Angeles)"),
        new("America/Anchorage", "Alaska (Anchorage)"),
        new("Pacific/Honolulu", "Hawaii (Honolulu)"),
        new("Europe/London", "UK (London)"),
        new("UTC", "UTC"),
    ];

    public static TimeZoneInfo Zone(string? timeZoneId) =>
        timeZoneId is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone) ? zone : TimeZoneInfo.Utc;

    public static string Label(string? timeZoneId) =>
        Choices.FirstOrDefault(c => c.Id == timeZoneId)?.Label ?? timeZoneId ?? "UTC";

    public static DateTime ToUtc(DateTime local, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        // A time inside the spring-forward gap doesn't exist, so it means the same clock time an hour later.
        if (zone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
    }

    public static DateTime ToLocal(DateTime utc, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);
}
