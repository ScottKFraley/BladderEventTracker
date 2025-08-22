namespace trackerApi.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


using trackerApi.Models;
using trackerApi.Services;

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

    public DbSet<TrackingLogItem> TrackingLogs { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    /// <summary>
    /// Adding this in order to rename the table from TrackingLogs to simply
    /// TrackingLog, among the other reasons this method is needed.
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TrackingLog table
        modelBuilder.Entity<TrackingLogItem>(entity =>
        {
            entity.ToTable("TrackingLog", ck => {
                ck.HasCheckConstraint("CK_TrackingLog_LeakAmount", "LeakAmount >= 0 AND LeakAmount <= 3");
                ck.HasCheckConstraint("CK_TrackingLog_Urgency", "Urgency >= 0 AND Urgency <= 4");
                ck.HasCheckConstraint("CK_TrackingLog_PainLevel", "PainLevel >= 0 AND PainLevel <= 10");
            });

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

            // Configure numeric fields - let database defaults handle default values
            // Remove HasDefaultValue() to allow explicit 0 values to be saved
        });

        // Users table
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            // Configure Id as uniqueidentifier with default value
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");

            // Configure Username
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsRequired();

            // Index for username lookups
            entity.HasIndex(e => e.Username)
                .HasDatabaseName("IX_Users_Username")
                .IsUnique();

            // Configure PasswordHash
            entity.Property(e => e.PasswordHash)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

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

        // RefreshTokens table
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            // Configure Id as uniqueidentifier with default value
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");

            // Configure Token
            entity.Property(e => e.Token)
                .HasMaxLength(500)
                .IsRequired();

            // Configure foreign key relationship
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure default values for timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            // Configure IsRevoked default
            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false);

            // Configure DeviceInfo
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(200);

            // Create indexes for performance
            entity.HasIndex(e => e.Token)
                .HasDatabaseName("IX_RefreshTokens_Token");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_RefreshTokens_UserId");

            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

            // Composite index for the most common refresh token query
            entity.HasIndex(e => new { e.Token, e.IsRevoked })
                .HasDatabaseName("IX_RefreshTokens_Token_IsRevoked")
                .HasFilter("IsRevoked = 0"); // Filtered index for active tokens only
        });
    }

    public override int SaveChanges()
    {
        try
        {
            var result = base.SaveChanges();
            _logger.LogDebug("SaveChanges completed successfully, {ChangeCount} changes saved", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during SaveChanges operation");
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("SaveChangesAsync completed successfully, {ChangeCount} changes saved", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during SaveChangesAsync operation");
            throw;
        }
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

        var connectionString = ConnectionStringHelper.ProcessConnectionString(
            configuration.GetConnectionString("DefaultConnection")!,
            configuration);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found in configuration. " +
                "Ensure DefaultConnection is set in appsettings.json.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: [258, 2, 53, 121, 232, 20]);
        });

        // Create a default logger if _logger is null
        var logger = _logger ?? new LoggerFactory().CreateLogger<AppDbContext>();

        return new AppDbContext(optionsBuilder.Options, logger);
    }
}

