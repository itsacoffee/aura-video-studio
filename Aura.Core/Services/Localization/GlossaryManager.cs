using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Localization;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Manages terminology glossaries for consistent translation
/// Supports project-specific and industry-specific terminology databases
/// </summary>
public class GlossaryManager
{
    private readonly ILogger<GlossaryManager> _logger;
    private readonly string _storageDirectory;
    private readonly Dictionary<string, ProjectGlossary> _glossaryCache = new();

    public GlossaryManager(ILogger<GlossaryManager> logger, string storageDirectory)
    {
        _logger = logger;
        _storageDirectory = storageDirectory;
        
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    /// <summary>
    /// Create a new glossary
    /// </summary>
    public async Task<ProjectGlossary> CreateGlossaryAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating glossary: {Name}", name);

        var glossary = new ProjectGlossary
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description
        };

        await SaveGlossaryAsync(glossary, cancellationToken);
        _glossaryCache[glossary.Id] = glossary;

        return glossary;
    }

    /// <summary>
    /// Get glossary by ID
    /// </summary>
    public async Task<ProjectGlossary?> GetGlossaryAsync(
        string glossaryId,
        CancellationToken cancellationToken = default)
    {
        if (_glossaryCache.TryGetValue(glossaryId, out var cached))
        {
            return cached;
        }

        var filePath = Path.Combine(_storageDirectory, $"{glossaryId}.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var glossary = JsonSerializer.Deserialize<ProjectGlossary>(json);
        
        if (glossary != null)
        {
            _glossaryCache[glossaryId] = glossary;
        }

        return glossary;
    }

    /// <summary>
    /// List all glossaries
    /// </summary>
    public async Task<List<ProjectGlossary>> ListGlossariesAsync(
        CancellationToken cancellationToken = default)
    {
        var glossaries = new List<ProjectGlossary>();
        var files = Directory.GetFiles(_storageDirectory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var glossary = JsonSerializer.Deserialize<ProjectGlossary>(json);
                
                if (glossary != null)
                {
                    glossaries.Add(glossary);
                    _glossaryCache[glossary.Id] = glossary;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load glossary from {File}", file);
            }
        }

        return glossaries.OrderBy(g => g.Name).ToList();
    }

    /// <summary>
    /// Add entry to glossary
    /// </summary>
    public async Task<GlossaryEntry> AddEntryAsync(
        string glossaryId,
        string term,
        Dictionary<string, string> translations,
        string? context = null,
        string? industry = null,
        CancellationToken cancellationToken = default)
    {
        var glossary = await GetGlossaryAsync(glossaryId, cancellationToken);
        if (glossary == null)
        {
            throw new ArgumentException($"Glossary {glossaryId} not found");
        }

        var entry = new GlossaryEntry
        {
            Term = term,
            Translations = translations,
            Context = context,
            Industry = industry
        };

        glossary.Entries.Add(entry);
        glossary.UpdatedAt = DateTime.UtcNow;

        await SaveGlossaryAsync(glossary, cancellationToken);

        _logger.LogInformation("Added term '{Term}' to glossary {Name}", term, glossary.Name);
        return entry;
    }

    /// <summary>
    /// Update glossary entry
    /// </summary>
    public async Task UpdateEntryAsync(
        string glossaryId,
        string entryId,
        Dictionary<string, string> translations,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        var glossary = await GetGlossaryAsync(glossaryId, cancellationToken);
        if (glossary == null)
        {
            throw new ArgumentException($"Glossary {glossaryId} not found");
        }

        var entry = glossary.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry == null)
        {
            throw new ArgumentException($"Entry {entryId} not found");
        }

        entry.Translations = translations;
        if (context != null)
        {
            entry.Context = context;
        }
        
        glossary.UpdatedAt = DateTime.UtcNow;
        await SaveGlossaryAsync(glossary, cancellationToken);

        _logger.LogInformation("Updated entry {EntryId} in glossary {Name}", entryId, glossary.Name);
    }

    /// <summary>
    /// Delete glossary entry
    /// </summary>
    public async Task DeleteEntryAsync(
        string glossaryId,
        string entryId,
        CancellationToken cancellationToken = default)
    {
        var glossary = await GetGlossaryAsync(glossaryId, cancellationToken);
        if (glossary == null)
        {
            throw new ArgumentException($"Glossary {glossaryId} not found");
        }

        var entry = glossary.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
        {
            glossary.Entries.Remove(entry);
            glossary.UpdatedAt = DateTime.UtcNow;
            await SaveGlossaryAsync(glossary, cancellationToken);

            _logger.LogInformation("Deleted entry {EntryId} from glossary {Name}", entryId, glossary.Name);
        }
    }

    /// <summary>
    /// Delete glossary
    /// </summary>
    public async Task DeleteGlossaryAsync(
        string glossaryId,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storageDirectory, $"{glossaryId}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _glossaryCache.Remove(glossaryId);
            
            _logger.LogInformation("Deleted glossary {GlossaryId}", glossaryId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Build translation dictionary from glossary
    /// </summary>
    public async Task<Dictionary<string, string>> BuildTranslationDictionaryAsync(
        string glossaryId,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var glossary = await GetGlossaryAsync(glossaryId, cancellationToken);
        if (glossary == null)
        {
            return new Dictionary<string, string>();
        }

        var dictionary = new Dictionary<string, string>();

        foreach (var entry in glossary.Entries)
        {
            if (entry.Translations.TryGetValue(targetLanguage, out var translation))
            {
                dictionary[entry.Term] = translation;
            }
        }

        _logger.LogInformation("Built dictionary with {Count} terms for {Language}", 
            dictionary.Count, targetLanguage);

        return dictionary;
    }

    /// <summary>
    /// Import glossary from CSV
    /// </summary>
    public async Task<ProjectGlossary> ImportFromCsvAsync(
        string name,
        string csvContent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing glossary from CSV: {Name}", name);

        var glossary = await CreateGlossaryAsync(name, "Imported from CSV", cancellationToken);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            throw new ArgumentException("CSV must contain at least a header row and one data row");
        }

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        var termIndex = Array.IndexOf(headers, "Term");
        
        if (termIndex < 0)
        {
            throw new ArgumentException("CSV must contain a 'Term' column");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',').Select(v => v.Trim()).ToArray();
            
            if (values.Length != headers.Length)
            {
                continue;
            }

            var term = values[termIndex];
            var translations = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length; j++)
            {
                if (j != termIndex && !string.IsNullOrEmpty(values[j]))
                {
                    translations[headers[j]] = values[j];
                }
            }

            if (translations.Any())
            {
                await AddEntryAsync(glossary.Id, term, translations, cancellationToken: cancellationToken);
            }
        }

        _logger.LogInformation("Imported {Count} entries", glossary.Entries.Count);
        return glossary;
    }

    /// <summary>
    /// Export glossary to CSV
    /// </summary>
    public async Task<string> ExportToCsvAsync(
        string glossaryId,
        CancellationToken cancellationToken = default)
    {
        var glossary = await GetGlossaryAsync(glossaryId, cancellationToken);
        if (glossary == null)
        {
            throw new ArgumentException($"Glossary {glossaryId} not found");
        }

        var allLanguages = glossary.Entries
            .SelectMany(e => e.Translations.Keys)
            .Distinct()
            .OrderBy(l => l)
            .ToList();

        var csv = new System.Text.StringBuilder();
        csv.Append("Term");
        foreach (var lang in allLanguages)
        {
            csv.Append($",{lang}");
        }
        csv.AppendLine();

        foreach (var entry in glossary.Entries)
        {
            csv.Append(entry.Term);
            foreach (var lang in allLanguages)
            {
                var translation = entry.Translations.TryGetValue(lang, out var trans) ? trans : "";
                csv.Append($",{translation}");
            }
            csv.AppendLine();
        }

        return csv.ToString();
    }

    private async Task SaveGlossaryAsync(
        ProjectGlossary glossary,
        CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_storageDirectory, $"{glossary.Id}.json");
        var json = JsonSerializer.Serialize(glossary, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _glossaryCache[glossary.Id] = glossary;
    }
}
