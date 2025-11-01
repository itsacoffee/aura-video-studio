using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Database entity for tracking first-run wizard completion status
/// </summary>
[Table("user_setup")]
public class UserSetupEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    public UserSetupEntity()
    {
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// User identifier (for future multi-user support, defaults to "default")
    /// </summary>
    [Required]
    [Column("user_id")]
    [MaxLength(100)]
    public string UserId { get; set; } = "default";

    /// <summary>
    /// Whether the user has completed the first-run wizard
    /// </summary>
    [Required]
    [Column("completed")]
    public bool Completed { get; set; } = false;

    /// <summary>
    /// When the wizard was completed
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Version of the wizard that was completed
    /// </summary>
    [Column("version")]
    [MaxLength(20)]
    public string? Version { get; set; }

    /// <summary>
    /// Last step completed (for resume functionality)
    /// </summary>
    [Column("last_step")]
    public int LastStep { get; set; } = 0;

    /// <summary>
    /// When the wizard was last updated
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Selected tier during setup (free, pro, etc.)
    /// </summary>
    [Column("selected_tier")]
    [MaxLength(50)]
    public string? SelectedTier { get; set; }

    /// <summary>
    /// JSON blob for storing additional wizard state
    /// </summary>
    [Column("wizard_state")]
    public string? WizardState { get; set; }
}
