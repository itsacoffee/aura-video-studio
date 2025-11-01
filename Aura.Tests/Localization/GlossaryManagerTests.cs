using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Localization;

public class GlossaryManagerTests : IDisposable
{
    private readonly Mock<ILogger<GlossaryManager>> _mockLogger;
    private readonly string _testDirectory;
    private readonly GlossaryManager _manager;

    public GlossaryManagerTests()
    {
        _mockLogger = new Mock<ILogger<GlossaryManager>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"aura_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _manager = new GlossaryManager(_mockLogger.Object, _testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateGlossaryAsync_CreatesNewGlossary()
    {
        // Act
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary", "Test description");

        // Assert
        Assert.NotNull(glossary);
        Assert.NotEmpty(glossary.Id);
        Assert.Equal("Test Glossary", glossary.Name);
        Assert.Equal("Test description", glossary.Description);
        Assert.Empty(glossary.Entries);
    }

    [Fact]
    public async Task GetGlossaryAsync_ReturnsCreatedGlossary()
    {
        // Arrange
        var created = await _manager.CreateGlossaryAsync("Test Glossary");

        // Act
        var retrieved = await _manager.GetGlossaryAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(created.Name, retrieved.Name);
    }

    [Fact]
    public async Task GetGlossaryAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _manager.GetGlossaryAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListGlossariesAsync_ReturnsAllGlossaries()
    {
        // Arrange
        await _manager.CreateGlossaryAsync("Glossary 1");
        await _manager.CreateGlossaryAsync("Glossary 2");
        await _manager.CreateGlossaryAsync("Glossary 3");

        // Act
        var glossaries = await _manager.ListGlossariesAsync();

        // Assert
        Assert.NotNull(glossaries);
        Assert.Equal(3, glossaries.Count);
    }

    [Fact]
    public async Task AddEntryAsync_AddsTermToGlossary()
    {
        // Arrange
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary");
        var translations = new Dictionary<string, string>
        {
            ["es"] = "hola",
            ["fr"] = "bonjour",
            ["de"] = "hallo"
        };

        // Act
        var entry = await _manager.AddEntryAsync(
            glossary.Id,
            "hello",
            translations,
            "Greeting",
            "General");

        // Assert
        Assert.NotNull(entry);
        Assert.Equal("hello", entry.Term);
        Assert.Equal(3, entry.Translations.Count);
        Assert.Equal("hola", entry.Translations["es"]);
    }

    [Fact]
    public async Task AddEntryAsync_InvalidGlossaryId_ThrowsException()
    {
        // Arrange
        var translations = new Dictionary<string, string> { ["es"] = "hola" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.AddEntryAsync("invalid", "hello", translations));
    }

    [Fact]
    public async Task DeleteEntryAsync_RemovesEntry()
    {
        // Arrange
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary");
        var translations = new Dictionary<string, string> { ["es"] = "hola" };
        var entry = await _manager.AddEntryAsync(glossary.Id, "hello", translations);

        // Act
        await _manager.DeleteEntryAsync(glossary.Id, entry.Id);

        // Assert
        var retrieved = await _manager.GetGlossaryAsync(glossary.Id);
        Assert.NotNull(retrieved);
        Assert.Empty(retrieved.Entries);
    }

    [Fact]
    public async Task DeleteGlossaryAsync_RemovesGlossary()
    {
        // Arrange
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary");

        // Act
        await _manager.DeleteGlossaryAsync(glossary.Id);

        // Assert
        var retrieved = await _manager.GetGlossaryAsync(glossary.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task BuildTranslationDictionaryAsync_CreatesCorrectDictionary()
    {
        // Arrange
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary");
        
        await _manager.AddEntryAsync(glossary.Id, "hello", 
            new Dictionary<string, string> { ["es"] = "hola", ["fr"] = "bonjour" });
        await _manager.AddEntryAsync(glossary.Id, "goodbye",
            new Dictionary<string, string> { ["es"] = "adiós", ["fr"] = "au revoir" });

        // Act
        var dictionary = await _manager.BuildTranslationDictionaryAsync(glossary.Id, "es");

        // Assert
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("hola", dictionary["hello"]);
        Assert.Equal("adiós", dictionary["goodbye"]);
    }

    [Fact]
    public async Task BuildTranslationDictionaryAsync_MissingLanguage_ReturnsPartialDictionary()
    {
        // Arrange
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary");
        
        await _manager.AddEntryAsync(glossary.Id, "hello",
            new Dictionary<string, string> { ["es"] = "hola" });
        await _manager.AddEntryAsync(glossary.Id, "goodbye",
            new Dictionary<string, string> { ["fr"] = "au revoir" });

        // Act
        var dictionary = await _manager.BuildTranslationDictionaryAsync(glossary.Id, "es");

        // Assert
        Assert.Single(dictionary);
        Assert.Equal("hola", dictionary["hello"]);
    }

    [Fact]
    public async Task ImportFromCsvAsync_ImportsGlossary()
    {
        // Arrange
        var csv = @"Term,es,fr,de
hello,hola,bonjour,hallo
goodbye,adiós,au revoir,auf wiedersehen";

        // Act
        var glossary = await _manager.ImportFromCsvAsync("Imported Glossary", csv);

        // Assert
        Assert.NotNull(glossary);
        Assert.Equal("Imported Glossary", glossary.Name);
        Assert.Equal(2, glossary.Entries.Count);
        
        var helloEntry = glossary.Entries.Find(e => e.Term == "hello");
        Assert.NotNull(helloEntry);
        Assert.Equal("hola", helloEntry.Translations["es"]);
        Assert.Equal("bonjour", helloEntry.Translations["fr"]);
        Assert.Equal("hallo", helloEntry.Translations["de"]);
    }

    [Fact]
    public async Task ExportToCsvAsync_ExportsGlossary()
    {
        // Arrange
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary");
        await _manager.AddEntryAsync(glossary.Id, "hello",
            new Dictionary<string, string> { ["es"] = "hola", ["fr"] = "bonjour" });

        // Act
        var csv = await _manager.ExportToCsvAsync(glossary.Id);

        // Assert
        Assert.NotNull(csv);
        Assert.Contains("Term", csv);
        Assert.Contains("hello", csv);
        Assert.Contains("hola", csv);
        Assert.Contains("bonjour", csv);
    }

    [Fact]
    public async Task UpdateEntryAsync_UpdatesTranslations()
    {
        // Arrange
        var glossary = await _manager.CreateGlossaryAsync("Test Glossary");
        var translations = new Dictionary<string, string> { ["es"] = "hola" };
        var entry = await _manager.AddEntryAsync(glossary.Id, "hello", translations);

        var updatedTranslations = new Dictionary<string, string>
        {
            ["es"] = "hola",
            ["fr"] = "bonjour",
            ["de"] = "hallo"
        };

        // Act
        await _manager.UpdateEntryAsync(glossary.Id, entry.Id, updatedTranslations, "Updated context");

        // Assert
        var retrieved = await _manager.GetGlossaryAsync(glossary.Id);
        Assert.NotNull(retrieved);
        var updatedEntry = retrieved.Entries.Find(e => e.Id == entry.Id);
        Assert.NotNull(updatedEntry);
        Assert.Equal(3, updatedEntry.Translations.Count);
        Assert.Equal("Updated context", updatedEntry.Context);
    }
}
