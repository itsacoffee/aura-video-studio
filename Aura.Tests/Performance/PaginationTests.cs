using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Extensions;
using Aura.Core.Models.Pagination;
using Xunit;

namespace Aura.Tests.Performance;

public class PaginationTests
{
    [Fact]
    public void PagedResult_Empty_ReturnsCorrectMetadata()
    {
        // Arrange & Act
        var result = PagedResult<string>.Empty(pageNumber: 1, pageSize: 10);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_Create_CalculatesCorrectMetadata()
    {
        // Arrange
        var items = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToList();
        
        // Act
        var result = PagedResult<string>.Create(items, pageNumber: 2, pageSize: 10, totalCount: 45);

        // Assert
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(45, result.TotalCount);
        Assert.Equal(5, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(11, result.FirstItemIndex);
        Assert.Equal(20, result.LastItemIndex);
    }

    [Fact]
    public void PagedResult_LastPage_HasCorrectMetadata()
    {
        // Arrange
        var items = Enumerable.Range(1, 5).Select(i => $"Item {i}").ToList();
        
        // Act
        var result = PagedResult<string>.Create(items, pageNumber: 3, pageSize: 10, totalCount: 25);

        // Assert
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
        Assert.Equal(21, result.FirstItemIndex);
        Assert.Equal(25, result.LastItemIndex);
    }

    [Fact]
    public void PaginationParams_EnforcesMaxPageSize()
    {
        // Arrange
        var pagination = new PaginationParams();
        
        // Act
        pagination.PageSize = 200; // Above max of 100

        // Assert
        Assert.Equal(100, pagination.PageSize);
    }

    [Fact]
    public void PaginationParams_CalculatesCorrectSkip()
    {
        // Arrange
        var pagination = new PaginationParams
        {
            PageNumber = 3,
            PageSize = 20
        };

        // Act
        var skip = pagination.Skip;

        // Assert
        Assert.Equal(40, skip);
    }

    [Fact]
    public void PaginationParams_DefaultsToDescending()
    {
        // Arrange
        var pagination = new PaginationParams();

        // Assert
        Assert.True(pagination.IsDescending);
        Assert.Equal("desc", pagination.SortDirection);
    }

    [Theory]
    [InlineData("asc", false)]
    [InlineData("ASC", false)]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("invalid", false)]
    public void PaginationParams_ParsesSortDirection(string sortDirection, bool expectedDescending)
    {
        // Arrange
        var pagination = new PaginationParams
        {
            SortDirection = sortDirection
        };

        // Assert
        Assert.Equal(expectedDescending, pagination.IsDescending);
    }

    [Fact]
    public async Task ToPagedResultAsync_WithQueryable_ReturnsPaginatedResults()
    {
        // Arrange
        var data = Enumerable.Range(1, 100).Select(i => new TestItem { Id = i, Name = $"Item {i}" });
        var queryable = data.AsQueryable();
        var pagination = new PaginationParams
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await queryable
            .Where(x => x.Id > 0)
            .ToPagedResultAsync(pagination);

        // Assert
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(10, result.TotalPages);
        
        // Verify correct items are returned
        Assert.Equal(11, result.Items[0].Id);
        Assert.Equal(20, result.Items[^1].Id);
    }

    [Fact]
    public async Task ToPagedResultAsync_EmptyQuery_ReturnsEmptyResult()
    {
        // Arrange
        var data = Array.Empty<TestItem>();
        var queryable = data.AsQueryable();
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await queryable.ToPagedResultAsync(pagination);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void ApplySort_ValidProperty_SortsCorrectly()
    {
        // Arrange
        var data = new[]
        {
            new TestItem { Id = 3, Name = "Charlie" },
            new TestItem { Id = 1, Name = "Alice" },
            new TestItem { Id = 2, Name = "Bob" }
        }.AsQueryable();

        // Act - Sort by Name ascending
        var result = data.ApplySort("Name", descending: false).ToList();

        // Assert
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
        Assert.Equal("Charlie", result[2].Name);
    }

    [Fact]
    public void ApplySort_InvalidProperty_ReturnsUnsortedQuery()
    {
        // Arrange
        var data = new[]
        {
            new TestItem { Id = 3, Name = "Charlie" },
            new TestItem { Id = 1, Name = "Alice" }
        }.AsQueryable();

        // Act - Sort by invalid property
        var result = data.ApplySort("NonExistentProperty", descending: false).ToList();

        // Assert
        Assert.Equal(2, result.Count); // Should still return all items
        Assert.Equal(3, result[0].Id); // Order unchanged
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
