using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Content.DocumentParsers;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class PdfParserTests
{
    private readonly Mock<ILogger<PdfParser>> _loggerMock;
    private readonly PdfParser _parser;

    public PdfParserTests()
    {
        _loggerMock = new Mock<ILogger<PdfParser>>();
        _parser = new PdfParser(_loggerMock.Object);
    }

    [Fact]
    public void SupportedFormat_ReturnsPdf()
    {
        Assert.Equal(DocFormat.Pdf, _parser.SupportedFormat);
    }

    [Fact]
    public void SupportedExtensions_ContainsPdf()
    {
        Assert.Contains(".pdf", _parser.SupportedExtensions);
    }

    [Theory]
    [InlineData("document.pdf", true)]
    [InlineData("document.PDF", true)]
    [InlineData("document.txt", false)]
    [InlineData("document.docx", false)]
    public void CanParse_CorrectlyIdentifiesPdfFiles(string fileName, bool expected)
    {
        Assert.Equal(expected, _parser.CanParse(fileName));
    }

    [Fact]
    public async Task ParseAsync_ValidPdf_ExtractsTextAndMetadata()
    {
        var pdfStream = CreateTestPdf("Test PDF Title", "Test Author", "This is a test PDF document with some sample text.");

        var result = await _parser.ParseAsync(pdfStream, "test.pdf", CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("test.pdf", result.Metadata.OriginalFileName);
        Assert.Equal(DocFormat.Pdf, result.Metadata.Format);
        Assert.True(result.Metadata.WordCount > 0);
        Assert.Contains("test", result.RawContent.ToLowerInvariant());
        Assert.NotNull(result.Structure);
        Assert.NotEmpty(result.Structure.Sections);
    }

    [Fact]
    public async Task ParseAsync_PdfWithMultiplePages_ExtractsAllContent()
    {
        var pdfStream = CreateMultiPageTestPdf();

        var result = await _parser.ParseAsync(pdfStream, "multipage.pdf", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("page one", result.RawContent.ToLowerInvariant());
        Assert.Contains("page two", result.RawContent.ToLowerInvariant());
    }

    [Fact]
    public async Task ParseAsync_EmptyPdf_ReturnsSuccessWithWarning()
    {
        var pdfStream = CreateEmptyPdf();

        var result = await _parser.ParseAsync(pdfStream, "empty.pdf", CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains("no text", result.Warnings[0].ToLowerInvariant());
    }

    [Fact]
    public async Task ParseAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var pdfStream = CreateTestPdf("Title", "Author", "Content");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _parser.ParseAsync(pdfStream, "test.pdf", cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancel", result.ErrorMessage.ToLowerInvariant());
    }

    private Stream CreateTestPdf(string title, string author, string content)
    {
        var stream = new MemoryStream();
        var writer = new PdfWriter(stream);
        writer.SetCloseStream(false);
        
        using (writer)
        {
            using var pdfDoc = new PdfDocument(writer);
            var docInfo = pdfDoc.GetDocumentInfo();
            docInfo.SetTitle(title);
            docInfo.SetAuthor(author);
            docInfo.SetSubject("Test Subject");
            docInfo.SetKeywords("test, keywords");

            using var document = new Document(pdfDoc);
            document.Add(new Paragraph(content));
        }
        stream.Position = 0;
        return stream;
    }

    private Stream CreateMultiPageTestPdf()
    {
        var stream = new MemoryStream();
        var writer = new PdfWriter(stream);
        writer.SetCloseStream(false);
        
        using (writer)
        {
            using var pdfDoc = new PdfDocument(writer);
            using var document = new Document(pdfDoc);
            
            document.Add(new Paragraph("This is page one content."));
            document.Add(new AreaBreak(iText.Layout.Properties.AreaBreakType.NEXT_PAGE));
            document.Add(new Paragraph("This is page two content."));
        }
        stream.Position = 0;
        return stream;
    }

    private Stream CreateEmptyPdf()
    {
        var stream = new MemoryStream();
        var writer = new PdfWriter(stream);
        writer.SetCloseStream(false);
        
        using (writer)
        {
            using var pdfDoc = new PdfDocument(writer);
            pdfDoc.AddNewPage();
        }
        stream.Position = 0;
        return stream;
    }
}
