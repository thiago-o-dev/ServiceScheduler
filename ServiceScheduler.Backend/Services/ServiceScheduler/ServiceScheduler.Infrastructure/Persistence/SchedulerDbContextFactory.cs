using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceScheduler.Infrastructure.Persistence;

public class SchedulerDbContextFactory : IDesignTimeDbContextFactory<SchedulerDbContext>
{
    public SchedulerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SchedulerDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=scheduler_db;Username=postgres;Password=postgres",
            x => x.MigrationsAssembly(typeof(SchedulerDbContext).Assembly.FullName));

        return new SchedulerDbContext(optionsBuilder.Options);
    }
}
