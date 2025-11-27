using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Localization;

/// <summary>
/// Metrics for monitoring translation quality and performance
/// </summary>
public class TranslationMetrics
{
    /// <summary>
    /// Ratio of translation length to source length
    /// </summary>
    public double LengthRatio { get; set; }

    /// <summary>
    /// Whether the response contained JSON/XML structured artifacts
    /// </summary>
    public bool HasStructuredArtifacts { get; set; }

    /// <summary>
    /// Whether the response contained unwanted prefixes like "Translation:"
    /// </summary>
    public bool HasUnwantedPrefixes { get; set; }

    /// <summary>
    /// Number of characters in the translated text
    /// </summary>
    public int CharacterCount { get; set; }

    /// <summary>
    /// Number of words in the translated text
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Time taken to perform the translation in seconds
    /// </summary>
    public double TranslationTimeSeconds { get; set; }

    /// <summary>
    /// Name of the LLM provider used
    /// </summary>
    public string ProviderUsed { get; set; } = string.Empty;

    /// <summary>
    /// Model identifier or name used for translation
    /// </summary>
    public string ModelIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// List of quality issues detected during translation
    /// </summary>
    public List<string> QualityIssues { get; set; } = new();

    /// <summary>
    /// Overall quality grade based on detected issues
    /// </summary>
    public TranslationQualityGrade Grade { get; set; }
}

/// <summary>
/// Quality grade for translation output
/// </summary>
public enum TranslationQualityGrade
{
    /// <summary>
    /// Clean output, appropriate length, fast processing
    /// </summary>
    Excellent,

    /// <summary>
    /// Minor issues that were cleaned up automatically
    /// </summary>
    Good,

    /// <summary>
    /// Multiple issues detected, may need manual review
    /// </summary>
    Fair,

    /// <summary>
    /// Significant problems, output may be unusable
    /// </summary>
    Poor
}
