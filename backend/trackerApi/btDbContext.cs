namespace trackerApi.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using trackerApi.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Models.TrackingLogItem> TrackingLogs { get; set; }

    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Adding this in order to rename the table from TrackingLogs to simply
    /// TrackingLog, let alone all the other reasons I may end up needed this
    /// method.
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TrackingLogItem>()
            .ToTable("TrackingLog");
    }
}

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BETrackingDb;Username=postgres;Password=yourpassword");

        return new AppDbContext(optionsBuilder.Options);
    }
}
