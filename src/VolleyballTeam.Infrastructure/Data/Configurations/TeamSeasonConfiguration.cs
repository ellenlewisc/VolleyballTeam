using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class TeamSeasonConfiguration : IEntityTypeConfiguration<TeamSeason>
{
    public void Configure(EntityTypeBuilder<TeamSeason> builder)
    {
        builder.Property(t => t.DisplayName).HasMaxLength(100);
        builder.Property(t => t.AgeGroup).HasMaxLength(20);
        builder.Property(t => t.Level).HasMaxLength(50);

        builder.HasAlternateKey(t => new { t.ClubId, t.Id });

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(t => t.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Team)
            .WithMany(t => t.Seasons)
            .HasForeignKey(t => new { t.ClubId, t.TeamId })
            .HasPrincipalKey(t => new { t.ClubId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Season)
            .WithMany(s => s.TeamSeasons)
            .HasForeignKey(t => new { t.ClubId, t.SeasonId })
            .HasPrincipalKey(s => new { s.ClubId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.RolledOverFrom)
            .WithMany()
            .HasForeignKey(t => new { t.ClubId, t.RolledOverFromId })
            .HasPrincipalKey(t => new { t.ClubId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ClubId, t.SeasonId, t.TeamId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
        builder.HasIndex(t => new { t.ClubId, t.SeasonId, t.DisplayName }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
