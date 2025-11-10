using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Aura.Core.Extensions;

/// <summary>
/// Extension methods for IQueryable to support pagination and performance optimization
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies pagination to a queryable and returns a PagedResult
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        if (pagination.PageNumber < 1)
            pagination.PageNumber = 1;

        if (pagination.PageSize < 1)
            pagination.PageSize = 20;

        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<T>.Empty(pagination.PageNumber, pagination.PageSize);
        }

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }

    /// <summary>
    /// Applies dynamic sorting to a queryable based on property name
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? propertyName,
        bool descending = false)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        
        try
        {
            var property = Expression.Property(parameter, propertyName);
            var lambda = Expression.Lambda(property, parameter);

            var methodName = descending ? "OrderByDescending" : "OrderBy";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), property.Type },
                query.Expression,
                Expression.Quote(lambda));

            return query.Provider.CreateQuery<T>(resultExpression);
        }
        catch
        {
            // If sorting fails (invalid property name), return unsorted query
            return query;
        }
    }

    /// <summary>
    /// Applies pagination with sorting
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool descending = true,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PaginationParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = descending ? "desc" : "asc"
        };

        query = query.ApplySort(sortBy, descending);
        return await query.ToPagedResultAsync(pagination, cancellationToken);
    }

    /// <summary>
    /// Executes the query and returns results with query execution time
    /// </summary>
    public static async Task<(T Result, TimeSpan Duration)> ExecuteWithTimingAsync<T>(
        this Task<T> task)
    {
        var startTime = DateTime.UtcNow;
        var result = await task;
        var duration = DateTime.UtcNow - startTime;
        return (result, duration);
    }
}
