using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Api.HostedServices;

/// <summary>
/// Service that preloads common LLM prompts into cache on startup
/// </summary>
public class LlmPrewarmService : IHostedService
{
    private readonly ILogger<LlmPrewarmService> _logger;
    private readonly ILlmCache _cache;
    private readonly LlmCacheOptions _cacheOptions;
    private readonly LlmPrewarmOptions _prewarmOptions;
    
    public LlmPrewarmService(
        ILogger<LlmPrewarmService> logger,
        ILlmCache cache,
        IOptions<LlmCacheOptions> cacheOptions,
        IOptions<LlmPrewarmOptions> prewarmOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        _prewarmOptions = prewarmOptions?.Value ?? throw new ArgumentNullException(nameof(prewarmOptions));
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_cacheOptions.Enabled || !_prewarmOptions.Enabled)
        {
            _logger.LogInformation("LLM cache prewarming is disabled");
            return;
        }
        
        if (_prewarmOptions.PrewarmPrompts == null || _prewarmOptions.PrewarmPrompts.Count == 0)
        {
            _logger.LogInformation("No prewarm prompts configured");
            return;
        }
        
        _logger.LogInformation(
            "Starting LLM cache prewarming with {Count} prompts (maxConcurrent={MaxConcurrent})",
            _prewarmOptions.PrewarmPrompts.Count,
            _prewarmOptions.MaxConcurrentPrewarms);
        
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        
        _ = Task.Run(async () => await PrewarmCacheAsync(cancellationToken), cancellationToken);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LLM cache prewarming service stopped");
        return Task.CompletedTask;
    }
    
    private async Task PrewarmCacheAsync(CancellationToken ct)
    {
        var successCount = 0;
        var failureCount = 0;
        var startTime = DateTime.UtcNow;
        
        try
        {
            var semaphore = new SemaphoreSlim(_prewarmOptions.MaxConcurrentPrewarms);
            var tasks = _prewarmOptions.PrewarmPrompts.Select(async prompt =>
            {
                await semaphore.WaitAsync(ct);
                
                try
                {
                    await PrewarmSinglePromptAsync(prompt, ct);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to prewarm prompt: provider={Provider}, model={Model}, op={Operation}",
                        prompt.ProviderName,
                        prompt.ModelName,
                        prompt.OperationType);
                    
                    Interlocked.Increment(ref failureCount);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            await Task.WhenAll(tasks);
            
            var elapsed = DateTime.UtcNow - startTime;
            
            _logger.LogInformation(
                "Cache prewarming completed: {Success} succeeded, {Failed} failed, elapsed {Elapsed:0.00}s",
                successCount,
                failureCount,
                elapsed.TotalSeconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cache prewarming was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache prewarming");
        }
    }
    
    private async Task PrewarmSinglePromptAsync(PrewarmPrompt prompt, CancellationToken ct)
    {
        var cacheKey = LlmCacheKeyGenerator.GenerateKey(
            prompt.ProviderName,
            prompt.ModelName,
            prompt.OperationType,
            prompt.SystemPrompt,
            prompt.UserPrompt,
            prompt.Temperature,
            prompt.MaxTokens);
        
        var existing = await _cache.GetAsync(cacheKey, ct);
        
        if (existing != null)
        {
            _logger.LogDebug(
                "Skipping prewarm for {Provider}/{Model}/{Operation} - already cached",
                prompt.ProviderName,
                prompt.ModelName,
                prompt.OperationType);
            return;
        }
        
        var placeholderResponse = $"[Prewarmed placeholder for {prompt.OperationType}]";
        
        var metadata = new CacheMetadata
        {
            ProviderName = prompt.ProviderName,
            ModelName = prompt.ModelName,
            OperationType = prompt.OperationType,
            TtlSeconds = prompt.TtlSeconds
        };
        
        await _cache.SetAsync(cacheKey, placeholderResponse, metadata, ct);
        
        _logger.LogInformation(
            "Prewarmed cache entry: provider={Provider}, model={Model}, op={Operation}, ttl={Ttl}s",
            prompt.ProviderName,
            prompt.ModelName,
            prompt.OperationType,
            prompt.TtlSeconds);
    }
}
