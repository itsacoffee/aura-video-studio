using System;

namespace Aura.Core.Data;

/// <summary>
/// Interface for entities that track creation and modification timestamps
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// When the entity was created
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the entity was last updated
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who created the entity (optional)
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// User who last modified the entity (optional)
    /// </summary>
    string? ModifiedBy { get; set; }
}

/// <summary>
/// Interface for entities that support soft delete
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Whether the entity has been soft-deleted
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// When the entity was deleted
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the entity
    /// </summary>
    string? DeletedBy { get; set; }
}

/// <summary>
/// Interface for entities with row versioning for optimistic concurrency
/// </summary>
public interface IVersionedEntity
{
    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    byte[]? RowVersion { get; set; }
}
