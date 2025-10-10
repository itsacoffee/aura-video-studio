using Aura.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for CorrelationIdMiddleware
/// </summary>
public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task CorrelationIdMiddleware_GeneratesCorrelationId_WhenNotProvided()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middlewareCalled = false;
        
        RequestDelegate next = (ctx) =>
        {
            middlewareCalled = true;
            // Verify correlation ID was added to context items
            Assert.True(ctx.Items.ContainsKey("CorrelationId"));
            Assert.NotNull(ctx.Items["CorrelationId"]);
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(middlewareCalled);
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
        Assert.NotNull(context.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Fact]
    public async Task CorrelationIdMiddleware_UsesProvidedCorrelationId_WhenPresent()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = expectedCorrelationId;
        
        var middlewareCalled = false;
        RequestDelegate next = (ctx) =>
        {
            middlewareCalled = true;
            // Verify the provided correlation ID was used
            Assert.Equal(expectedCorrelationId, ctx.Items["CorrelationId"]);
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(middlewareCalled);
        Assert.Equal(expectedCorrelationId, context.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Fact]
    public async Task CorrelationIdMiddleware_AddsCorrelationIdToResponseHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }

    [Fact]
    public async Task CorrelationIdMiddleware_AddsCorrelationIdToHttpContextItems()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Items.ContainsKey("CorrelationId"));
        var correlationId = context.Items["CorrelationId"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }

    [Fact]
    public async Task CorrelationIdMiddleware_CorrelationIdMatchesBetweenHeaderAndItems()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var headerCorrelationId = context.Response.Headers["X-Correlation-ID"].ToString();
        var itemsCorrelationId = context.Items["CorrelationId"]?.ToString();
        
        Assert.Equal(headerCorrelationId, itemsCorrelationId);
    }
}
