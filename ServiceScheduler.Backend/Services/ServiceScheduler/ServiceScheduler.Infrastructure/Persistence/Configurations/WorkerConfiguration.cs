using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceScheduler.Infrastructure.Persistence.Configurations;

public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(150);
        builder.Property(w => w.Phone).IsRequired().HasMaxLength(30);
        builder.Property(w => w.Email).IsRequired().HasMaxLength(150);
        builder.Property(w => w.Cpf).IsRequired().HasMaxLength(14);
        builder.HasIndex(w => w.Email).IsUnique();

        // Map AvailablePeriods as owned entity collection
        builder.OwnsMany(w => w.AvailablePeriods, pb =>
        {
            pb.WithOwner();
            pb.Property<int>("Id");
            pb.HasKey("Id");
            pb.Property(p => p.DayOfWeek).IsRequired();
            pb.Property(p => p.StartTime).IsRequired();
            pb.Property(p => p.EndTime).IsRequired();
        });

        // Map UnavailablePeriods as owned entity collection
        builder.OwnsMany(w => w.UnavailablePeriods, up =>
        {
            up.WithOwner();
            up.Property<int>("Id");
            up.HasKey("Id");
            up.Property(u => u.Start).IsRequired();
            up.Property(u => u.End).IsRequired();
            up.Property(u => u.Reason).HasMaxLength(250);
        });
    }
}
