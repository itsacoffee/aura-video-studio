using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Object pool for StringBuilder to reduce allocations in high-throughput scenarios
/// </summary>
public class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> _pool = 
        new DefaultObjectPoolProvider().CreateStringBuilderPool(
            initialCapacity: 256,
            maximumRetainedCapacity: 4096);

    /// <summary>
    /// Get a StringBuilder from the pool
    /// </summary>
    public static StringBuilder Get()
    {
        return _pool.Get();
    }

    /// <summary>
    /// Return a StringBuilder to the pool
    /// </summary>
    public static void Return(StringBuilder sb)
    {
        _pool.Return(sb);
    }

    /// <summary>
    /// Get a StringBuilder, use it, and automatically return it
    /// </summary>
    public static string Build(Action<StringBuilder> builder)
    {
        var sb = Get();
        try
        {
            builder(sb);
            return sb.ToString();
        }
        finally
        {
            Return(sb);
        }
    }
}
