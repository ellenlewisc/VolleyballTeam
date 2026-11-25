using System.ComponentModel.DataAnnotations;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;

namespace VolleyballTeam.Web.ViewModels;

public record PlayersPageViewModel(IReadOnlyList<TeamOption> Teams, int? SelectedTeamSeasonId, bool IsAdmin);

public record RosterPageViewModel(TeamSeasonDetails Team, TeamSeasonAccess Access, IReadOnlyList<RosterRow> Rows, bool ShowLeft);

public class PlacementInput
{
    [Range(0, 99)]
    [Display(Name = "Jersey #")]
    public int? JerseyNumber { get; set; }

    [Display(Name = "Primary position")]
    public Position? PrimaryPosition { get; set; }

    [Display(Name = "Secondary position")]
    public Position? SecondaryPosition { get; set; }

    public RosterPlacement ToPlacement() => new(JerseyNumber, PrimaryPosition, SecondaryPosition);
}

public class PlayerNameInput : PlacementInput
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Range(1990, 2100)]
    [Display(Name = "Grad year")]
    public int? GradYear { get; set; }
}

public class RosterAddViewModel
{
    public TeamSeasonDetails? Team { get; set; }
    public string? Search { get; set; }
    public IReadOnlyList<PlayerMatch> Matches { get; set; } = [];
    public PlayerNameInput NewPlayer { get; set; } = new();
}

public class GuardianFormViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";

    [StringLength(40)]
    [Display(Name = "Relationship", Prompt = "Mother, Father, Guardian…")]
    public string? Relationship { get; set; }

    public GuardianInput ToInput() => new(FirstName, LastName, Email, Relationship);
}

public record GuardianListItem(GuardianRow Guardian, string? RemoveUrl);

public record GuardiansPanelViewModel(IReadOnlyList<GuardianListItem> Guardians, string? AddUrl, GuardianFormViewModel NewGuardian);

public class RosterEntryViewModel
{
    public TeamSeasonDetails? Team { get; set; }
    public RosterEntryDetails? Entry { get; set; }
    public PlayerNameInput Player { get; set; } = new();
    public GuardiansPanelViewModel? Guardians { get; set; }
}

public class RosterImportViewModel
{
    public TeamSeasonDetails? Team { get; set; }
    public ImportPreview? Preview { get; set; }
    public string? CsvText { get; set; }
}

public class PlayerEditInput
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Range(1990, 2100)]
    [Display(Name = "Grad year")]
    public int? GradYear { get; set; }
}

public class PlayerPageViewModel
{
    public PlayerDetails? Player { get; set; }
    public PlayerEditInput Edit { get; set; } = new();
    public GuardiansPanelViewModel? Guardians { get; set; }
}
