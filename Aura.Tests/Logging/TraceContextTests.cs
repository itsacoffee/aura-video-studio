using Aura.Core.Logging;
using Xunit;

namespace Aura.Tests.Logging;

public class TraceContextTests
{
    [Fact]
    public void TraceContext_Should_Generate_Unique_IDs()
    {
        // Arrange & Act
        var context1 = new TraceContext();
        var context2 = new TraceContext();

        // Assert
        Assert.NotEqual(context1.TraceId, context2.TraceId);
        Assert.NotEqual(context1.SpanId, context2.SpanId);
        Assert.NotEmpty(context1.TraceId);
        Assert.NotEmpty(context1.SpanId);
    }

    [Fact]
    public void TraceContext_Should_Propagate_TraceId_To_Child()
    {
        // Arrange
        var parentContext = new TraceContext();

        // Act
        var childContext = parentContext.CreateChildSpan("ChildOperation");

        // Assert
        Assert.Equal(parentContext.TraceId, childContext.TraceId);
        Assert.NotEqual(parentContext.SpanId, childContext.SpanId);
        Assert.Equal(parentContext.SpanId, childContext.ParentSpanId);
    }

    [Fact]
    public void TraceContext_Should_Accept_External_TraceId()
    {
        // Arrange
        var externalTraceId = "external-trace-123";
        var externalParentSpanId = "external-span-456";

        // Act
        var context = new TraceContext(externalTraceId, externalParentSpanId);

        // Assert
        Assert.Equal(externalTraceId, context.TraceId);
        Assert.Equal(externalParentSpanId, context.ParentSpanId);
        Assert.NotEmpty(context.SpanId);
    }

    [Fact]
    public void TraceContext_Should_Store_And_Retrieve_Metadata()
    {
        // Arrange
        var context = new TraceContext();

        // Act
        context.Metadata["key1"] = "value1";
        context.Metadata["key2"] = 123;

        // Assert
        Assert.Equal("value1", context.Metadata["key1"]);
        Assert.Equal(123, context.Metadata["key2"]);
    }

    [Fact]
    public void TraceContext_Should_Propagate_Metadata_To_Child()
    {
        // Arrange
        var parentContext = new TraceContext();
        parentContext.Metadata["parentKey"] = "parentValue";
        parentContext.UserId = "user123";

        // Act
        var childContext = parentContext.CreateChildSpan("ChildOperation");

        // Assert
        Assert.Equal("parentValue", childContext.Metadata["parentKey"]);
        Assert.Equal("user123", childContext.UserId);
    }

    [Fact]
    public void TraceContext_Scope_Should_Set_Current_Context()
    {
        // Arrange
        var context = new TraceContext();
        Assert.Null(TraceContext.Current);

        // Act
        using (TraceContext.BeginScope(context))
        {
            // Assert - within scope
            Assert.NotNull(TraceContext.Current);
            Assert.Equal(context.TraceId, TraceContext.Current?.TraceId);
        }

        // Assert - after scope
        Assert.Null(TraceContext.Current);
    }

    [Fact]
    public void TraceContext_Nested_Scopes_Should_Work_Correctly()
    {
        // Arrange
        var outerContext = new TraceContext();
        var innerContext = new TraceContext();

        // Act & Assert
        using (TraceContext.BeginScope(outerContext))
        {
            Assert.Equal(outerContext.TraceId, TraceContext.Current?.TraceId);

            using (TraceContext.BeginScope(innerContext))
            {
                Assert.Equal(innerContext.TraceId, TraceContext.Current?.TraceId);
            }

            Assert.Equal(outerContext.TraceId, TraceContext.Current?.TraceId);
        }

        Assert.Null(TraceContext.Current);
    }

    [Fact]
    public void TraceContext_BeginNewScope_Should_Create_New_Context()
    {
        // Arrange & Act
        using var scope = TraceContext.BeginNewScope("TestOperation");

        // Assert
        Assert.NotNull(TraceContext.Current);
        Assert.Equal("TestOperation", TraceContext.Current?.OperationName);
        Assert.NotEmpty(TraceContext.Current?.TraceId ?? string.Empty);
        Assert.NotEmpty(TraceContext.Current?.SpanId ?? string.Empty);
    }

    [Fact]
    public async Task TraceContext_Should_Flow_Across_Async_Boundaries()
    {
        // Arrange
        var context = new TraceContext { OperationName = "AsyncOperation" };

        // Act
        using (TraceContext.BeginScope(context))
        {
            await Task.Delay(10);

            // Assert - context should still be available after await
            Assert.NotNull(TraceContext.Current);
            Assert.Equal("AsyncOperation", TraceContext.Current?.OperationName);
        }
    }
}
