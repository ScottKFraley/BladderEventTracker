using Microsoft.EntityFrameworkCore;

using trackerApi.DbContext;

namespace trackerApi.UnitTests;

// Unit Tests
public class AppDbContextUnitTests
{
    [Fact]
    public void CreateDbContext_ShouldCreateValidContext()
    {
        // Arrange
        var factory = new AppDbContextFactory();

        // Act
        var context = factory.CreateDbContext([]);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<AppDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_ShouldHaveValidConnectionString()
    {
        // Arrange
        var factory = new AppDbContextFactory();

        // Act
        var context = factory.CreateDbContext(new string[] { });
        var connection = context.Database.GetDbConnection();

        // Assert
        Assert.NotNull(connection);
        Assert.Contains("Host=localhost", connection.ConnectionString);
        Assert.Contains("Database=BETrackingDb", connection.ConnectionString);
        Assert.DoesNotContain("${DbPassword}", connection.ConnectionString);
    }
}
