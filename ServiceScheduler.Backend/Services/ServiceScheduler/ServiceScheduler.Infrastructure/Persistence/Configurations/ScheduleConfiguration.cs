using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceScheduler.Infrastructure.Persistence.Configurations;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CustomerId).IsRequired();
        builder.Property(s => s.WorkerId).IsRequired();
        builder.Property(s => s.ScheduledAt).IsRequired();
        builder.Property(s => s.Duration).IsRequired();
        builder.Property(s => s.BruteValue).HasPrecision(18, 2);
        builder.Property(s => s.CustomNetValue).HasPrecision(18, 2);
        builder.Property(s => s.Status).IsRequired().HasConversion<int>();

        // Map Services (ScheduledService owned collection)
        builder.OwnsMany(s => s.Services, sb =>
        {
            sb.WithOwner();
            sb.Property<int>("Id");
            sb.HasKey("Id");
            sb.Property(x => x.ServiceId).IsRequired();
            sb.Property(x => x.Status).IsRequired().HasConversion<int>();
        });
    }
}
