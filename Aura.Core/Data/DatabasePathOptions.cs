namespace Aura.Core.Data;

/// <summary>
/// Provides configuration for resolving the SQLite database path.
/// </summary>
public class DatabasePathOptions
{
    /// <summary>
    /// Gets or sets the absolute path to the SQLite database file.
    /// </summary>
    public string? SqlitePath { get; set; }
}

