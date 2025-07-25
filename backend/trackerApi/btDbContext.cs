namespace trackerApi.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

using trackerApi.Models;

public class AppDbContext : DbContext
{
    private readonly ILogger<AppDbContext> _logger;

    public AppDbContext(DbContextOptions<AppDbContext> options, ILogger<AppDbContext> logger)
        : base(options)
    {
        _logger = logger;
        _logger.LogInformation("DbContext instance created");
    }

    protected AppDbContext()
    {
        _logger = new LoggerFactory().CreateLogger<AppDbContext>();
        _logger.LogInformation("DbContext instance created");
    }

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

        // TrackingLog table
        modelBuilder.Entity<TrackingLogItem>(entity =>
        {
            entity.ToTable("TrackingLog");  // Your existing table name configuration

            // Configure Id as uniqueidentifier with default value
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");

            // Configure foreign key relationship
            entity.HasOne(t => t.User)
                .WithMany(u => u.TrackingLogs)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // or DeleteBehavior.Restrict if you don't want automatic deletion

            // Configure EventDate with default
            entity.Property(e => e.EventDate)
                .HasDefaultValueSql("GETDATE()");

            // For SQL Server, we don't need to specify column type as it will use datetime2

            // Configure boolean defaults
            entity.Property(e => e.Accident)
                .HasDefaultValue(false);

            entity.Property(e => e.ChangePadOrUnderware)
                .HasDefaultValue(false);

            entity.Property(e => e.AwokeFromSleep)
                .HasDefaultValue(false);

            // Configure numeric fields with defaults and constraints
            entity.Property(e => e.LeakAmount)
                .HasDefaultValue(1)
                .HasAnnotation("CheckConstraint", "LeakAmount >= 0 AND LeakAmount <= 3");

            entity.Property(e => e.Urgency)
                .HasDefaultValue(1)
                .HasAnnotation("CheckConstraint", "Urgency >= 0 AND Urgency <= 4");

            entity.Property(e => e.PainLevel)
                .HasDefaultValue(1)
                .HasAnnotation("CheckConstraint", "PainLevel >= 0 AND PainLevel <= 10");
        });

        // Users table
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            // Configure Id as uniqueidentifier with default value
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");

            // Configure default values for timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnUpdate(); // Update on row modification

            // for IsAdmin
            entity.Property(e => e.IsAdmin)
                .HasDefaultValue(false);  // Sets default value to false for new users
        });
    }

    public override void Dispose()
    {
        _logger.LogInformation("DbContext instance disposing <----<----");

        GC.SuppressFinalize(this);

        base.Dispose();
    }
}

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private readonly IConfiguration? _configuration;
    private readonly ILogger<AppDbContext>? _logger;

    /// <summary>
    /// We need this parameterless constructor for migrations.
    /// </summary>
    public AppDbContextFactory()
    {
        _configuration = null;

        // Create a default logger
        _logger = new LoggerFactory().CreateLogger<AppDbContext>();
    }

    public AppDbContextFactory(ILogger<AppDbContext> logger, IConfiguration? configuration = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public AppDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = _configuration ?? new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddUserSecrets<AppDbContextFactory>()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found in configuration. " +
                "Ensure DefaultConnection is set in appsettings.json.");
        }

        // Get the SQL password from configuration or environment variables
        var sqlPassword = configuration["SqlPassword"] ??
                         configuration["SQL_PASSWORD"] ??
                         Environment.GetEnvironmentVariable("SqlPassword") ??
                         Environment.GetEnvironmentVariable("SQL_PASSWORD");

        if (string.IsNullOrEmpty(sqlPassword))
        {
            throw new InvalidOperationException(
                "SQL password not found in configuration. " +
                "Ensure SqlPassword is set in user secrets or SQL_PASSWORD in environment variables.");
        }

        // Replace the placeholder with actual password
        connectionString = connectionString.Replace("${SqlPassword}", sqlPassword);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        // Create a default logger if _logger is null
        var logger = _logger ?? new LoggerFactory().CreateLogger<AppDbContext>();

        return new AppDbContext(optionsBuilder.Options, logger);
    }
}

