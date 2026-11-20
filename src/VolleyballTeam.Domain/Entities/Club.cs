namespace VolleyballTeam.Domain.Entities;

public class Club : BaseModel
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoBlobPath { get; set; }
    public string PrimaryColor { get; set; } = "#1d4ed8";
    public string SecondaryColor { get; set; } = "#f59e0b";
    public string TimeZoneId { get; set; } = "UTC";

    public ICollection<Team> Teams { get; set; } = [];
}
