using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Security;

[Collection(SharedDatabaseDefinition.Name)]
public class SecurityHeadersTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Responses_carry_security_headers()
    {
        using var client = fixture.Web.CreateClientFor(userId: null);

        var response = await client.GetAsync("/account/login");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Contains("frame-ancestors 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }

    [Fact]
    public async Task Signed_in_pages_are_not_cached()
    {
        using var client = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        var response = await client.GetAsync("/alpha");

        Assert.True(response.Headers.CacheControl?.NoStore);
    }
}
