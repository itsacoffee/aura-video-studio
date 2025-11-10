using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Base class for integration tests with WebApplicationFactory setup
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            ConfigureServices(builder);
        });

        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
    }

    /// <summary>
    /// Override to configure test-specific services
    /// </summary>
    protected virtual void ConfigureServices(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Default: No additional configuration
    }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get an optional service from the DI container
    /// </summary>
    protected T? GetOptionalService<T>()
    {
        return Scope.ServiceProvider.GetService<T>();
    }

    public virtual void Dispose()
    {
        Scope?.Dispose();
        Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
