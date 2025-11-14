using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Aura.Core.Models.PromptManagement;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Resolves variables in prompt templates with type safety and transformations
/// </summary>
public class PromptVariableResolver
{
    private readonly ILogger<PromptVariableResolver> _logger;
    private static readonly Regex VariablePattern = new(@"\{\{(\w+)(?:\s*\|\s*(\w+)(?::(.+?))?)?\}\}", RegexOptions.Compiled);

    public PromptVariableResolver(ILogger<PromptVariableResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolve all variables in a prompt template
    /// </summary>
    public async Task<string> ResolveAsync(
        string template,
        List<PromptVariable> variableDefinitions,
        Dictionary<string, object> values,
        VariableResolverOptions options,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        _logger.LogDebug("Resolving variables in template with {Count} variables", values.Count);

        ValidateRequiredVariables(variableDefinitions, values, options);

        var result = VariablePattern.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            var transformation = match.Groups[2].Success ? match.Groups[2].Value : null;
            var transformParam = match.Groups[3].Success ? match.Groups[3].Value : null;

            if (!values.TryGetValue(variableName, out var value))
            {
                var varDef = variableDefinitions.FirstOrDefault(v => v.Name == variableName);
                if (varDef?.DefaultValue != null)
                {
                    value = varDef.DefaultValue;
                }
                else
                {
                    _logger.LogWarning("Variable {Name} not found and has no default", variableName);
                    return match.Value;
                }
            }

            var varDefinition = variableDefinitions.FirstOrDefault(v => v.Name == variableName);
            if (varDefinition != null)
            {
                ValidateVariableType(variableName, value, varDefinition, options);
                ValidateVariableConstraints(variableName, value, varDefinition);
            }

            var stringValue = ConvertToString(value, varDefinition?.Type ?? VariableType.String);

            if (options.SanitizeValues)
            {
                stringValue = SanitizeValue(stringValue, options);
            }

            if (!string.IsNullOrEmpty(transformation))
            {
                stringValue = ApplyTransformation(stringValue, transformation, transformParam);
            }

            return stringValue;
        });

        return result;
    }

    /// <summary>
    /// Extract variable names from a template
    /// </summary>
    public List<string> ExtractVariableNames(string template)
    {
        var matches = VariablePattern.Matches(template);
        return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
    }

    /// <summary>
    /// Validate that all required variables are provided
    /// </summary>
    private void ValidateRequiredVariables(
        List<PromptVariable> definitions,
        Dictionary<string, object> values,
        VariableResolverOptions options)
    {
        var missingRequired = definitions
            .Where(v => v.Required && !values.ContainsKey(v.Name) && v.DefaultValue == null)
            .Select(v => v.Name)
            .ToList();

        if (missingRequired.Count != 0)
        {
            var message = $"Missing required variables: {string.Join(", ", missingRequired)}";
            _logger.LogError(message);

            if (options.ThrowOnMissingRequired)
                throw new ArgumentException(message);
        }
    }

    /// <summary>
    /// Validate variable type matches definition
    /// </summary>
    private void ValidateVariableType(
        string name,
        object value,
        PromptVariable definition,
        VariableResolverOptions options)
    {
        var isValid = definition.Type switch
        {
            VariableType.String => value is string,
            VariableType.Numeric => value is int or long or float or double or decimal,
            VariableType.Boolean => value is bool,
            VariableType.Array => value is System.Collections.IEnumerable && value is not string,
            VariableType.Object => value != null,
            _ => true
        };

        if (!isValid)
        {
            var message = $"Variable {name} type mismatch. Expected {definition.Type}, got {value?.GetType().Name ?? "null"}";
            _logger.LogWarning(message);

            if (options.ThrowOnInvalidType)
                throw new ArgumentException(message);
        }
    }

    /// <summary>
    /// Validate variable constraints (length, format, allowed values)
    /// </summary>
    private void ValidateVariableConstraints(string name, object value, PromptVariable definition)
    {
        if (value is string strValue)
        {
            if (definition.MinLength.HasValue && strValue.Length < definition.MinLength.Value)
            {
                _logger.LogWarning("Variable {Name} is shorter than minimum length {Min}", 
                    name, definition.MinLength);
            }

            if (definition.MaxLength.HasValue && strValue.Length > definition.MaxLength.Value)
            {
                _logger.LogWarning("Variable {Name} exceeds maximum length {Max}", 
                    name, definition.MaxLength);
            }

            if (!string.IsNullOrEmpty(definition.FormatPattern))
            {
                if (!Regex.IsMatch(strValue, definition.FormatPattern))
                {
                    _logger.LogWarning("Variable {Name} doesn't match format pattern {Pattern}",
                        name, definition.FormatPattern);
                }
            }

            if (definition.AllowedValues?.Any() == true && !definition.AllowedValues.Contains(strValue))
            {
                _logger.LogWarning("Variable {Name} value '{Value}' is not in allowed values",
                    name, strValue);
            }
        }
    }

    /// <summary>
    /// Convert value to string based on type
    /// </summary>
    private string ConvertToString(object value, VariableType type)
    {
        return type switch
        {
            VariableType.Array when value is System.Collections.IEnumerable enumerable =>
                string.Join(", ", enumerable.Cast<object>().Select(o => o?.ToString() ?? string.Empty)),
            VariableType.Object when value != null =>
                System.Text.Json.JsonSerializer.Serialize(value),
            _ => value?.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Sanitize variable value for safety
    /// </summary>
    private string SanitizeValue(string value, VariableResolverOptions options)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length > options.MaxStringLength)
        {
            value = value.Substring(0, options.MaxStringLength);
            _logger.LogWarning("Variable value truncated to {Length} characters", options.MaxStringLength);
        }

        if (!options.AllowHtml)
        {
            value = HttpUtility.HtmlEncode(value);
        }

        value = value
            .Replace("\0", string.Empty)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        return value;
    }

    /// <summary>
    /// Apply transformation function to value
    /// </summary>
    private string ApplyTransformation(string value, string transformation, string? parameter)
    {
        try
        {
            return transformation.ToLowerInvariant() switch
            {
                "uppercase" => value.ToUpperInvariant(),
                "lowercase" => value.ToLowerInvariant(),
                "capitalize" => CapitalizeWords(value),
                "truncate" when int.TryParse(parameter, out var maxLen) =>
                    value.Length <= maxLen ? value : string.Concat(value.AsSpan(0, maxLen), "..."),
                "join" when value.Contains(',') =>
                    string.Join(parameter ?? ", ", value.Split(',').Select(s => s.Trim())),
                "format" when !string.IsNullOrEmpty(parameter) =>
                    string.Format(parameter.Replace("'", string.Empty), value),
                "escape" => HttpUtility.HtmlEncode(value),
                "striphtml" => StripHtml(value),
                _ => value
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply transformation {Transform} to value", transformation);
            return value;
        }
    }

    /// <summary>
    /// Capitalize first letter of each word
    /// </summary>
    private string CapitalizeWords(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var words = value.Split(' ');
        var sb = new StringBuilder();

        foreach (var word in words)
        {
            if (sb.Length > 0)
                sb.Append(' ');

            if (word.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                    sb.Append(word.Substring(1).ToLowerInvariant());
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Strip HTML tags from value
    /// </summary>
    private string StripHtml(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return Regex.Replace(value, @"<[^>]*>", string.Empty);
    }
}
