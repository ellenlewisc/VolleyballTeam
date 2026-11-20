using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure.Data.Configurations;

public class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.Property(c => c.Slug).HasMaxLength(ClubSlug.MaxLength);
        builder.Property(c => c.LogoBlobPath).HasMaxLength(500);
        builder.Property(c => c.PrimaryColor).HasMaxLength(7);
        builder.Property(c => c.SecondaryColor).HasMaxLength(7);
        builder.Property(c => c.TimeZoneId).HasMaxLength(64);

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}
