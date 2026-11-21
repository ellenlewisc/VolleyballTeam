using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using VolleyballTeam.Domain;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Tests.Tenancy;

public class QueryFilterCoverageTests
{
    [Fact]
    public void Every_tenant_entity_has_a_tenant_filter()
    {
        var types = EntityTypesOf<TenantModel>();

        Assert.NotEmpty(types);
        Assert.All(types, t => Assert.True(
            t.FindDeclaredQueryFilter(QueryFilters.Tenant) is not null,
            $"{t.ClrType.Name} is a TenantModel without a tenant query filter; add it in AppDbContext.OnModelCreating."));
    }

    [Fact]
    public void Every_entity_has_a_soft_delete_filter()
    {
        var types = EntityTypesOf<BaseModel>();

        Assert.NotEmpty(types);
        Assert.All(types, t => Assert.True(
            t.FindDeclaredQueryFilter(QueryFilters.SoftDelete) is not null,
            $"{t.ClrType.Name} is a BaseModel without a soft-delete query filter; add it in AppDbContext.OnModelCreating."));
    }

    private static List<IEntityType> EntityTypesOf<TBase>()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;
        using var db = new AppDbContext(options, new TenantContext());

        return db.Model.GetEntityTypes()
            .Where(t => typeof(TBase).IsAssignableFrom(t.ClrType))
            .ToList();
    }
}
