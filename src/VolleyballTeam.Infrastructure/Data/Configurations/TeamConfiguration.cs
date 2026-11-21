using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(100);
        builder.Property(t => t.Season).HasMaxLength(20);
        builder.Property(t => t.AgeGroup).HasMaxLength(20);
        builder.Property(t => t.Level).HasMaxLength(50);

        // Lets child rows reference (ClubId, TeamId) so the database rejects cross-club links.
        builder.HasAlternateKey(t => new { t.ClubId, t.Id });

        builder.HasOne(t => t.Club)
            .WithMany(c => c.Teams)
            .HasForeignKey(t => t.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ClubId, t.Season, t.Name }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
