using Microsoft.AspNetCore.Authorization;
using VolleyballTeam.Infrastructure.Access;

namespace VolleyballTeam.Web.Authorization;

public record TeamSeasonResource(int Id);

public class TeamSeasonRequirement(AccessLevel minimum) : IAuthorizationRequirement
{
    public static readonly TeamSeasonRequirement View = new(AccessLevel.ReadOnly);
    public static readonly TeamSeasonRequirement Edit = new(AccessLevel.Edit);

    public AccessLevel Minimum { get; } = minimum;
}

public class TeamSeasonAccessHandler(TeamSeasonAccessService access)
    : AuthorizationHandler<TeamSeasonRequirement, TeamSeasonResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TeamSeasonRequirement requirement, TeamSeasonResource resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var result = await access.GetAsync(context.User.GetUserId(), resource.Id);
        if (result.Level >= requirement.Minimum)
        {
            context.Succeed(requirement);
        }
    }
}
