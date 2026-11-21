using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Web.Tenancy;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string RouteKey = "club";

    public async Task InvokeAsync(HttpContext context, TenantContext tenant, AppDbContext db)
    {
        if (context.GetRouteValue(RouteKey) is string slug)
        {
            var club = await db.Clubs
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Slug == slug, context.RequestAborted);

            if (club is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            tenant.SetClub(club);
        }

        await next(context);
    }
}
