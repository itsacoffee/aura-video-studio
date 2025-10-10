using System.Threading.Tasks;

namespace Aura.Cli.Commands;

/// <summary>
/// Base interface for CLI commands
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Execute the command with the given arguments
    /// </summary>
    Task<int> ExecuteAsync(string[] args);
}

/// <summary>
/// Common options shared across commands
/// </summary>
public class CommandOptions
{
    public bool DryRun { get; set; }
    public bool Verbose { get; set; }
    public string? OutputDirectory { get; set; }
}
