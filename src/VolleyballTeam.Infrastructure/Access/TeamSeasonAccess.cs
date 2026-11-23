using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Infrastructure.Access;

public enum AccessLevel
{
    None,
    ReadOnly,
    Edit,
}

public record TeamSeasonAccess(AccessLevel Level, StaffRole? StaffRole, bool IsClubAdmin)
{
    public static readonly TeamSeasonAccess Denied = new(AccessLevel.None, null, false);

    public bool CanView => Level != AccessLevel.None;
    public bool CanEdit => Level == AccessLevel.Edit;

    // clubRole is null when the user has no active membership in the club or their account is deactivated.
    public static TeamSeasonAccess Decide(ClubRole? clubRole, SeasonStatus seasonStatus, StaffRole? staffRole)
    {
        if (clubRole is null)
        {
            return Denied;
        }

        var archived = seasonStatus == SeasonStatus.Archived;

        if (clubRole is ClubRole.Owner or ClubRole.Admin)
        {
            return new(archived ? AccessLevel.ReadOnly : AccessLevel.Edit, staffRole, true);
        }

        return staffRole switch
        {
            null => Denied,
            Domain.Enums.StaffRole.HeadCoach when archived => new(AccessLevel.ReadOnly, staffRole, false),
            _ when archived => Denied,
            _ => new(AccessLevel.Edit, staffRole, false),
        };
    }
}
