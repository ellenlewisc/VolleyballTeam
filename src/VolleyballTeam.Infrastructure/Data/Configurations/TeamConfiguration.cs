using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(100);

        // Phase 0 columns now living on TeamSeason; nullable shadows keep the data until the cleanup migration.
        builder.Property<string?>("Season").HasMaxLength(20);
        builder.Property<string?>("AgeGroup").HasMaxLength(20);
        builder.Property<string?>("Level").HasMaxLength(50);

        // Lets child rows reference (ClubId, TeamId) so the database rejects cross-club links.
        builder.HasAlternateKey(t => new { t.ClubId, t.Id });

        builder.HasOne(t => t.Club)
            .WithMany(c => c.Teams)
            .HasForeignKey(t => t.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ClubId, t.Name }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
