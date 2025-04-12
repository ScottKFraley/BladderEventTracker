using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using trackerApi.DbContext;

namespace trackerApi.TestUtils;

public static class DbContextMockHelper
{
    public static Mock<AppDbContext> CreateMockDbContext<TEntity>(IQueryable<TEntity> data)
        where TEntity : class
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var loggerMock = new Mock<ILogger<AppDbContext>>();

        var mockContext = new Mock<AppDbContext>(
            MockBehavior.Loose,
            options,
            loggerMock.Object
        );

        var mockSet = CreateMockDbSet(data);
        mockContext.Setup(c => c.Set<TEntity>()).Returns(mockSet.Object);

        return mockContext;
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockDbSet = new Mock<DbSet<T>>();

        mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

        // Setup async operations
        mockDbSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] ids) =>
            {
                // This runs as actual code, not as an expression tree
                foreach (var item in data)
                {
                    var idProperty = typeof(T).GetProperty("Id");
                    if (idProperty != null)
                    {
                        var itemId = idProperty.GetValue(item);
                        if (itemId != null && itemId.Equals(ids[0]))
                        {
                            return item;
                        }
                    }
                }
                return null;
            });

        return mockDbSet;
    }
}
