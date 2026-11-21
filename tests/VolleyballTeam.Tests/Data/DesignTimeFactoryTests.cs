using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Tests.Data;

public class DesignTimeFactoryTests
{
    [Fact]
    public void Creates_a_context_without_app_configuration()
    {
        using var db = new DesignTimeDbContextFactory().CreateDbContext([]);

        db.Database.SetConnectionString("Server=deploy-target;Database=volleyball");

        Assert.True(db.Database.IsSqlServer());
        Assert.Contains("deploy-target", db.Database.GetConnectionString(), StringComparison.Ordinal);
        Assert.NotEmpty(db.Database.GetMigrations());
    }
}
