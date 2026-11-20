using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Web.Tenancy;

public class ClubSlugRouteConstraint : IRouteConstraint
{
    public const string Name = "clubslug";

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) && ClubSlug.IsValid(value as string);
}
