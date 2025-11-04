using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Content.DocumentParsers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class WordParserTests
{
    private readonly Mock<ILogger<WordParser>> _loggerMock;
    private readonly WordParser _parser;

    public WordParserTests()
    {
        _loggerMock = new Mock<ILogger<WordParser>>();
        _parser = new WordParser(_loggerMock.Object);
    }

    [Fact]
    public void SupportedFormat_ReturnsWord()
    {
        Assert.Equal(DocFormat.Word, _parser.SupportedFormat);
    }

    [Fact]
    public void SupportedExtensions_ContainsDocx()
    {
        Assert.Contains(".docx", _parser.SupportedExtensions);
    }

    [Theory]
    [InlineData("document.docx", true)]
    [InlineData("document.DOCX", true)]
    [InlineData("document.doc", false)]
    [InlineData("document.txt", false)]
    [InlineData("document.pdf", false)]
    public void CanParse_CorrectlyIdentifiesDocxFiles(string fileName, bool expected)
    {
        Assert.Equal(expected, _parser.CanParse(fileName));
    }

    [Fact]
    public async Task ParseAsync_ValidDocx_ExtractsTextAndMetadata()
    {
        var docxStream = CreateTestDocx("Test Document", "Test Author", "This is a test document with sample content.");

        var result = await _parser.ParseAsync(docxStream, "test.docx", CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("test.docx", result.Metadata.OriginalFileName);
        Assert.Equal(DocFormat.Word, result.Metadata.Format);
        Assert.True(result.Metadata.WordCount > 0);
        Assert.Contains("test", result.RawContent.ToLowerInvariant());
        Assert.NotNull(result.Structure);
        Assert.NotEmpty(result.Structure.Sections);
    }

    [Fact]
    public async Task ParseAsync_DocxWithHeadings_PreservesStructure()
    {
        var docxStream = CreateDocxWithHeadings();

        var result = await _parser.ParseAsync(docxStream, "structured.docx", CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Structure.Sections.Count >= 2);
        Assert.Contains("Introduction", result.Structure.Sections[0].Heading);
    }

    [Fact]
    public async Task ParseAsync_DocxWithTable_ExtractsTableContent()
    {
        var docxStream = CreateDocxWithTable();

        var result = await _parser.ParseAsync(docxStream, "table.docx", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("|", result.RawContent);
    }

    [Fact]
    public async Task ParseAsync_LegacyDocFormat_ReturnsError()
    {
        var docxStream = CreateTestDocx("Title", "Author", "Content");

        var result = await _parser.ParseAsync(docxStream, "legacy.doc", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("legacy", result.ErrorMessage.ToLowerInvariant());
    }

    [Fact]
    public async Task ParseAsync_WithCancellation_ReturnsError()
    {
        var docxStream = CreateTestDocx("Title", "Author", "Content");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _parser.ParseAsync(docxStream, "test.docx", cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancel", result.ErrorMessage.ToLowerInvariant());
    }

    private Stream CreateTestDocx(string title, string author, string content)
    {
        var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(content));

            wordDocument.PackageProperties.Title = title;
            wordDocument.PackageProperties.Creator = author;
            wordDocument.PackageProperties.Subject = "Test Subject";
            wordDocument.PackageProperties.Keywords = "test, keywords";
            wordDocument.PackageProperties.Created = DateTime.UtcNow;
        }
        stream.Position = 0;
        return stream;
    }

    private Stream CreateDocxWithHeadings()
    {
        var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            var heading1 = body.AppendChild(new Paragraph());
            heading1.ParagraphProperties = new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading1" });
            var run1 = heading1.AppendChild(new Run());
            run1.AppendChild(new Text("Introduction"));

            var para1 = body.AppendChild(new Paragraph());
            var run2 = para1.AppendChild(new Run());
            run2.AppendChild(new Text("This is the introduction content."));

            var heading2 = body.AppendChild(new Paragraph());
            heading2.ParagraphProperties = new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading1" });
            var run3 = heading2.AppendChild(new Run());
            run3.AppendChild(new Text("Conclusion"));

            var para2 = body.AppendChild(new Paragraph());
            var run4 = para2.AppendChild(new Run());
            run4.AppendChild(new Text("This is the conclusion content."));
        }
        stream.Position = 0;
        return stream;
    }

    private Stream CreateDocxWithTable()
    {
        var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            var table = new Table();
            
            var row1 = new TableRow();
            var cell1 = new TableCell();
            cell1.Append(new Paragraph(new Run(new Text("Header 1"))));
            var cell2 = new TableCell();
            cell2.Append(new Paragraph(new Run(new Text("Header 2"))));
            row1.Append(cell1, cell2);
            
            var row2 = new TableRow();
            var cell3 = new TableCell();
            cell3.Append(new Paragraph(new Run(new Text("Data 1"))));
            var cell4 = new TableCell();
            cell4.Append(new Paragraph(new Run(new Text("Data 2"))));
            row2.Append(cell3, cell4);
            
            table.Append(row1, row2);
            body.Append(table);
        }
        stream.Position = 0;
        return stream;
    }
}
