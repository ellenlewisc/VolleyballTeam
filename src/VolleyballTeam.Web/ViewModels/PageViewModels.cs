using VolleyballTeam.Infrastructure.Services;

namespace VolleyballTeam.Web.ViewModels;

public record ChooseClubViewModel(IReadOnlyList<UserClubSummary> Clubs);

public record ClubDashboardViewModel(
    string ClubName, IReadOnlyList<TeamSeasonCard> Current, IReadOnlyList<TeamSeasonCard> Past, bool IsAdmin);

public record ErrorViewModel(int StatusCode, string RequestId);
