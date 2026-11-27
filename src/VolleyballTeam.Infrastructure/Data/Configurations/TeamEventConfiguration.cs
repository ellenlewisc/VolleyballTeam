using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class TeamEventConfiguration : IEntityTypeConfiguration<TeamEvent>
{
    public const int TitleMaxLength = 100;
    public const int LocationMaxLength = 200;
    public const int OpponentMaxLength = 100;
    public const int NotesMaxLength = 2000;

    public void Configure(EntityTypeBuilder<TeamEvent> builder)
    {
        builder.Property(e => e.Title).HasMaxLength(TitleMaxLength);
        builder.Property(e => e.Location).HasMaxLength(LocationMaxLength);
        builder.Property(e => e.Opponent).HasMaxLength(OpponentMaxLength);
        builder.Property(e => e.Notes).HasMaxLength(NotesMaxLength);
        builder.Ignore(e => e.IsCancelled);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeamSeason)
            .WithMany(t => t.Events)
            .HasForeignKey(e => new { e.ClubId, e.TeamSeasonId })
            .HasPrincipalKey(t => new { t.ClubId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ClubId, e.TeamSeasonId, e.StartUtc });
    }
}
