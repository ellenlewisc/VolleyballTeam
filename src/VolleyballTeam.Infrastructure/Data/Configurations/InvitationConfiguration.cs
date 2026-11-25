using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.Property(i => i.Email).HasMaxLength(256);
        builder.Property(i => i.FirstName).HasMaxLength(100);
        builder.Property(i => i.LastName).HasMaxLength(100);
        builder.Property(i => i.TokenHash).HasMaxLength(64);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(i => i.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.TeamSeason)
            .WithMany()
            .HasForeignKey(i => new { i.ClubId, i.TeamSeasonId })
            .HasPrincipalKey(t => new { t.ClubId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(i => i.AcceptedUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => new { i.ClubId, i.Email });
    }
}
