using System;
using System.IO;
using System.Threading.Tasks;
using Aura.Api.Middleware;
using Aura.Core.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for FirstRunMiddleware to ensure onboarding endpoints are accessible
/// </summary>
public class FirstRunMiddlewareTests
{
    private readonly ILogger<FirstRunMiddleware> _logger;

    public FirstRunMiddlewareTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<FirstRunMiddleware>();
    }

    [Theory]
    [InlineData("/api/apikeys/save")]
    [InlineData("/api/keys/set")]
    [InlineData("/api/keys/test")]
    [InlineData("/api/providers/validate")]
    [InlineData("/api/dependencies/rescan")]
    [InlineData("/api/preflight")]
    [InlineData("/api/probes/run")]
    [InlineData("/api/downloads/ffmpeg/install")]
    public async Task FirstRunMiddleware_AllowsOnboardingEndpoints_WhenSetupNotCompleted(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "POST";

        var serviceCollection = new ServiceCollection();
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        serviceCollection.AddScoped<AuraDbContext>(_ => new AuraDbContext(options));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        context.RequestServices = serviceProvider;

        var middlewareCalled = false;
        RequestDelegate next = (ctx) =>
        {
            middlewareCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new FirstRunMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - middleware should allow the request to proceed
        Assert.True(middlewareCalled, $"Middleware should allow {path} during onboarding");
    }

    [Theory]
    [InlineData("/api/jobs")]
    [InlineData("/api/videos/generate")]
    [InlineData("/api/dashboard")]
    public async Task FirstRunMiddleware_BlocksNonOnboardingEndpoints_WhenSetupNotCompleted(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "POST";
        context.Response.Body = new MemoryStream();

        var serviceCollection = new ServiceCollection();
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        serviceCollection.AddScoped<AuraDbContext>(_ => new AuraDbContext(options));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        context.RequestServices = serviceProvider;

        var middlewareCalled = false;
        RequestDelegate next = (ctx) =>
        {
            middlewareCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new FirstRunMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - middleware should block the request
        Assert.False(middlewareCalled, $"Middleware should block {path} when setup not completed");
        Assert.Equal(428, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/apikeys/save")]
    [InlineData("/api/providers/validate")]
    [InlineData("/api/jobs")]
    public async Task FirstRunMiddleware_AllowsAllEndpoints_WhenSetupCompleted(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "POST";

        var serviceCollection = new ServiceCollection();
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        using var dbContext = new AuraDbContext(options);
        
        // Mark setup as completed
        dbContext.UserSetups.Add(new UserSetupEntity
        {
            UserId = "default",
            Completed = true,
            CompletedAt = DateTime.UtcNow,
            Version = "1.0.0"
        });
        await dbContext.SaveChangesAsync();

        serviceCollection.AddScoped<AuraDbContext>(_ => new AuraDbContext(options));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        context.RequestServices = serviceProvider;

        var middlewareCalled = false;
        RequestDelegate next = (ctx) =>
        {
            middlewareCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new FirstRunMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - middleware should allow all requests when setup is completed
        Assert.True(middlewareCalled, $"Middleware should allow {path} when setup completed");
    }

    [Fact]
    public async Task FirstRunMiddleware_AllowsHealthEndpoints_Always()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/health/live";

        var serviceCollection = new ServiceCollection();
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        serviceCollection.AddScoped<AuraDbContext>(_ => new AuraDbContext(options));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        context.RequestServices = serviceProvider;

        var middlewareCalled = false;
        RequestDelegate next = (ctx) =>
        {
            middlewareCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new FirstRunMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(middlewareCalled, "Health endpoints should always be accessible");
    }
}
