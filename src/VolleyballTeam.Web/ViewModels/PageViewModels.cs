using VolleyballTeam.Infrastructure.Services;

namespace VolleyballTeam.Web.ViewModels;

public record ChooseClubViewModel(IReadOnlyList<UserClubSummary> Clubs);

public record ClubDashboardViewModel(string ClubName, IReadOnlyList<TeamSummary> Teams);

public record ErrorViewModel(int StatusCode, string RequestId);
