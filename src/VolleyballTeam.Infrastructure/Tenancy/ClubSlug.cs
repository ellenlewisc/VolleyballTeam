using System.Text.RegularExpressions;

namespace VolleyballTeam.Infrastructure.Tenancy;

public static partial class ClubSlug
{
    public const int MaxLength = 50;

    // Top-level route segments that a club slug would otherwise shadow.
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "account", "admin", "api", "css", "error", "home", "img", "js", "lib", "mcp", "privacy",
    };

    public static bool IsValid(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.Length <= MaxLength
        && SlugPattern().IsMatch(slug)
        && !Reserved.Contains(slug);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
