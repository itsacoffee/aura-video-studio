using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.PromptManagement;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Hosted service that initializes system prompt templates on application startup
/// </summary>
public class SystemPromptInitializer : IHostedService
{
    private readonly ILogger<SystemPromptInitializer> _logger;
    private readonly IPromptRepository _repository;

    public SystemPromptInitializer(
        ILogger<SystemPromptInitializer> logger,
        IPromptRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing system prompt templates");

        try
        {
            var systemTemplates = SystemPromptTemplateFactory.CreateSystemTemplates();

            foreach (var template in systemTemplates)
            {
                var existing = await _repository.GetByIdAsync(template.Id, cancellationToken);
                if (existing == null)
                {
                    await _repository.CreateAsync(template, cancellationToken);
                    _logger.LogInformation("Created system template: {Name}", template.Name);
                }
                else
                {
                    _logger.LogDebug("System template already exists: {Name}", template.Name);
                }
            }

            _logger.LogInformation("System prompt templates initialized: {Count} templates",
                systemTemplates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system prompt templates");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
