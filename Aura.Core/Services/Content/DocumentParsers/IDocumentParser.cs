using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;

namespace Aura.Core.Services.Content.DocumentParsers;

/// <summary>
/// Interface for document format parsers
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Gets the document format supported by this parser
    /// </summary>
    DocFormat SupportedFormat { get; }
    
    /// <summary>
    /// Gets the file extensions supported by this parser (e.g., ".txt", ".md")
    /// </summary>
    string[] SupportedExtensions { get; }
    
    /// <summary>
    /// Determines if this parser can handle the given file
    /// </summary>
    bool CanParse(string fileName);
    
    /// <summary>
    /// Parses a document from a stream and extracts structure and content
    /// </summary>
    Task<DocumentImportResult> ParseAsync(Stream stream, string fileName, CancellationToken ct = default);
}
