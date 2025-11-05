using System.Threading;
using System.Threading.Tasks;
using LicensingModels = Aura.Core.Models.Licensing;

namespace Aura.Core.Services.Licensing;

/// <summary>
/// Service for managing licensing and provenance information
/// </summary>
public interface ILicensingService
{
    /// <summary>
    /// Generate licensing manifest for a project
    /// </summary>
    /// <param name="projectId">Project/Job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete licensing manifest</returns>
    Task<LicensingModels.ProjectLicensingManifest> GenerateManifestAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export licensing manifest in specified format
    /// </summary>
    /// <param name="manifest">Licensing manifest</param>
    /// <param name="format">Export format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported content as string</returns>
    Task<string> ExportManifestAsync(LicensingModels.ProjectLicensingManifest manifest, LicensingModels.LicensingExportFormat format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate manifest and check for issues
    /// </summary>
    /// <param name="manifest">Manifest to validate</param>
    /// <returns>True if manifest is valid for export</returns>
    bool ValidateManifest(LicensingModels.ProjectLicensingManifest manifest);

    /// <summary>
    /// Record sign-off for licensing
    /// </summary>
    /// <param name="signOff">Sign-off information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordSignOffAsync(LicensingModels.LicensingSignOff signOff, CancellationToken cancellationToken = default);
}
