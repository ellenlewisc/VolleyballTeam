using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Web.Authorization;

public class ClubMemberRequirement(params ClubRole[] roles) : IAuthorizationRequirement
{
    public IReadOnlyCollection<ClubRole> Roles { get; } = roles;
}

public class ClubMemberHandler(TenantContext tenant, AppDbContext db) : AuthorizationHandler<ClubMemberRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ClubMemberRequirement requirement)
    {
        if (tenant.ClubId is null || !int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        var roles = requirement.Roles;

        // Checked on every request so deactivation takes effect immediately, not at the next cookie revalidation.
        if (await db.ClubMemberships.AnyAsync(m => m.UserId == userId
            && m.DeactivatedAt == null
            && m.User!.DeactivatedAt == null
            && (roles.Count == 0 || roles.Contains(m.Role))))
        {
            context.Succeed(requirement);
        }
    }
}
