using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Models.ContentSafety;
using Aura.Core.Services.ContentSafety;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for content safety and filtering
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContentSafetyController : ControllerBase
{
    private readonly ILogger<ContentSafetyController> _logger;
    private readonly ContentSafetyService _safetyService;
    private readonly KeywordListManager _keywordManager;
    private readonly TopicFilterManager _topicManager;
    private readonly ProviderSettings _providerSettings;
    private readonly string _policiesPath;
    private readonly string _auditLogPath;

    public ContentSafetyController(
        ILogger<ContentSafetyController> logger,
        ContentSafetyService safetyService,
        KeywordListManager keywordManager,
        TopicFilterManager topicManager,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _safetyService = safetyService;
        _keywordManager = keywordManager;
        _topicManager = topicManager;
        _providerSettings = providerSettings;
        
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _policiesPath = Path.Combine(auraDataDir, "content-safety-policies.json");
        _auditLogPath = Path.Combine(auraDataDir, "content-safety-audit.json");
    }

    /// <summary>
    /// Analyze content for safety violations
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeContent(
        [FromBody] SafetyAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content is required" });
            }

            var policy = await GetPolicyAsync(request.PolicyId, ct);
            if (policy == null)
            {
                return NotFound(new { error = "Policy not found" });
            }

            var result = await _safetyService.AnalyzeContentAsync(
                Guid.NewGuid().ToString(), 
                request.Content, 
                policy, 
                ct);

            var response = MapToAnalysisResponse(result);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content");
            return StatusCode(500, new { error = "Failed to analyze content" });
        }
    }

    /// <summary>
    /// Get all safety policies
    /// </summary>
    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicies(CancellationToken ct)
    {
        try
        {
            var policies = await LoadPoliciesAsync(ct);
            var response = policies.Select(MapToPolicyResponse).ToList();
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading policies");
            return StatusCode(500, new { error = "Failed to load policies" });
        }
    }

    /// <summary>
    /// Get specific safety policy
    /// </summary>
    [HttpGet("policies/{id}")]
    public async Task<IActionResult> GetPolicy(string id, CancellationToken ct)
    {
        try
        {
            var policy = await GetPolicyAsync(id, ct);
            if (policy == null)
            {
                return NotFound(new { error = "Policy not found" });
            }

            var response = MapToPolicyResponse(policy);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading policy");
            return StatusCode(500, new { error = "Failed to load policy" });
        }
    }

    /// <summary>
    /// Create new safety policy
    /// </summary>
    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy(
        [FromBody] SafetyPolicyRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Policy name is required" });
            }

            var policy = MapToPolicyModel(request);
            policy.Id = Guid.NewGuid().ToString();
            policy.CreatedAt = DateTime.UtcNow;
            policy.UpdatedAt = DateTime.UtcNow;

            var policies = await LoadPoliciesAsync(ct);
            policies.Add(policy);
            await SavePoliciesAsync(policies, ct);

            var response = MapToPolicyResponse(policy);
            return CreatedAtAction(nameof(GetPolicy), new { id = policy.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating policy");
            return StatusCode(500, new { error = "Failed to create policy" });
        }
    }

    /// <summary>
    /// Update existing safety policy
    /// </summary>
    [HttpPut("policies/{id}")]
    public async Task<IActionResult> UpdatePolicy(
        string id,
        [FromBody] SafetyPolicyRequest request,
        CancellationToken ct)
    {
        try
        {
            var policies = await LoadPoliciesAsync(ct);
            var existing = policies.FirstOrDefault(p => p.Id == id);
            
            if (existing == null)
            {
                return NotFound(new { error = "Policy not found" });
            }

            var updated = MapToPolicyModel(request);
            updated.Id = existing.Id;
            updated.CreatedAt = existing.CreatedAt;
            updated.UpdatedAt = DateTime.UtcNow;
            updated.UsageCount = existing.UsageCount;
            updated.LastUsedAt = existing.LastUsedAt;

            policies.Remove(existing);
            policies.Add(updated);
            await SavePoliciesAsync(policies, ct);

            var response = MapToPolicyResponse(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating policy");
            return StatusCode(500, new { error = "Failed to update policy" });
        }
    }

    /// <summary>
    /// Delete safety policy
    /// </summary>
    [HttpDelete("policies/{id}")]
    public async Task<IActionResult> DeletePolicy(string id, CancellationToken ct)
    {
        try
        {
            var policies = await LoadPoliciesAsync(ct);
            var policy = policies.FirstOrDefault(p => p.Id == id);
            
            if (policy == null)
            {
                return NotFound(new { error = "Policy not found" });
            }

            if (policy.IsDefault)
            {
                return BadRequest(new { error = "Cannot delete default policy" });
            }

            policies.Remove(policy);
            await SavePoliciesAsync(policies, ct);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting policy");
            return StatusCode(500, new { error = "Failed to delete policy" });
        }
    }

    /// <summary>
    /// Get preset policies
    /// </summary>
    [HttpGet("presets")]
    public IActionResult GetPresets()
    {
        try
        {
            var presets = SafetyPolicyPresets.GetAllPresets();
            var response = presets.Select(p => MapToPolicyResponse(p.Value)).ToList();
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading presets");
            return StatusCode(500, new { error = "Failed to load presets" });
        }
    }

    /// <summary>
    /// Import keyword list
    /// </summary>
    [HttpPost("keywords/import")]
    public IActionResult ImportKeywords([FromBody] ImportKeywordsRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { error = "Text is required" });
            }

            var action = string.IsNullOrWhiteSpace(request.DefaultAction)
                ? SafetyAction.Warn
                : Enum.Parse<SafetyAction>(request.DefaultAction, true);

            var rules = _keywordManager.ImportFromText(request.Text, action);
            var response = rules.Select(MapToKeywordRuleDto).ToList();

            return Ok(new { count = rules.Count, rules = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing keywords");
            return StatusCode(500, new { error = "Failed to import keywords" });
        }
    }

    /// <summary>
    /// Get starter keyword lists
    /// </summary>
    [HttpGet("keywords/starter-lists")]
    public IActionResult GetStarterLists()
    {
        try
        {
            var lists = _keywordManager.GetStarterLists();
            return Ok(lists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading starter lists");
            return StatusCode(500, new { error = "Failed to load starter lists" });
        }
    }

    /// <summary>
    /// Get common topics for filtering
    /// </summary>
    [HttpGet("topics/common")]
    public IActionResult GetCommonTopics()
    {
        try
        {
            var topics = _topicManager.GetCommonTopics();
            return Ok(topics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading common topics");
            return StatusCode(500, new { error = "Failed to load common topics" });
        }
    }

    /// <summary>
    /// Record safety decision for audit trail
    /// </summary>
    [HttpPost("audit")]
    public async Task<IActionResult> RecordDecision(
        [FromBody] SafetyDecisionRequest request,
        CancellationToken ct)
    {
        try
        {
            var auditLog = new SafetyAuditLog
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                ContentId = request.ContentId,
                PolicyId = request.PolicyId,
                UserId = "user",
                Decision = Enum.Parse<SafetyDecision>(request.Decision, true),
                DecisionReason = request.DecisionReason,
                OverriddenViolations = request.OverriddenViolations ?? new List<string>()
            };

            await AppendAuditLogAsync(auditLog, ct);

            return Ok(new { id = auditLog.Id, timestamp = auditLog.Timestamp });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording decision");
            return StatusCode(500, new { error = "Failed to record decision" });
        }
    }

    /// <summary>
    /// Get audit logs with optional filtering
    /// </summary>
    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? contentId,
        [FromQuery] string? policyId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        try
        {
            var logs = await LoadAuditLogsAsync(ct);

            if (!string.IsNullOrWhiteSpace(contentId))
            {
                logs = logs.Where(l => l.ContentId == contentId).ToList();
            }

            if (!string.IsNullOrWhiteSpace(policyId))
            {
                logs = logs.Where(l => l.PolicyId == policyId).ToList();
            }

            var total = logs.Count;
            var page = logs.OrderByDescending(l => l.Timestamp).Skip(skip).Take(take).ToList();

            return Ok(new { total, skip, take, logs = page });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit logs");
            return StatusCode(500, new { error = "Failed to load audit logs" });
        }
    }

    private async Task<SafetyPolicy?> GetPolicyAsync(string? policyId, CancellationToken ct)
    {
        var policies = await LoadPoliciesAsync(ct);
        
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return policies.FirstOrDefault(p => p.IsDefault) ?? SafetyPolicyPresets.GetModeratePolicy();
        }

        return policies.FirstOrDefault(p => p.Id == policyId) ?? SafetyPolicyPresets.GetModeratePolicy();
    }

    private async Task<List<SafetyPolicy>> LoadPoliciesAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_policiesPath))
        {
            var defaultPolicies = SafetyPolicyPresets.GetAllPresets()
                .Select(p => p.Value)
                .ToList();
            defaultPolicies.First(p => p.Preset == SafetyPolicyPreset.Moderate).IsDefault = true;
            await SavePoliciesAsync(defaultPolicies, ct);
            return defaultPolicies;
        }

        var json = await System.IO.File.ReadAllTextAsync(_policiesPath, ct);
        return JsonSerializer.Deserialize<List<SafetyPolicy>>(json) ?? new List<SafetyPolicy>();
    }

    private async Task SavePoliciesAsync(List<SafetyPolicy> policies, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(policies, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(_policiesPath, json, ct);
    }

    private async Task<List<SafetyAuditLog>> LoadAuditLogsAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_auditLogPath))
        {
            return new List<SafetyAuditLog>();
        }

        var json = await System.IO.File.ReadAllTextAsync(_auditLogPath, ct);
        return JsonSerializer.Deserialize<List<SafetyAuditLog>>(json) ?? new List<SafetyAuditLog>();
    }

    private async Task AppendAuditLogAsync(SafetyAuditLog log, CancellationToken ct)
    {
        var logs = await LoadAuditLogsAsync(ct);
        logs.Add(log);
        
        if (logs.Count > 10000)
        {
            logs = logs.OrderByDescending(l => l.Timestamp).Take(10000).ToList();
        }

        var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(_auditLogPath, json, ct);
    }

    private SafetyAnalysisResponse MapToAnalysisResponse(SafetyAnalysisResult result)
    {
        return new SafetyAnalysisResponse(
            result.IsSafe,
            result.OverallSafetyScore,
            result.Violations.Select(v => new SafetyViolationDto(
                v.Id,
                v.Category.ToString(),
                v.SeverityScore,
                v.Reason,
                v.MatchedContent,
                v.Position,
                v.RecommendedAction.ToString(),
                v.SuggestedFix,
                v.CanOverride
            )).ToList(),
            result.Warnings.Select(w => new SafetyWarningDto(
                w.Id,
                w.Category.ToString(),
                w.Message,
                w.Context,
                w.Suggestions
            )).ToList(),
            result.CategoryScores.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value
            ),
            result.RequiresReview,
            result.AllowWithDisclaimer,
            result.RecommendedDisclaimer,
            result.SuggestedFixes
        );
    }

    private SafetyPolicyResponse MapToPolicyResponse(SafetyPolicy policy)
    {
        return new SafetyPolicyResponse(
            policy.Id,
            policy.Name,
            policy.Description,
            policy.IsEnabled,
            policy.AllowUserOverride,
            policy.Preset.ToString(),
            policy.Categories.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => new SafetyCategoryDto(
                    kvp.Value.Type.ToString(),
                    kvp.Value.Threshold,
                    kvp.Value.IsEnabled,
                    kvp.Value.DefaultAction.ToString(),
                    kvp.Value.SeverityActions?.ToDictionary(sa => sa.Key, sa => sa.Value.ToString()),
                    kvp.Value.CustomGuidelines
                )
            ),
            policy.KeywordRules.Select(MapToKeywordRuleDto).ToList(),
            policy.TopicFilters.Select(t => new TopicFilterDto(
                t.Id,
                t.Topic,
                t.IsBlocked,
                t.ConfidenceThreshold,
                t.Action.ToString(),
                t.Subtopics,
                t.AllowedContexts
            )).ToList(),
            policy.BrandSafety != null ? new BrandSafetySettingsDto(
                policy.BrandSafety.RequiredKeywords,
                policy.BrandSafety.BannedCompetitors,
                policy.BrandSafety.BrandTerminology,
                policy.BrandSafety.BrandVoiceGuidelines,
                policy.BrandSafety.RequiredDisclaimers,
                policy.BrandSafety.MinBrandVoiceScore
            ) : null,
            policy.AgeSettings != null ? new AgeAppropriatenessSettingsDto(
                policy.AgeSettings.MinimumAge,
                policy.AgeSettings.MaximumAge,
                policy.AgeSettings.TargetRating.ToString(),
                policy.AgeSettings.RequireParentalGuidance,
                policy.AgeSettings.AgeSpecificRestrictions
            ) : null,
            policy.CulturalSettings != null ? new CulturalSensitivitySettingsDto(
                policy.CulturalSettings.TargetRegions,
                policy.CulturalSettings.CulturalTaboos,
                policy.CulturalSettings.AvoidStereotypes,
                policy.CulturalSettings.RequireInclusiveLanguage,
                policy.CulturalSettings.ReligiousSensitivities
            ) : null,
            policy.ComplianceSettings != null ? new ComplianceSettingsDto(
                policy.ComplianceSettings.RequiredDisclosures,
                policy.ComplianceSettings.CoppaCompliant,
                policy.ComplianceSettings.GdprCompliant,
                policy.ComplianceSettings.FtcCompliant,
                policy.ComplianceSettings.IndustryRegulations,
                policy.ComplianceSettings.AutoDisclosures
            ) : null,
            policy.IsDefault,
            policy.UsageCount,
            policy.LastUsedAt,
            policy.CreatedAt,
            policy.UpdatedAt
        );
    }

    private KeywordRuleDto MapToKeywordRuleDto(KeywordRule rule)
    {
        return new KeywordRuleDto(
            rule.Id,
            rule.Keyword,
            rule.MatchType.ToString(),
            rule.IsCaseSensitive,
            rule.Action.ToString(),
            rule.Replacement,
            rule.ContextExceptions,
            rule.IsRegex
        );
    }

    private SafetyPolicy MapToPolicyModel(SafetyPolicyRequest request)
    {
        var policy = new SafetyPolicy
        {
            Name = request.Name,
            Description = request.Description,
            IsEnabled = request.IsEnabled,
            AllowUserOverride = request.AllowUserOverride,
            Preset = Enum.Parse<SafetyPolicyPreset>(request.Preset, true)
        };

        if (request.Categories != null)
        {
            foreach (var (key, cat) in request.Categories)
            {
                var categoryType = Enum.Parse<SafetyCategoryType>(key, true);
                policy.Categories[categoryType] = new SafetyCategory
                {
                    Type = Enum.Parse<SafetyCategoryType>(cat.Type, true),
                    Threshold = cat.Threshold,
                    IsEnabled = cat.IsEnabled,
                    DefaultAction = Enum.Parse<SafetyAction>(cat.DefaultAction, true),
                    SeverityActions = cat.SeverityActions?.ToDictionary(
                        sa => sa.Key,
                        sa => Enum.Parse<SafetyAction>(sa.Value, true)
                    ) ?? new Dictionary<int, SafetyAction>(),
                    CustomGuidelines = cat.CustomGuidelines
                };
            }
        }

        if (request.KeywordRules != null)
        {
            policy.KeywordRules = request.KeywordRules.Select(k => new KeywordRule
            {
                Id = k.Id ?? Guid.NewGuid().ToString(),
                Keyword = k.Keyword,
                MatchType = Enum.Parse<KeywordMatchType>(k.MatchType, true),
                IsCaseSensitive = k.IsCaseSensitive,
                Action = Enum.Parse<SafetyAction>(k.Action, true),
                Replacement = k.Replacement,
                ContextExceptions = k.ContextExceptions ?? new List<string>(),
                IsRegex = k.IsRegex
            }).ToList();
        }

        if (request.TopicFilters != null)
        {
            policy.TopicFilters = request.TopicFilters.Select(t => new TopicFilter
            {
                Id = t.Id ?? Guid.NewGuid().ToString(),
                Topic = t.Topic,
                IsBlocked = t.IsBlocked,
                ConfidenceThreshold = t.ConfidenceThreshold,
                Action = Enum.Parse<SafetyAction>(t.Action, true),
                Subtopics = t.Subtopics ?? new List<string>(),
                AllowedContexts = t.AllowedContexts ?? new List<string>()
            }).ToList();
        }

        if (request.BrandSafety != null)
        {
            policy.BrandSafety = new BrandSafetySettings
            {
                RequiredKeywords = request.BrandSafety.RequiredKeywords ?? new List<string>(),
                BannedCompetitors = request.BrandSafety.BannedCompetitors ?? new List<string>(),
                BrandTerminology = request.BrandSafety.BrandTerminology ?? new List<string>(),
                BrandVoiceGuidelines = request.BrandSafety.BrandVoiceGuidelines,
                RequiredDisclaimers = request.BrandSafety.RequiredDisclaimers ?? new List<string>(),
                MinBrandVoiceScore = request.BrandSafety.MinBrandVoiceScore
            };
        }

        if (request.AgeSettings != null)
        {
            policy.AgeSettings = new AgeAppropriatenessSettings
            {
                MinimumAge = request.AgeSettings.MinimumAge,
                MaximumAge = request.AgeSettings.MaximumAge,
                TargetRating = Enum.Parse<ContentRating>(request.AgeSettings.TargetRating, true),
                RequireParentalGuidance = request.AgeSettings.RequireParentalGuidance,
                AgeSpecificRestrictions = request.AgeSettings.AgeSpecificRestrictions ?? new List<string>()
            };
        }

        if (request.CulturalSettings != null)
        {
            policy.CulturalSettings = new CulturalSensitivitySettings
            {
                TargetRegions = request.CulturalSettings.TargetRegions ?? new List<string>(),
                CulturalTaboos = request.CulturalSettings.CulturalTaboos ?? new Dictionary<string, List<string>>(),
                AvoidStereotypes = request.CulturalSettings.AvoidStereotypes,
                RequireInclusiveLanguage = request.CulturalSettings.RequireInclusiveLanguage,
                ReligiousSensitivities = request.CulturalSettings.ReligiousSensitivities ?? new List<string>()
            };
        }

        if (request.ComplianceSettings != null)
        {
            policy.ComplianceSettings = new ComplianceSettings
            {
                RequiredDisclosures = request.ComplianceSettings.RequiredDisclosures ?? new List<string>(),
                CoppaCompliant = request.ComplianceSettings.CoppaCompliant,
                GdprCompliant = request.ComplianceSettings.GdprCompliant,
                FtcCompliant = request.ComplianceSettings.FtcCompliant,
                IndustryRegulations = request.ComplianceSettings.IndustryRegulations ?? new List<string>(),
                AutoDisclosures = request.ComplianceSettings.AutoDisclosures ?? new Dictionary<string, string>()
            };
        }

        return policy;
    }
}
