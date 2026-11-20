using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class TeamMembershipConfiguration : IEntityTypeConfiguration<TeamMembership>
{
    public void Configure(EntityTypeBuilder<TeamMembership> builder)
    {
        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(m => m.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Team)
            .WithMany(t => t.Memberships)
            .HasForeignKey(m => new { m.ClubId, m.TeamId })
            .HasPrincipalKey(t => new { t.ClubId, t.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ClubId, m.TeamId, m.UserId }).IsUnique();
    }
}
