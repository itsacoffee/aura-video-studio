using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PromptManagement;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// In-memory implementation of prompt repository for development/testing
/// In production, this would be replaced with a database-backed implementation
/// </summary>
public class InMemoryPromptRepository : IPromptRepository
{
    private readonly Dictionary<string, PromptTemplate> _templates = new();
    private readonly Dictionary<string, List<PromptTemplateVersion>> _versions = new();
    private readonly Dictionary<string, PromptABTest> _abTests = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<PromptTemplate> CreateAsync(PromptTemplate template, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _templates[template.Id] = template;
            return template;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PromptTemplate?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return _templates.GetValueOrDefault(id);
    }

    public async Task<List<PromptTemplate>> ListAsync(
        PromptCategory? category = null,
        PipelineStage? stage = null,
        TemplateSource? source = null,
        TemplateStatus? status = null,
        string? createdBy = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var query = _templates.Values.AsEnumerable();

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        if (stage.HasValue)
            query = query.Where(t => t.Stage == stage.Value);

        if (source.HasValue)
            query = query.Where(t => t.Source == source.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrEmpty(createdBy))
            query = query.Where(t => t.CreatedBy == createdBy);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerSearch = searchTerm.ToLowerInvariant();
            query = query.Where(t =>
                t.Name.ToLowerInvariant().Contains(lowerSearch) ||
                t.Description.ToLowerInvariant().Contains(lowerSearch) ||
                t.Tags.Any(tag => tag.ToLowerInvariant().Contains(lowerSearch)));
        }

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<PromptTemplate> UpdateAsync(PromptTemplate template, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!_templates.ContainsKey(template.Id))
                throw new ArgumentException($"Template {template.Id} not found");

            _templates[template.Id] = template;
            return template;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _templates.Remove(id);
            _versions.Remove(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PromptTemplateVersion> CreateVersionAsync(
        PromptTemplateVersion version, 
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!_versions.ContainsKey(version.TemplateId))
                _versions[version.TemplateId] = new List<PromptTemplateVersion>();

            _versions[version.TemplateId].Add(version);
            return version;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<PromptTemplateVersion>> GetVersionHistoryAsync(
        string templateId, 
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return _versions.GetValueOrDefault(templateId) ?? new List<PromptTemplateVersion>();
    }

    public async Task<PromptABTest> CreateABTestAsync(PromptABTest test, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _abTests[test.Id] = test;
            return test;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PromptABTest?> GetABTestAsync(string testId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return _abTests.GetValueOrDefault(testId);
    }

    public async Task<PromptABTest> UpdateABTestAsync(PromptABTest test, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!_abTests.ContainsKey(test.Id))
                throw new ArgumentException($"A/B test {test.Id} not found");

            _abTests[test.Id] = test;
            return test;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<PromptABTest>> ListABTestsAsync(
        ABTestStatus? status = null, 
        CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var query = _abTests.Values.AsEnumerable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        return query.OrderByDescending(t => t.CreatedAt).ToList();
    }
}
