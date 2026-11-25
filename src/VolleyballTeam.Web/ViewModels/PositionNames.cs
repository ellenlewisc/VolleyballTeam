using VolleyballTeam.Domain.Enums;

namespace VolleyballTeam.Web.ViewModels;

public static class PositionNames
{
    public static string Title(Position position) => position switch
    {
        Position.OutsideHitter => "Outside hitter",
        Position.MiddleBlocker => "Middle blocker",
        Position.DefensiveSpecialist => "Defensive specialist",
        _ => position.ToString(),
    };
}
