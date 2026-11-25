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
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<TeamSeason> TeamSeasons => Set<TeamSeason>();
    public DbSet<TeamSeasonStaff> TeamSeasonStaff => Set<TeamSeasonStaff>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<RosterEntry> RosterEntries => Set<RosterEntry>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

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
        builder.ApplyConfiguration(new SeasonConfiguration());
        builder.ApplyConfiguration(new TeamSeasonConfiguration());
        builder.ApplyConfiguration(new TeamSeasonStaffConfiguration());
        builder.ApplyConfiguration(new PlayerConfiguration());
        builder.ApplyConfiguration(new GuardianConfiguration());
        builder.ApplyConfiguration(new RosterEntryConfiguration());
        builder.ApplyConfiguration(new InvitationConfiguration());
        builder.ApplyConfiguration(new AppUserConfiguration());

        // Every entity must be listed here; QueryFilterCoverageTests fails the build if one is missed.
        ConfigureBaseModel<Club>(builder);
        ApplyTenantFilters<Team>(builder);
        ApplyTenantFilters<ClubMembership>(builder);
        ApplyTenantFilters<TeamMembership>(builder);
        ApplyTenantFilters<Season>(builder);
        ApplyTenantFilters<TeamSeason>(builder);
        ApplyTenantFilters<TeamSeasonStaff>(builder);
        ApplyTenantFilters<Player>(builder);
        ApplyTenantFilters<Guardian>(builder);
        ApplyTenantFilters<RosterEntry>(builder);
        ApplyTenantFilters<Invitation>(builder);
    }

    private static void ConfigureBaseModel<TEntity>(ModelBuilder builder)
        where TEntity : BaseModel
    {
        var entity = builder.Entity<TEntity>();
        entity.HasQueryFilter(QueryFilters.SoftDelete, e => e.DeletedAt == null);

        // Accounts are deactivated, never deleted, so these never need to cascade.
        entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UpdatedById).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.DeletedById).OnDelete(DeleteBehavior.NoAction);
    }

    private void ApplyTenantFilters<TEntity>(ModelBuilder builder)
        where TEntity : TenantModel
    {
        builder.Entity<TEntity>().HasQueryFilter(QueryFilters.Tenant, e => e.ClubId == CurrentClubId);
        ConfigureBaseModel<TEntity>(builder);
    }
}
