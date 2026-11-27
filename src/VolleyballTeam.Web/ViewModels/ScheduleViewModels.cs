using System.ComponentModel.DataAnnotations;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Web.ViewModels;

public record SchedulePageViewModel(TeamSeasonDetails Team, TeamSeasonAccess Access, IReadOnlyList<EventRow> Events, bool ShowPast);

public class EventFormViewModel : IValidatableObject
{
    public TeamSeasonDetails? Team { get; set; }
    public int? EventId { get; set; }

    public EventType Type { get; set; } = EventType.Practice;

    [StringLength(100)]
    [Display(Name = "Title", Prompt = "Optional, e.g. Provincials")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Choose when it starts.")]
    [Display(Name = "Starts")]
    public DateTime? StartsAt { get; set; }

    [Required(ErrorMessage = "Choose when it ends.")]
    [Display(Name = "Ends")]
    public DateTime? EndsAt { get; set; }

    [StringLength(200)]
    [Display(Name = "Location", Prompt = "Gym name and address")]
    public string? Location { get; set; }

    [StringLength(100)]
    public string? Opponent { get; set; }

    [StringLength(2000)]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Repeat weekly until")]
    public DateOnly? RepeatUntil { get; set; }

    public EventInput ToInput() => new(Type, Title, StartsAt!.Value, EndsAt!.Value, Location, Opponent, Notes);

    public static EventFormViewModel From(EventRow row) => new()
    {
        EventId = row.Id,
        Type = row.Type,
        Title = row.Title,
        StartsAt = row.StartsLocal,
        EndsAt = row.EndsLocal,
        Location = row.Location,
        Opponent = row.Opponent,
        Notes = row.Notes,
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAt is { } starts && EndsAt is { } ends && ends <= starts)
        {
            yield return new ValidationResult("The event has to end after it starts.", [nameof(EndsAt)]);
        }
    }
}

public class ClubSettingsViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Club name")]
    public string Name { get; set; } = "";

    [Required]
    [Display(Name = "Time zone")]
    public string TimeZoneId { get; set; } = "UTC";

    public IReadOnlyList<TimeZoneChoice> TimeZones { get; set; } = ClubTime.Choices;
}
