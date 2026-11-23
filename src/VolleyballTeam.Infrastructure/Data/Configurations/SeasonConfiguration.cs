using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(20);
        builder.Ignore(s => s.IsArchived);

        builder.HasAlternateKey(s => new { s.ClubId, s.Id });

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(s => s.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ClubId, s.Name }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
