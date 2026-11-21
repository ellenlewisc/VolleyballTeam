using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public class MemberStatusService(AppDbContext db, UserManager<AppUser> userManager, TimeProvider clock)
{
    public async Task<bool> SetAccountActiveAsync(int userId, bool active, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (user.IsActive == active)
        {
            return true;
        }

        user.DeactivatedAt = active ? null : clock.GetUtcNow().UtcDateTime;

        // A new stamp invalidates existing sign-in cookies at the next validation check.
        var result = await userManager.UpdateSecurityStampAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> SetClubMembershipActiveAsync(int userId, bool active, CancellationToken cancellationToken = default)
    {
        var membership = await db.ClubMemberships.SingleOrDefaultAsync(m => m.UserId == userId, cancellationToken);
        if (membership is null)
        {
            return false;
        }

        membership.DeactivatedAt = active ? null : membership.DeactivatedAt ?? clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
