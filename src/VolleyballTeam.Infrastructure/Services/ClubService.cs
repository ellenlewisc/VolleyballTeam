using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure.Services;

public class ClubService(AppDbContext db, TenantContext tenant)
{
    public async Task UpdateSettingsAsync(string name, string timeZoneId, CancellationToken cancellationToken = default)
    {
        var clubId = tenant.ClubId ?? throw new TenantViolationException("Club settings need a current club.");
        var club = await db.Clubs.SingleAsync(c => c.Id == clubId, cancellationToken);

        // A club bootstrapped with a zone outside the list can keep it.
        if (timeZoneId != club.TimeZoneId && ClubTime.Choices.All(c => c.Id != timeZoneId))
        {
            throw new BusinessRuleException("Choose a time zone from the list.");
        }

        club.Name = name.Trim();
        club.TimeZoneId = timeZoneId;
        await db.SaveChangesAsync(cancellationToken);
    }
}
