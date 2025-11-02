using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PromptManagement;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Repository interface for prompt template persistence
/// </summary>
public interface IPromptRepository
{
    Task<PromptTemplate> CreateAsync(PromptTemplate template, CancellationToken ct = default);
    Task<PromptTemplate?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<PromptTemplate>> ListAsync(
        PromptCategory? category = null,
        PipelineStage? stage = null,
        TemplateSource? source = null,
        TemplateStatus? status = null,
        string? createdBy = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);
    Task<PromptTemplate> UpdateAsync(PromptTemplate template, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    
    Task<PromptTemplateVersion> CreateVersionAsync(PromptTemplateVersion version, CancellationToken ct = default);
    Task<List<PromptTemplateVersion>> GetVersionHistoryAsync(string templateId, CancellationToken ct = default);
    
    Task<PromptABTest> CreateABTestAsync(PromptABTest test, CancellationToken ct = default);
    Task<PromptABTest?> GetABTestAsync(string testId, CancellationToken ct = default);
    Task<PromptABTest> UpdateABTestAsync(PromptABTest test, CancellationToken ct = default);
    Task<List<PromptABTest>> ListABTestsAsync(ABTestStatus? status = null, CancellationToken ct = default);
}
