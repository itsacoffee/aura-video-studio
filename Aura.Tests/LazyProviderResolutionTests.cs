using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify that the application starts successfully even with misconfigured providers
/// </summary>
public class LazyProviderResolutionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LazyProviderResolutionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_Should_StartSuccessfully_WithoutEagerProviderResolution()
    {
        // Arrange & Act - Simply creating the client triggers application startup
        var client = _factory.CreateClient();

        // Assert - If we get here without exceptions, startup succeeded
        var response = await client.GetAsync("/api/healthz");
        
        // Verify the health check endpoint is accessible
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    [Fact]
    public async Task Application_Should_StartSuccessfully_EvenWithMissingApiKeys()
    {
        // Arrange - Create a custom factory without API keys configured
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            // No need to configure anything - the default environment has no API keys
            // This simulates a fresh installation
        });

        // Act - Create client to trigger startup
        var client = customFactory.CreateClient();

        // Assert - Application should start successfully
        var response = await client.GetAsync("/api/healthz");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ProviderWarmupService_Should_RunInBackground()
    {
        // Arrange & Act
        var client = _factory.CreateClient();

        // The warmup service should run in the background after startup
        // Give it a moment to complete
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert - Application should still be responsive
        var response = await client.GetAsync("/api/healthz");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ScriptOrchestrator_Should_InitializeProvidersLazily_OnFirstUse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make a request that would use the script orchestrator
        // This simulates first actual use of providers
        var response = await client.GetAsync("/api/capabilities");

        // Assert - Should succeed without provider initialization errors
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public void ServiceProvider_Should_ResolveOrchestratorsWithoutThrowingAtStartup()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Resolving the orchestrators should not throw
        var scriptOrchestrator = services.GetRequiredService<Aura.Core.Orchestrator.ScriptOrchestrator>();
        Assert.NotNull(scriptOrchestrator);

        var plannerService = services.GetRequiredService<Aura.Core.Planner.IRecommendationService>();
        Assert.NotNull(plannerService);
    }
}
