
namespace trackerApi.IntegrationTests.Tests;

[Collection("Database")]
public class AppDbContextIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public AppDbContextIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanConnectToDatabase()
    {
        // Act & Assert
        var canConnect = await _fixture.Context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public void DatabaseHasExpectedTables()
    {
        // Act / Assert
        // Verify the tables exist in the Model 
        Assert.NotNull(_fixture.Context.Model.FindEntityType(typeof(User)));
        Assert.NotNull(_fixture.Context.Model.FindEntityType(typeof(TrackingLogItem)));
    }

    [Fact]
    public async Task CanQueryTables()
    {
        // Verify we can query the tables
        var hasUsers = await _fixture.Context.Users.AnyAsync();
        var hasLogs = await _fixture.Context.TrackingLogs.AnyAsync();
        
        // We're not asserting true/false here because the tables might be empty
        // We're just verifying we can query them without errors
        Assert.True(true);  // If we get here, the queries succeeded
    }
}
