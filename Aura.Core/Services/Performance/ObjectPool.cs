using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Generic object pool for efficient resource reuse
/// Reduces GC pressure and improves performance for frequently created/disposed objects
/// </summary>
public class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _objects;
    private readonly Func<T> _objectFactory;
    private readonly Action<T>? _resetAction;
    private readonly int _maxSize;
    private readonly ILogger? _logger;
    private int _currentSize;

    public ObjectPool(
        Func<T> objectFactory,
        Action<T>? resetAction = null,
        int maxSize = 100,
        ILogger? logger = null)
    {
        _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
        _resetAction = resetAction;
        _maxSize = maxSize;
        _logger = logger;
        _objects = new ConcurrentBag<T>();
        _currentSize = 0;
    }

    /// <summary>
    /// Get an object from the pool or create a new one
    /// </summary>
    public T Get()
    {
        if (_objects.TryTake(out var item))
        {
            Interlocked.Decrement(ref _currentSize);
            return item;
        }

        return _objectFactory();
    }

    /// <summary>
    /// Return an object to the pool
    /// </summary>
    public void Return(T item)
    {
        if (item == null)
        {
            return;
        }

        if (_currentSize >= _maxSize)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
            return;
        }

        try
        {
            _resetAction?.Invoke(item);
            _objects.Add(item);
            Interlocked.Increment(ref _currentSize);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to reset pooled object, discarding");
            
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Get the current size of the pool
    /// </summary>
    public int Size => _currentSize;

    /// <summary>
    /// Clear the pool and dispose all objects
    /// </summary>
    public void Clear()
    {
        while (_objects.TryTake(out var item))
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _currentSize = 0;
    }
}

/// <summary>
/// Async-safe object pool for resources that need async initialization
/// </summary>
public class AsyncObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _objects;
    private readonly Func<Task<T>> _objectFactory;
    private readonly Func<T, Task>? _resetAction;
    private readonly int _maxSize;
    private readonly ILogger? _logger;
    private int _currentSize;
    private readonly SemaphoreSlim _semaphore;

    public AsyncObjectPool(
        Func<Task<T>> objectFactory,
        Func<T, Task>? resetAction = null,
        int maxSize = 100,
        ILogger? logger = null)
    {
        _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
        _resetAction = resetAction;
        _maxSize = maxSize;
        _logger = logger;
        _objects = new ConcurrentBag<T>();
        _currentSize = 0;
        _semaphore = new SemaphoreSlim(maxSize, maxSize);
    }

    /// <summary>
    /// Get an object from the pool or create a new one asynchronously
    /// </summary>
    public async Task<T> GetAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        if (_objects.TryTake(out var item))
        {
            Interlocked.Decrement(ref _currentSize);
            return item;
        }

        try
        {
            return await _objectFactory().ConfigureAwait(false);
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Return an object to the pool asynchronously
    /// </summary>
    public async Task ReturnAsync(T item, CancellationToken cancellationToken = default)
    {
        if (item == null)
        {
            _semaphore.Release();
            return;
        }

        if (_currentSize >= _maxSize)
        {
            if (item is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _semaphore.Release();
            return;
        }

        try
        {
            if (_resetAction != null)
            {
                await _resetAction(item).ConfigureAwait(false);
            }

            _objects.Add(item);
            Interlocked.Increment(ref _currentSize);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to reset pooled object, discarding");
            
            if (item is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Get the current size of the pool
    /// </summary>
    public int Size => _currentSize;

    /// <summary>
    /// Clear the pool and dispose all objects asynchronously
    /// </summary>
    public async Task ClearAsync()
    {
        while (_objects.TryTake(out var item))
        {
            if (item is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _currentSize = 0;
    }
}

/// <summary>
/// Pooled object wrapper that automatically returns the object to the pool when disposed
/// </summary>
public class PooledObject<T> : IDisposable where T : class
{
    private readonly ObjectPool<T> _pool;
    private T? _object;
    private bool _disposed;

    public PooledObject(ObjectPool<T> pool, T obj)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _object = obj ?? throw new ArgumentNullException(nameof(obj));
    }

    public T Value => _object ?? throw new ObjectDisposedException(nameof(PooledObject<T>));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_object != null)
        {
            _pool.Return(_object);
            _object = null;
        }

        _disposed = true;
    }
}

/// <summary>
/// Extension methods for object pools
/// </summary>
public static class ObjectPoolExtensions
{
    /// <summary>
    /// Rent an object from the pool with automatic return on dispose
    /// </summary>
    public static PooledObject<T> Rent<T>(this ObjectPool<T> pool) where T : class
    {
        var obj = pool.Get();
        return new PooledObject<T>(pool, obj);
    }
}
