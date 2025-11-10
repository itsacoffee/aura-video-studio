using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Data;

/// <summary>
/// Unit of work pattern for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Project state repository
    /// </summary>
    ProjectStateRepository ProjectStates { get; }

    /// <summary>
    /// Project version repository
    /// </summary>
    ProjectVersionRepository ProjectVersions { get; }

    /// <summary>
    /// Configuration repository
    /// </summary>
    ConfigurationRepository Configurations { get; }

    /// <summary>
    /// Export history repository
    /// </summary>
    IRepository<ExportHistoryEntity, string> ExportHistory { get; }

    /// <summary>
    /// Template repository
    /// </summary>
    IRepository<TemplateEntity, string> Templates { get; }

    /// <summary>
    /// User setup repository
    /// </summary>
    IRepository<UserSetupEntity, string> UserSetups { get; }

    /// <summary>
    /// Custom template repository
    /// </summary>
    IRepository<CustomTemplateEntity, string> CustomTemplates { get; }

    /// <summary>
    /// Action log repository
    /// </summary>
    IRepository<ActionLogEntity, Guid> ActionLogs { get; }

    /// <summary>
    /// System configuration repository
    /// </summary>
    IRepository<SystemConfigurationEntity, int> SystemConfigurations { get; }

    /// <summary>
    /// Save all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Begin a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackAsync(CancellationToken ct = default);
}
