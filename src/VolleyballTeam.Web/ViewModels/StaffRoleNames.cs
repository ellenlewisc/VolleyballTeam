using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Web.ViewModels;

public static class StaffRoleNames
{
    public static string Title(StaffRole? role) => role switch
    {
        StaffRole.HeadCoach => "Head coach",
        StaffRole.AssistantCoach => "Assistant coach",
        StaffRole.TeamManager => "Team manager",
        _ => "Staff",
    };

    public static string Describe(StaffRole? role) => Title(role).ToLowerInvariant();
}
