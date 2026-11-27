using System.ComponentModel.DataAnnotations;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;

namespace VolleyballTeam.Web.ViewModels;

public record TeamSeasonPageViewModel(
    TeamSeasonDetails Team,
    TeamSeasonAccess Access,
    IReadOnlyList<MemberOption> StaffCandidates,
    AddStaffViewModel AddStaff,
    IReadOnlyList<EventRow> NextEvents);

public class AddStaffViewModel
{
    [Required(ErrorMessage = "Choose a person.")]
    [Display(Name = "Person")]
    public int? UserId { get; set; }

    public StaffRole Role { get; set; } = StaffRole.AssistantCoach;
}

public class TeamSeasonEditViewModel
{
    public int Id { get; set; }
    public string SeasonName { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Team name this season")]
    public string DisplayName { get; set; } = "";

    [StringLength(20)]
    [Display(Name = "Age group")]
    public string? AgeGroup { get; set; }

    [StringLength(50)]
    public string? Level { get; set; }
}

public class SeasonFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Name", Prompt = "2027-28")]
    public string Name { get; set; } = "";

    [DataType(DataType.Date)]
    [Display(Name = "Starts on")]
    public DateOnly? StartsOn { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ends on")]
    public DateOnly? EndsOn { get; set; }
}

public class TeamCreateViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Team name", Prompt = "Black")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Choose a season.")]
    [Display(Name = "Season")]
    public int? SeasonId { get; set; }

    [StringLength(100)]
    [Display(Name = "Name this season", Prompt = "16U Black")]
    public string? DisplayName { get; set; }

    [StringLength(20)]
    [Display(Name = "Age group", Prompt = "16U")]
    public string? AgeGroup { get; set; }

    [StringLength(50)]
    public string? Level { get; set; }

    public IReadOnlyList<SeasonRow> Seasons { get; set; } = [];
}

public record SeasonsPageViewModel(IReadOnlyList<SeasonRow> Seasons);

public record TeamsPageViewModel(IReadOnlyList<TeamRow> Teams);

public class RolloverViewModel
{
    public RolloverView? Plan { get; set; }
    public int? SourceSeasonId { get; set; }
    public List<RolloverRowInput> Rows { get; set; } = [];
}

public class RolloverRowInput
{
    public int SourceTeamSeasonId { get; set; }
    public bool Include { get; set; }

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(20)]
    public string? AgeGroup { get; set; }

    [StringLength(50)]
    public string? Level { get; set; }

    public List<int> StaffUserIds { get; set; } = [];

    public List<int> PlayerIds { get; set; } = [];
}
