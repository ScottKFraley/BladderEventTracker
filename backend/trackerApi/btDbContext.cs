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

            // Configure Id as UUID
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            // Configure foreign key relationship
            entity.HasOne(t => t.User)
                .WithMany(u => u.TrackingLogs)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // or DeleteBehavior.Restrict if you don't want automatic deletion

            // Create a unique constraint on EventDate
            entity.HasIndex(e => e.EventDate).IsUnique();

            // Configure EventDate with default
            entity.Property(e => e.EventDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

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

            // Configure Id as an auto-incrementing identity column
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            // Configure default values for timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnUpdate(); // Update on row modification (if supported)

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
        var dbPassword = configuration["DbPassword"];

        // Replace the hostname for local development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            connectionString = connectionString!.Replace("database", "localhost");
        }

        connectionString = connectionString!.Replace("${DbPassword}", dbPassword ?? "test-password");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Create a default logger if _logger is null
        var logger = _logger ?? new LoggerFactory().CreateLogger<AppDbContext>();

        return new AppDbContext(optionsBuilder.Options, logger);
    }
}

public partial class UpdatedUsersTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Standard table creation (generated by EF Core)
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false, defaultValueSql: "gen_random_uuid()"),
                Username = table.Column<string>(maxLength: 50, nullable: false),
                PasswordHash = table.Column<string>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        // Add trigger for UpdatedAt
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION update_updated_at_column()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.""UpdatedAt"" = CURRENT_TIMESTAMP;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER set_updated_at
            BEFORE UPDATE ON ""Users""
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column();
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Users");

        // Drop trigger
        migrationBuilder.Sql(@"
            DROP TRIGGER IF EXISTS set_updated_at ON ""Users"";
            DROP FUNCTION IF EXISTS update_updated_at_column();
        ");
    }
}
