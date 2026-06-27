using BuildingBlocks.Persistence.EntityFramework;
using BuildingBlocks.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Domain.Entities;
using System;

namespace ServiceScheduler.Infrastructure.Persistence;

public class SchedulerDbContext : AppDbContext
{
    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceBundle> ServiceBundles => Set<ServiceBundle>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulerDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
