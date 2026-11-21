namespace VolleyballTeam.Infrastructure.Config;

public class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public string ClubName { get; set; } = "";
    public string ClubSlug { get; set; } = "";
    public string TimeZoneId { get; set; } = "UTC";
    public string AdminEmail { get; set; } = "";
    public string AdminPassword { get; set; } = "";
    public string AdminFirstName { get; set; } = "";
    public string AdminLastName { get; set; } = "";
    public string? TeamName { get; set; }
    public string? TeamSeason { get; set; }
}
