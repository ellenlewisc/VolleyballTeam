using System.Text.RegularExpressions;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Web.Tenancy;

public static partial class ClubTheme
{
    // Colors are written into an inline style, so anything that isn't a plain hex value is dropped.
    public static string CssVariables(Club? club)
    {
        if (club is null)
        {
            return "";
        }

        var primary = HexColor().IsMatch(club.PrimaryColor) ? $"--club-primary:{club.PrimaryColor};" : "";
        var secondary = HexColor().IsMatch(club.SecondaryColor) ? $"--club-secondary:{club.SecondaryColor};" : "";
        return primary + secondary;
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
