using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Security;

[Collection(SharedDatabaseDefinition.Name)]
public class DeactivationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Deactivated_club_member_is_denied_the_club()
    {
        var userId = await fixture.AddMemberAsync(fixture.Alpha.Club);
        await SetClubMembershipActiveAsync(userId, active: false);
        using var client = fixture.Web.CreateClientFor(userId);

        var response = await client.GetAsync("/alpha");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/access-denied", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reactivated_club_member_gets_access_back()
    {
        var userId = await fixture.AddMemberAsync(fixture.Alpha.Club);
        await SetClubMembershipActiveAsync(userId, active: false);
        await SetClubMembershipActiveAsync(userId, active: true);
        using var client = fixture.Web.CreateClientFor(userId);

        var response = await client.GetAsync("/alpha");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Deactivated_club_member_does_not_see_the_club_in_their_list()
    {
        var userId = await fixture.AddMemberAsync(fixture.Alpha.Club);
        await SetClubMembershipActiveAsync(userId, active: false);

        using var scope = fixture.Web.Services.CreateScope();
        var clubs = await scope.ServiceProvider.GetRequiredService<UserClubService>().GetClubsForUserAsync(userId);

        Assert.Empty(clubs);
    }

    [Fact]
    public async Task Deactivated_account_is_denied_and_cannot_sign_in()
    {
        var userId = await fixture.AddMemberAsync(fixture.Alpha.Club);

        using (var scope = fixture.Web.Services.CreateScope())
        {
            Assert.True(await scope.ServiceProvider.GetRequiredService<MemberStatusService>().SetAccountActiveAsync(userId, active: false));
        }

        using var client = fixture.Web.CreateClientFor(userId);
        var response = await client.GetAsync("/alpha");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var verifyScope = fixture.Web.Services.CreateScope();
        var userManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var signInManager = verifyScope.ServiceProvider.GetRequiredService<SignInManager<AppUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.NotNull(user);
        Assert.False(await signInManager.CanSignInAsync(user));
    }

    private async Task SetClubMembershipActiveAsync(int userId, bool active)
    {
        using var scope = fixture.Web.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().SetClub(fixture.Alpha.Club);
        Assert.True(await scope.ServiceProvider.GetRequiredService<MemberStatusService>().SetClubMembershipActiveAsync(userId, active));
    }
}
