using System.Globalization;
using System.Security.Claims;
using VolleyballTeam.Infrastructure.Auditing;

namespace VolleyballTeam.Web.Security;

public class CurrentUserMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, CurrentUser currentUser)
    {
        if (int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
        {
            currentUser.Set(userId);
        }

        return next(context);
    }
}
