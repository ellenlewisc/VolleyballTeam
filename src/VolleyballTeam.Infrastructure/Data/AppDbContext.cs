using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure.Data.Configurations;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, TenantContext tenant)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options)
{
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();

    public int? CurrentClubId => tenant.ClubId;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ClubConfiguration());
        builder.ApplyConfiguration(new TeamConfiguration());
        builder.ApplyConfiguration(new ClubMembershipConfiguration());
        builder.ApplyConfiguration(new TeamMembershipConfiguration());
        builder.ApplyConfiguration(new AppUserConfiguration());

        // Every TenantModel must be listed here; TenantFilterCoverageTests fails the build if one is missed.
        ApplyTenantFilter<Team>(builder);
        ApplyTenantFilter<ClubMembership>(builder);
        ApplyTenantFilter<TeamMembership>(builder);
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : TenantModel
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.ClubId == CurrentClubId);
    }
}
