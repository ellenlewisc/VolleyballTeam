namespace VolleyballTeam.Infrastructure.Config;

public class DevSeedOptions
{
    public const string SectionName = "DevSeed";

    public string ClubName { get; set; } = "Demo Volleyball Club";
    public string ClubSlug { get; set; } = "demo-club";
    public string TimeZoneId { get; set; } = "UTC";
    public string AdminEmail { get; set; } = "";
    public string AdminPassword { get; set; } = "";
    public string AdminFirstName { get; set; } = "Demo";
    public string AdminLastName { get; set; } = "Coach";
}
