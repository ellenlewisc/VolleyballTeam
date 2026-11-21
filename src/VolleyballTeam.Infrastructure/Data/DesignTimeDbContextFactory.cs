using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure.Data;

// Lets dotnet ef and the migration bundle build the context without starting the web host or its config.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public const string ConnectionEnvironmentVariable = "VBT_DESIGN_CONNECTION";
    private const string LocalDb = @"Server=(localdb)\MSSQLLocalDB;Database=VolleyballTeam;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable) ?? LocalDb;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .Options;

        return new AppDbContext(options, new TenantContext());
    }
}
