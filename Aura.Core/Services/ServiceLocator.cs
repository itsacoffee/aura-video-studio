using Microsoft.Extensions.DependencyInjection;

namespace Aura.Core.Services;

/// <summary>
/// Temporary service locator for accessing DI services from non-DI contexts.
/// This is a transitional pattern to be refactored when VideoOrchestrator is updated
/// to use proper constructor injection.
/// 
/// IMPORTANT: This should only be used in legacy code paths that cannot be easily
/// refactored. New code should use constructor injection instead.
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Initializes the service locator with the application's service provider.
    /// Should be called once during application startup after DI container is built.
    /// </summary>
    /// <param name="serviceProvider">The configured service provider.</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        lock (_lock)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
    }

    /// <summary>
    /// Gets whether the service locator has been initialized.
    /// </summary>
    public static bool IsInitialized
    {
        get
        {
            lock (_lock)
            {
                return _serviceProvider != null;
            }
        }
    }

    /// <summary>
    /// Gets a service of the specified type, or null if not available.
    /// </summary>
    /// <typeparam name="T">Service type.</typeparam>
    /// <returns>The service instance, or null if not registered.</returns>
    public static T? GetService<T>() where T : class
    {
        lock (_lock)
        {
            if (_serviceProvider == null)
            {
                return null;
            }

            return _serviceProvider.GetService<T>();
        }
    }

    /// <summary>
    /// Gets a required service of the specified type.
    /// Throws if the service is not registered.
    /// </summary>
    /// <typeparam name="T">Service type.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">If service locator not initialized.</exception>
    /// <exception cref="InvalidOperationException">If service not registered.</exception>
    public static T GetRequiredService<T>() where T : class
    {
        lock (_lock)
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(
                    "ServiceLocator has not been initialized. Call Initialize() during application startup.");
            }

            return _serviceProvider.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// Resets the service locator. Intended for testing purposes only.
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _serviceProvider = null;
        }
    }
}
