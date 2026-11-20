using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Web.Authorization;

public class ClubMemberRequirement : IAuthorizationRequirement;

public class ClubMemberHandler(TenantContext tenant, AppDbContext db) : AuthorizationHandler<ClubMemberRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ClubMemberRequirement requirement)
    {
        if (tenant.ClubId is null || !int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        if (await db.ClubMemberships.AnyAsync(m => m.UserId == userId))
        {
            context.Succeed(requirement);
        }
    }
}
