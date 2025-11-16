using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Ollama;

namespace Aura.Core.AI.Tools;

/// <summary>
/// Interface for AI tool executors that can be called by LLMs
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Name of the tool
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get the tool definition for LLM
    /// </summary>
    OllamaToolDefinition GetToolDefinition();

    /// <summary>
    /// Execute the tool with the provided arguments
    /// </summary>
    /// <param name="arguments">Arguments as a JSON string</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result of the tool execution as a JSON string</returns>
    Task<string> ExecuteAsync(string arguments, CancellationToken ct);
}
