using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class TeamSeasonStaffConfiguration : IEntityTypeConfiguration<TeamSeasonStaff>
{
    public void Configure(EntityTypeBuilder<TeamSeasonStaff> builder)
    {
        builder.ToTable("TeamSeasonStaff");

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(s => s.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.TeamSeason)
            .WithMany(t => t.Staff)
            .HasForeignKey(s => new { s.ClubId, s.TeamSeasonId })
            .HasPrincipalKey(t => new { t.ClubId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ClubId, s.TeamSeasonId, s.UserId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
