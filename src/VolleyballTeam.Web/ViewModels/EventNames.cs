using System.Globalization;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Services;

namespace VolleyballTeam.Web.ViewModels;

public static class EventNames
{
    public static string Heading(EventRow row) =>
        row.Title ?? (row.Type == EventType.Game && row.Opponent is not null ? $"Game vs {row.Opponent}" : row.Type.ToString());

    public static string When(EventRow row)
    {
        var culture = CultureInfo.InvariantCulture;
        var start = row.StartsLocal;
        var end = row.EndsLocal;

        return start.Date == end.Date
            ? $"{start.ToString("ddd, MMM d", culture)} · {start.ToString("h:mm tt", culture)} – {end.ToString("h:mm tt", culture)}"
            : $"{start.ToString("ddd, MMM d, h:mm tt", culture)} – {end.ToString("ddd, MMM d, h:mm tt", culture)}";
    }

    public static string Month(DateTime local) => local.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public static string BadgeClasses(EventType type) => type switch
    {
        EventType.Practice => "bg-sky-100 text-sky-800",
        EventType.Game => "bg-green-100 text-green-800",
        EventType.Tournament => "bg-violet-100 text-violet-800",
        _ => "bg-slate-200 text-slate-700",
    };
}
