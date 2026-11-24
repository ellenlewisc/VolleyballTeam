using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Ignore(p => p.FullName);

        builder.HasAlternateKey(p => new { p.ClubId, p.Id });

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(p => p.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ClubId, p.LastName, p.FirstName });
    }
}

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.Property(g => g.FirstName).HasMaxLength(100);
        builder.Property(g => g.LastName).HasMaxLength(100);
        builder.Property(g => g.Email).HasMaxLength(256);
        builder.Property(g => g.Relationship).HasMaxLength(40);
        builder.Property(g => g.UnsubscribeToken).HasMaxLength(32);
        builder.Ignore(g => g.FullName);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(g => g.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Player)
            .WithMany(p => p.Guardians)
            .HasForeignKey(g => new { g.ClubId, g.PlayerId })
            .HasPrincipalKey(p => new { p.ClubId, p.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => g.UnsubscribeToken).IsUnique();
        builder.HasIndex(g => new { g.ClubId, g.PlayerId, g.Email }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}

public class RosterEntryConfiguration : IEntityTypeConfiguration<RosterEntry>
{
    public void Configure(EntityTypeBuilder<RosterEntry> builder)
    {
        builder.Ignore(r => r.IsActive);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(r => r.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TeamSeason)
            .WithMany(t => t.Roster)
            .HasForeignKey(r => new { r.ClubId, r.TeamSeasonId })
            .HasPrincipalKey(t => new { t.ClubId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Player)
            .WithMany(p => p.RosterEntries)
            .HasForeignKey(r => new { r.ClubId, r.PlayerId })
            .HasPrincipalKey(p => new { p.ClubId, p.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // A player and a jersey number can each appear once among the current (not left, not deleted) entries.
        builder.HasIndex(r => new { r.ClubId, r.TeamSeasonId, r.PlayerId })
            .IsUnique()
            .HasFilter("[LeftOn] IS NULL AND [DeletedAt] IS NULL");
        builder.HasIndex(r => new { r.ClubId, r.TeamSeasonId, r.JerseyNumber })
            .IsUnique()
            .HasFilter("[LeftOn] IS NULL AND [DeletedAt] IS NULL AND [JerseyNumber] IS NOT NULL");
    }
}
