using System.Security.Claims;

namespace VolleyballTeam.Web.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The current user has no id claim."),
            System.Globalization.CultureInfo.InvariantCulture);
}
