using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Tenancy;

public class TenantContext
{
    public Club? Club { get; private set; }

    public int? ClubId => Club?.Id;

    public void SetClub(Club club)
    {
        ArgumentNullException.ThrowIfNull(club);

        if (Club is not null && Club.Id != club.Id)
        {
            throw new TenantViolationException($"This scope is already bound to club {Club.Id}.");
        }

        Club = club;
    }
}
