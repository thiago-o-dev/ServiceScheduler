using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Domain.Entities;

namespace ServiceScheduler.Infrastructure.Persistence.Configurations;

public sealed class ServiceBundleConfiguration : IEntityTypeConfiguration<ServiceBundle>
{
    public void Configure(EntityTypeBuilder<ServiceBundle> builder)
    {
        builder.HasKey(sb => sb.Id);
        builder.Property(sb => sb.Name).IsRequired().HasMaxLength(150);
        builder.Property(sb => sb.Description).HasMaxLength(500);
        builder.Property(sb => sb.Value).HasPrecision(18, 2);

        // Map list of Guid ServiceIds (native uuid[] in postgres or converters if needed)
        builder.Property(sb => sb.ServiceIds);
    }
}
