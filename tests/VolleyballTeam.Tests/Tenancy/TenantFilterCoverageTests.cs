using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Tests.Tenancy;

public class TenantFilterCoverageTests
{
    [Fact]
    public void Every_tenant_entity_has_a_query_filter()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;
        using var db = new AppDbContext(options, new TenantContext());

        var tenantTypes = db.Model.GetEntityTypes()
            .Where(t => typeof(TenantModel).IsAssignableFrom(t.ClrType))
            .ToList();

        Assert.NotEmpty(tenantTypes);
        Assert.All(tenantTypes, t => Assert.True(
            t.GetDeclaredQueryFilters().Count > 0,
            $"{t.ClrType.Name} is a TenantModel without a tenant query filter; add it in AppDbContext.OnModelCreating."));
    }
}
