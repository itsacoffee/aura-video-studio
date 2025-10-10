using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm.Validators;

/// <summary>
/// Validator for RuleBased LLM provider (always available as fallback)
/// </summary>
public class RuleBasedLlmValidator : ProviderValidator
{
    private readonly ILogger<RuleBasedLlmValidator> _logger;

    public override string ProviderName => "RuleBased";

    public RuleBasedLlmValidator(ILogger<RuleBasedLlmValidator> logger)
    {
        _logger = logger;
    }

    public override Task<ProviderValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        // RuleBased provider is always available as it's built-in
        return Task.FromResult(new ProviderValidationResult
        {
            IsAvailable = true,
            ProviderName = ProviderName,
            Details = "RuleBased provider is always available"
        });
    }
}
