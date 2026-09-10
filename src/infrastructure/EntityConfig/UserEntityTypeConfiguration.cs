using domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace infrastructure.EntityConfig;

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(x => x.DisplayName)
            .HasMaxLength(100);
    }
}