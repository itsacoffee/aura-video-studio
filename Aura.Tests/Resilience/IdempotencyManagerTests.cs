using Aura.Core.Resilience.Idempotency;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Resilience;

public class IdempotencyManagerTests
{
    private readonly Mock<ILogger<IdempotencyManager>> _loggerMock;
    private readonly IdempotencyManager _manager;

    public IdempotencyManagerTests()
    {
        _loggerMock = new Mock<ILogger<IdempotencyManager>>();
        _manager = new IdempotencyManager(_loggerMock.Object);
    }

    [Fact]
    public void TryGetResult_ShouldReturnFalse_WhenKeyNotFound()
    {
        // Arrange
        var key = "test-key";

        // Act
        var found = _manager.TryGetResult<string>(key, out var result);

        // Assert
        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void StoreResult_AndTryGetResult_ShouldReturnStoredValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        _manager.StoreResult(key, value);
        var found = _manager.TryGetResult<string>(key, out var result);

        // Assert
        Assert.True(found);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_ShouldExecuteOnce_WhenCalledMultipleTimes()
    {
        // Arrange
        var key = "test-key";
        var executionCount = 0;

        async Task<string> Operation()
        {
            executionCount++;
            await Task.Delay(10);
            return "result";
        }

        // Act
        var result1 = await _manager.ExecuteIdempotentAsync(key, Operation);
        var result2 = await _manager.ExecuteIdempotentAsync(key, Operation);
        var result3 = await _manager.ExecuteIdempotentAsync(key, Operation);

        // Assert
        Assert.Equal("result", result1);
        Assert.Equal("result", result2);
        Assert.Equal("result", result3);
        Assert.Equal(1, executionCount); // Should only execute once
    }

    [Fact]
    public void StoreResult_WithCustomTTL_ShouldExpireAfterTTL()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var ttl = TimeSpan.FromMilliseconds(100);

        // Act
        _manager.StoreResult(key, value, ttl);
        Thread.Sleep(200); // Wait for expiration

        var found = _manager.TryGetResult<string>(key, out var result);

        // Assert
        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void CleanupExpired_ShouldRemoveExpiredRecords()
    {
        // Arrange
        _manager.StoreResult("key1", "value1", TimeSpan.FromMilliseconds(50));
        _manager.StoreResult("key2", "value2", TimeSpan.FromHours(1));
        
        Thread.Sleep(100); // Wait for key1 to expire

        // Act
        var removedCount = _manager.CleanupExpired();

        // Assert
        Assert.Equal(1, removedCount);
        Assert.False(_manager.TryGetResult<string>("key1", out _));
        Assert.True(_manager.TryGetResult<string>("key2", out _));
    }

    [Fact]
    public void GetRecordCount_ShouldReturnCorrectCount()
    {
        // Arrange
        _manager.StoreResult("key1", "value1");
        _manager.StoreResult("key2", "value2");
        _manager.StoreResult("key3", "value3");

        // Act
        var count = _manager.GetRecordCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void Clear_ShouldRemoveAllRecords()
    {
        // Arrange
        _manager.StoreResult("key1", "value1");
        _manager.StoreResult("key2", "value2");
        _manager.StoreResult("key3", "value3");

        // Act
        _manager.Clear();

        // Assert
        Assert.Equal(0, _manager.GetRecordCount());
        Assert.False(_manager.TryGetResult<string>("key1", out _));
        Assert.False(_manager.TryGetResult<string>("key2", out _));
        Assert.False(_manager.TryGetResult<string>("key3", out _));
    }

    [Fact]
    public void StoreResult_ShouldOverwriteExistingKey()
    {
        // Arrange
        var key = "test-key";
        
        // Act
        _manager.StoreResult(key, "value1");
        _manager.StoreResult(key, "value2");

        // Assert
        _manager.TryGetResult<string>(key, out var result);
        Assert.Equal("value2", result);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_ShouldHandleExceptions()
    {
        // Arrange
        var key = "test-key";

        async Task<string> Operation()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Test error");
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _manager.ExecuteIdempotentAsync(key, Operation);
        });

        // The result should not be stored on exception
        Assert.False(_manager.TryGetResult<string>(key, out _));
    }
}
