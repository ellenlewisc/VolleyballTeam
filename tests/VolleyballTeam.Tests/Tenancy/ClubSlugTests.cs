using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Tests.Tenancy;

public class ClubSlugTests
{
    [Theory]
    [InlineData("demo-club")]
    [InlineData("lakeside")]
    [InlineData("club-2026")]
    public void Valid_slugs_are_accepted(string slug) => Assert.True(ClubSlug.IsValid(slug));

    [Theory]
    [InlineData("")]
    [InlineData("Demo-Club")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--dash")]
    [InlineData("has space")]
    [InlineData("account")]
    [InlineData("error")]
    public void Invalid_or_reserved_slugs_are_rejected(string slug) => Assert.False(ClubSlug.IsValid(slug));
}
