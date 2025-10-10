using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images.Validators;

/// <summary>
/// Validator for stock image provider (always available as fallback)
/// </summary>
public class StockImageValidator : ProviderValidator
{
    private readonly ILogger<StockImageValidator> _logger;

    public override string ProviderName => "StockImages";

    public StockImageValidator(ILogger<StockImageValidator> logger)
    {
        _logger = logger;
    }

    public override Task<ProviderValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        // Stock images are always available as they're bundled with the application
        return Task.FromResult(new ProviderValidationResult
        {
            IsAvailable = true,
            ProviderName = ProviderName,
            Details = "Stock images are always available"
        });
    }
}
