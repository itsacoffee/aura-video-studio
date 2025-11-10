using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Data;

/// <summary>
/// Implementation of the Unit of Work pattern
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AuraDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    // Lazy-initialized repositories
    private ProjectStateRepository? _projectStates;
    private ProjectVersionRepository? _projectVersions;
    private ConfigurationRepository? _configurations;
    private IRepository<ExportHistoryEntity, string>? _exportHistory;
    private IRepository<TemplateEntity, string>? _templates;
    private IRepository<UserSetupEntity, string>? _userSetups;
    private IRepository<CustomTemplateEntity, string>? _customTemplates;
    private IRepository<ActionLogEntity, Guid>? _actionLogs;
    private IRepository<SystemConfigurationEntity, int>? _systemConfigurations;

    public UnitOfWork(AuraDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ProjectStateRepository ProjectStates =>
        _projectStates ??= new ProjectStateRepository(
            _context, 
            _logger.CreateLogger<ProjectStateRepository>());

    public ProjectVersionRepository ProjectVersions =>
        _projectVersions ??= new ProjectVersionRepository(
            _context,
            _logger.CreateLogger<ProjectVersionRepository>());

    public ConfigurationRepository Configurations =>
        _configurations ??= new ConfigurationRepository(
            _context,
            _logger.CreateLogger<ConfigurationRepository>());

    public IRepository<ExportHistoryEntity, string> ExportHistory =>
        _exportHistory ??= new GenericRepository<ExportHistoryEntity, string>(
            _context,
            _logger.CreateLogger<GenericRepository<ExportHistoryEntity, string>>());

    public IRepository<TemplateEntity, string> Templates =>
        _templates ??= new GenericRepository<TemplateEntity, string>(
            _context,
            _logger.CreateLogger<GenericRepository<TemplateEntity, string>>());

    public IRepository<UserSetupEntity, string> UserSetups =>
        _userSetups ??= new GenericRepository<UserSetupEntity, string>(
            _context,
            _logger.CreateLogger<GenericRepository<UserSetupEntity, string>>());

    public IRepository<CustomTemplateEntity, string> CustomTemplates =>
        _customTemplates ??= new GenericRepository<CustomTemplateEntity, string>(
            _context,
            _logger.CreateLogger<GenericRepository<CustomTemplateEntity, string>>());

    public IRepository<ActionLogEntity, Guid> ActionLogs =>
        _actionLogs ??= new GenericRepository<ActionLogEntity, Guid>(
            _context,
            _logger.CreateLogger<GenericRepository<ActionLogEntity, Guid>>());

    public IRepository<SystemConfigurationEntity, int> SystemConfigurations =>
        _systemConfigurations ??= new GenericRepository<SystemConfigurationEntity, int>(
            _context,
            _logger.CreateLogger<GenericRepository<SystemConfigurationEntity, int>>());

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while saving changes");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while saving changes");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _transaction = await _context.Database.BeginTransactionAsync(ct);
        _logger.LogDebug("Database transaction started");
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit");
        }

        try
        {
            await _transaction.CommitAsync(ct);
            _logger.LogDebug("Database transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            await RollbackAsync(ct);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to rollback");
        }

        try
        {
            await _transaction.RollbackAsync(ct);
            _logger.LogWarning("Database transaction rolled back");
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
