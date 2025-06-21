using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

using Moq;

using System.Linq.Expressions;

using trackerApi.DbContext;

namespace trackerApi.TestUtils;

/// <summary>
/// See the bof for more info.
/// </summary>
public static class DbContextMockHelper
{
    public static Mock<AppDbContext> CreateMockDbContext<TEntity>(IQueryable<TEntity> data) where TEntity : class
    {
        var mockContext = new Mock<AppDbContext>();
        var mockSet = new Mock<DbSet<TEntity>>();

        mockSet.As<IQueryable<TEntity>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<TEntity>(data.Provider, data));

        mockSet.As<IQueryable<TEntity>>()
            .Setup(m => m.Expression)
            .Returns(data.Expression);

        mockSet.As<IQueryable<TEntity>>()
            .Setup(m => m.ElementType)
            .Returns(data.ElementType);

        mockSet.As<IQueryable<TEntity>>()
            .Setup(m => m.GetEnumerator())
            .Returns(data.GetEnumerator());

        mockContext.Setup(c => c.Set<TEntity>())
            .Returns(mockSet.Object);

        return mockContext;
    }
}

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    private readonly IQueryable<TEntity> _data;

    public TestAsyncQueryProvider(IQueryProvider inner, IQueryable<TEntity> data)
    {
        _inner = inner;
        _data = data;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(_data);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(_data.Cast<TElement>());
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        var result = Execute<IEnumerable<TResult>>(expression);
        return new TestAsyncEnumerable<TResult>(result.AsQueryable());
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        if (expression is MethodCallExpression methodCall &&
            methodCall.Method.Name == "FirstOrDefault")
        {
            // Get the type argument of Task<T>
            var resultType = typeof(TResult).GetGenericArguments()[0];

            // Handle FirstOrDefaultAsync
            if (methodCall.Arguments.Count > 1)
            {
                var predicate = (Expression<Func<TEntity, bool>>)((UnaryExpression)methodCall.Arguments[1]).Operand;
                var result = _data.FirstOrDefault(predicate);
                var taskResult = typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new object?[] { result });
                return (TResult)taskResult!;
            }
            else
            {
                var result = _data.FirstOrDefault();
                var taskResult = typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new object?[] { result });
                return (TResult)taskResult!;
            }
        }

        // For other cases, fall back to regular execution
        var regularResult = Execute<TResult>(expression);
        return regularResult;
    }
}


public class TestAsyncEnumerable<T> : IAsyncEnumerable<T>, IQueryable<T>
{
    private readonly IQueryable<T> _inner;

    public TestAsyncEnumerable(IQueryable<T> inner)
    {
        _inner = inner;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(_inner.GetEnumerator());
    }

    public Type ElementType => _inner.ElementType;
    public Expression Expression => _inner.Expression;
    public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_inner.Provider, _inner);

    public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        _inner.Dispose();

        return new ValueTask();
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }
}
