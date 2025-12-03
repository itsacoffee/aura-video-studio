using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Templates;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Templates;

/// <summary>
/// Service for managing and applying video structure templates.
/// </summary>
public class VideoTemplateService : IVideoTemplateService
{
    private readonly ILogger<VideoTemplateService> _logger;
    private readonly IReadOnlyList<VideoTemplate> _templates;

    /// <summary>
    /// Regex pattern to match template variables in the format {{variableName}}.
    /// Used for interpolating user-provided values into prompt templates.
    /// Example: "Write about {{topic}} for {{audience}}" with variables 
    /// {topic: "Python", audience: "beginners"} becomes 
    /// "Write about Python for beginners".
    /// </summary>
    private static readonly Regex VariablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public VideoTemplateService(ILogger<VideoTemplateService> logger)
    {
        _logger = logger;
        _templates = BuiltInScriptTemplates.GetAll();
    }

    /// <inheritdoc />
    public IReadOnlyList<VideoTemplate> GetAllTemplates() => _templates;

    /// <inheritdoc />
    public VideoTemplate? GetTemplateById(string id)
    {
        return _templates.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<VideoTemplate> GetTemplatesByCategory(string category)
    {
        return _templates
            .Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<VideoTemplate> SearchTemplates(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _templates;

        var lowerQuery = query.ToLowerInvariant();

        return _templates.Where(t =>
            t.Name.ToLowerInvariant().Contains(lowerQuery) ||
            t.Description.ToLowerInvariant().Contains(lowerQuery) ||
            t.Category.ToLowerInvariant().Contains(lowerQuery) ||
            t.Metadata.Tags.Any(tag => tag.ToLowerInvariant().Contains(lowerQuery)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<TemplatedBrief> ApplyTemplateAsync(
        string templateId,
        IDictionary<string, string> variableValues,
        string? language = null,
        CancellationToken ct = default)
    {
        var template = GetTemplateById(templateId)
            ?? throw new ArgumentException($"Template '{templateId}' not found", nameof(templateId));

        // Validate variables
        var (isValid, errors) = ValidateVariables(templateId, variableValues);
        if (!isValid)
        {
            throw new ArgumentException($"Invalid variables: {string.Join(", ", errors)}");
        }

        _logger.LogInformation("Applying template {TemplateId} with variables: {Variables}",
            templateId, string.Join(", ", variableValues.Select(kv => $"{kv.Key}={kv.Value}")));

        // Generate sections
        var sections = await GenerateSectionsAsync(template, variableValues, ct).ConfigureAwait(false);

        // Calculate total duration
        var totalDuration = sections.Aggregate(TimeSpan.Zero, (sum, s) => sum + s.SuggestedDuration);

        // Create Brief from template
        var topic = variableValues.TryGetValue("topic", out var t) ? t :
                   variableValues.TryGetValue("product", out var p) ? p :
                   variableValues.TryGetValue("optionA", out var a) ? $"{a} comparison" :
                   template.Name;

        var audience = variableValues.TryGetValue("audience", out var aud) ? aud : "General";

        var brief = new Brief(
            Topic: topic,
            Audience: audience,
            Goal: template.Category switch
            {
                "Educational" => "Educate",
                "Marketing" => "Persuade",
                "Entertainment" => "Entertain",
                "Reviews" => "Inform",
                _ => "Inform"
            },
            Tone: template.Metadata.RecommendedTones.FirstOrDefault() ?? "Informative",
            Language: language ?? "en-US",
            Aspect: template.Metadata.SupportedAspects.FirstOrDefault());

        var planSpec = new PlanSpec(
            TargetDuration: totalDuration,
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard",
            TargetSceneCount: sections.Count);

        return new TemplatedBrief(brief, planSpec, sections, template);
    }

    /// <inheritdoc />
    public async Task<ScriptPreviewResponse> PreviewScriptAsync(
        string templateId,
        IDictionary<string, string> variableValues,
        CancellationToken ct = default)
    {
        var template = GetTemplateById(templateId)
            ?? throw new ArgumentException($"Template '{templateId}' not found", nameof(templateId));

        // Validate variables
        var (isValid, errors) = ValidateVariables(templateId, variableValues);
        if (!isValid)
        {
            throw new ArgumentException($"Invalid variables: {string.Join(", ", errors)}");
        }

        // Generate sections
        var sections = await GenerateSectionsAsync(template, variableValues, ct).ConfigureAwait(false);

        // Build script from sections
        var scriptBuilder = new StringBuilder();
        foreach (var section in sections)
        {
            scriptBuilder.AppendLine($"## {section.Name}");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine(section.Content);
            scriptBuilder.AppendLine();
        }

        var totalDuration = sections.Aggregate(TimeSpan.Zero, (sum, s) => sum + s.SuggestedDuration);

        return new ScriptPreviewResponse(
            Script: scriptBuilder.ToString().Trim(),
            Sections: sections,
            EstimatedDuration: totalDuration,
            SceneCount: sections.Count);
    }

    /// <inheritdoc />
    public (bool IsValid, IReadOnlyList<string> Errors) ValidateVariables(
        string templateId,
        IDictionary<string, string> variableValues)
    {
        var template = GetTemplateById(templateId);
        if (template == null)
        {
            return (false, new[] { $"Template '{templateId}' not found" });
        }

        var errors = new List<string>();

        foreach (var variable in template.Variables)
        {
            var hasValue = variableValues.TryGetValue(variable.Name, out var value);

            // Check required variables
            if (variable.IsRequired && (!hasValue || string.IsNullOrWhiteSpace(value)))
            {
                errors.Add($"'{variable.DisplayName}' is required");
                continue;
            }

            if (!hasValue || string.IsNullOrWhiteSpace(value))
                continue;

            // Validate number type
            if (variable.Type == VariableType.Number)
            {
                if (!int.TryParse(value, out var numValue))
                {
                    errors.Add($"'{variable.DisplayName}' must be a number");
                }
                else
                {
                    if (variable.MinValue.HasValue && numValue < variable.MinValue.Value)
                    {
                        errors.Add($"'{variable.DisplayName}' must be at least {variable.MinValue}");
                    }
                    if (variable.MaxValue.HasValue && numValue > variable.MaxValue.Value)
                    {
                        errors.Add($"'{variable.DisplayName}' must be at most {variable.MaxValue}");
                    }
                }
            }

            // Validate selection type
            if (variable.Type == VariableType.Selection && variable.Options != null)
            {
                if (!variable.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"'{variable.DisplayName}' must be one of: {string.Join(", ", variable.Options)}");
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    private async Task<IReadOnlyList<GeneratedSection>> GenerateSectionsAsync(
        VideoTemplate template,
        IDictionary<string, string> variableValues,
        CancellationToken ct)
    {
        var sections = new List<GeneratedSection>();

        foreach (var templateSection in template.Structure.Sections)
        {
            if (templateSection.IsRepeatable && templateSection.RepeatCountVariable != null)
            {
                // Handle repeatable sections (like numbered items in listicles)
                if (variableValues.TryGetValue(templateSection.RepeatCountVariable, out var countStr) &&
                    int.TryParse(countStr, out var count))
                {
                    for (int i = 1; i <= count; i++)
                    {
                        var itemVariables = new Dictionary<string, string>(variableValues)
                        {
                            ["itemNumber"] = i.ToString(),
                            ["stepNumber"] = i.ToString()
                        };

                        var content = await GenerateSectionContentAsync(
                            templateSection, itemVariables, ct).ConfigureAwait(false);

                        sections.Add(new GeneratedSection(
                            Name: $"{templateSection.Name} #{i}",
                            Content: content,
                            SuggestedDuration: templateSection.SuggestedDuration,
                            Type: templateSection.Type));
                    }
                }
            }
            else
            {
                var content = await GenerateSectionContentAsync(
                    templateSection, variableValues, ct).ConfigureAwait(false);

                sections.Add(new GeneratedSection(
                    Name: templateSection.Name,
                    Content: content,
                    SuggestedDuration: templateSection.SuggestedDuration,
                    Type: templateSection.Type));
            }
        }

        return sections;
    }

    private Task<string> GenerateSectionContentAsync(
        TemplateSection section,
        IDictionary<string, string> variableValues,
        CancellationToken ct)
    {
        // Generate fallback content based on section type and variables.
        // This provides sensible default content when no LLM is available.
        // When an LLM provider is integrated, this method can be enhanced to use
        // the section's PromptTemplate with the LLM for higher-quality content generation.
        var content = GenerateFallbackContent(section, variableValues);
        return Task.FromResult(content);
    }

    /// <summary>
    /// Replaces variable placeholders in a template string with actual values.
    /// </summary>
    /// <param name="template">Template string containing {{variableName}} placeholders</param>
    /// <param name="variables">Dictionary of variable names to values</param>
    /// <returns>Interpolated string with variables replaced</returns>
    private static string InterpolateVariables(string template, IDictionary<string, string> variables)
    {
        return VariablePattern.Replace(template, match =>
        {
            var varName = match.Groups[1].Value;
            return variables.TryGetValue(varName, out var value) ? value : match.Value;
        });
    }

    /// <summary>
    /// Generates reasonable default content for a section based on its type.
    /// This is used as fallback content generation when no LLM provider is available.
    /// </summary>
    private static string GenerateFallbackContent(
        TemplateSection section,
        IDictionary<string, string> variables)
    {
        // Generate reasonable fallback content based on section type
        var topic = variables.TryGetValue("topic", out var t) ? t :
                   variables.TryGetValue("product", out var p) ? p : "the topic";

        return section.Type switch
        {
            SectionType.Hook => $"Let's dive into {topic}. This is going to be interesting!",
            SectionType.Introduction => $"Welcome! Today we're exploring {topic}. Here's what you need to know.",
            SectionType.Problem => $"Many people struggle with {topic}. Let's understand the challenges.",
            SectionType.Solution => $"Here's how {topic} can help solve these challenges.",
            SectionType.MainPoint => $"The key thing to remember about {topic} is this...",
            SectionType.CallToAction => $"Thanks for watching! If you found this helpful, don't forget to subscribe.",
            SectionType.Conclusion => $"That wraps up our discussion of {topic}. Let's summarize the key points.",
            SectionType.NumberedItem => $"Here's an important point about {topic} that you should know.",
            SectionType.Recap => $"Let's quickly recap what we've covered about {topic}.",
            SectionType.Setup => $"Let me set the scene for this story about {topic}.",
            SectionType.Climax => $"And then came the turning point...",
            SectionType.Resolution => $"Here's how everything turned out.",
            SectionType.Lesson => $"The key lesson from this experience is...",
            SectionType.Overview => $"In this tutorial, we'll cover {topic} step by step.",
            SectionType.Prerequisites => $"Before we start with {topic}, make sure you have...",
            SectionType.Step => $"For this step, we'll focus on...",
            SectionType.CommonMistakes => $"Watch out for these common mistakes when working with {topic}.",
            SectionType.Summary => $"To summarize, we've learned how to...",
            SectionType.Attention => $"Stop scrolling! You need to see this.",
            SectionType.Interest => $"Here's why this matters to you.",
            SectionType.Desire => $"Imagine what you could achieve with this.",
            SectionType.Action => $"Don't wait - take action now!",
            SectionType.OptionA => $"Let's look at the first option.",
            SectionType.OptionB => $"Now let's consider the second option.",
            SectionType.Verdict => $"After careful consideration, here's my recommendation.",
            _ => $"Content about {topic}."
        };
    }
}
