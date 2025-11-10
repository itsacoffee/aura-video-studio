using System;
using System.Buffers;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Array pooling utilities for reducing heap allocations
/// </summary>
public static class BufferPool
{
    /// <summary>
    /// Rent a byte array from the shared pool
    /// </summary>
    public static byte[] RentBytes(int minimumLength)
    {
        return ArrayPool<byte>.Shared.Rent(minimumLength);
    }

    /// <summary>
    /// Return a byte array to the shared pool
    /// </summary>
    public static void ReturnBytes(byte[] array, bool clearArray = false)
    {
        ArrayPool<byte>.Shared.Return(array, clearArray);
    }

    /// <summary>
    /// Rent a char array from the shared pool
    /// </summary>
    public static char[] RentChars(int minimumLength)
    {
        return ArrayPool<char>.Shared.Rent(minimumLength);
    }

    /// <summary>
    /// Return a char array to the shared pool
    /// </summary>
    public static void ReturnChars(char[] array, bool clearArray = false)
    {
        ArrayPool<char>.Shared.Return(array, clearArray);
    }

    /// <summary>
    /// Execute an action with a rented byte array, automatically returning it
    /// </summary>
    public static T UseBytes<T>(int minimumLength, Func<byte[], T> action, bool clearArray = false)
    {
        var array = RentBytes(minimumLength);
        try
        {
            return action(array);
        }
        finally
        {
            ReturnBytes(array, clearArray);
        }
    }

    /// <summary>
    /// Execute an action with a rented char array, automatically returning it
    /// </summary>
    public static T UseChars<T>(int minimumLength, Func<char[], T> action, bool clearArray = false)
    {
        var array = RentChars(minimumLength);
        try
        {
            return action(array);
        }
        finally
        {
            ReturnChars(array, clearArray);
        }
    }
}
