using Aura.Core.Providers;

namespace Aura.Core.AI.Routing;

/// <summary>
/// Factory for creating LLM provider instances by name.
/// </summary>
public interface ILlmProviderFactory
{
    /// <summary>
    /// Create a provider instance by name and model.
    /// </summary>
    ILlmProvider Create(string providerName, string modelName);

    /// <summary>
    /// Check if a provider is available.
    /// </summary>
    bool IsAvailable(string providerName);
}
