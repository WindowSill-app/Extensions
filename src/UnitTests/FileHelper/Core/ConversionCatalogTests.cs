using FluentAssertions;

using WindowSill.FileHelper.Core;

namespace UnitTests.FileHelper.Core;

/// <summary>
/// Tests for <see cref="ConversionCatalog"/>: extension-to-format resolution (including alias extensions), the
/// source-to-targets matrix, and the converter factory's guard rails.
/// </summary>
/// <remarks>
/// These cover the catalog's decision-making only. The real Syncfusion rendering behind each pair is verified
/// end-to-end on the test VM rather than in this unit-test host.
/// </remarks>
public class ConversionCatalogTests
{
    [Theory]
    [InlineData(".docx", DocumentFileFormat.Docx)]
    [InlineData(".doc", DocumentFileFormat.Doc)]
    [InlineData(".rtf", DocumentFileFormat.Rtf)]
    [InlineData(".html", DocumentFileFormat.Html)]
    [InlineData(".htm", DocumentFileFormat.Html)]
    [InlineData(".md", DocumentFileFormat.Markdown)]
    [InlineData(".markdown", DocumentFileFormat.Markdown)]
    [InlineData(".txt", DocumentFileFormat.Txt)]
    [InlineData(".pdf", DocumentFileFormat.Pdf)]
    internal void TryGetFormat_ResolvesEverySupportedExtension(string extension, DocumentFileFormat expected)
    {
        ConversionCatalog.TryGetFormat(extension, out DocumentFileFormat format).Should().BeTrue();
        format.Should().Be(expected);
    }

    [Theory]
    [InlineData(".DOCX")]
    [InlineData(".Md")]
    [InlineData(".HTM")]
    internal void TryGetFormat_IsCaseInsensitive(string extension)
    {
        ConversionCatalog.TryGetFormat(extension, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(".zip")]
    [InlineData(".png")]
    [InlineData("")]
    [InlineData("docx")]
    internal void TryGetFormat_RejectsUnsupportedExtensions(string extension)
    {
        ConversionCatalog.TryGetFormat(extension, out _).Should().BeFalse();
    }

    [Fact]
    internal void GetTargets_ForPdf_IsEmptyBecausePdfCannotBeReadBack()
    {
        ConversionCatalog.GetTargets(DocumentFileFormat.Pdf).Should().BeEmpty();
    }

    [Theory]
    [InlineData(DocumentFileFormat.Docx)]
    [InlineData(DocumentFileFormat.Doc)]
    [InlineData(DocumentFileFormat.Rtf)]
    [InlineData(DocumentFileFormat.Html)]
    [InlineData(DocumentFileFormat.Markdown)]
    [InlineData(DocumentFileFormat.Txt)]
    internal void GetTargets_NeverOffersTheSourceFormatAsItsOwnTarget(DocumentFileFormat source)
    {
        ConversionCatalog.GetTargets(source).Should().NotContain(info => info.Format == source);
    }

    [Theory]
    [InlineData(DocumentFileFormat.Docx)]
    [InlineData(DocumentFileFormat.Markdown)]
    [InlineData(DocumentFileFormat.Html)]
    [InlineData(DocumentFileFormat.Txt)]
    internal void GetTargets_LeadsWithPdf_SoThePopupCanPromoteIt(DocumentFileFormat source)
    {
        ConversionCatalog.GetTargets(source)[0].Format.Should().Be(DocumentFileFormat.Pdf);
    }

    [Fact]
    internal void GetTargets_ForDocx_MatchesTheWritableFormatsMinusDocx()
    {
        ConversionCatalog.GetTargets(DocumentFileFormat.Docx)
            .Select(info => info.Format)
            .Should()
            .Equal(
                DocumentFileFormat.Pdf,
                DocumentFileFormat.Markdown,
                DocumentFileFormat.Html,
                DocumentFileFormat.Rtf,
                DocumentFileFormat.Txt);
    }

    [Fact]
    internal void GetTargets_ForLegacyDoc_IncludesDocxSoItCanBeUpgraded()
    {
        ConversionCatalog.GetTargets(DocumentFileFormat.Doc)
            .Select(info => info.Format)
            .Should()
            .Contain(DocumentFileFormat.Docx);
    }

    [Fact]
    internal void GetTargets_NeverOffersLegacyDocAsATarget()
    {
        foreach (DocumentFileFormat source in Enum.GetValues<DocumentFileFormat>())
        {
            ConversionCatalog.GetTargets(source).Should().NotContain(info => info.Format == DocumentFileFormat.Doc);
        }
    }

    [Theory]
    [InlineData(DocumentFileFormat.Txt, DocumentFileFormat.Pdf, ".pdf")]
    [InlineData(DocumentFileFormat.Markdown, DocumentFileFormat.Docx, ".docx")]
    [InlineData(DocumentFileFormat.Html, DocumentFileFormat.Markdown, ".md")]
    [InlineData(DocumentFileFormat.Docx, DocumentFileFormat.Html, ".html")]
    [InlineData(DocumentFileFormat.Rtf, DocumentFileFormat.Txt, ".txt")]
    internal void CreateConverter_BindsTheTargetsOutputExtension(DocumentFileFormat source, DocumentFileFormat target, string expectedExtension)
    {
        ConversionCatalog.CreateConverter(source, target).OutputExtension.Should().Be(expectedExtension);
    }

    [Fact]
    internal void CreateConverter_CanBuildEveryPairInTheAdvertisedMatrix()
    {
        foreach (DocumentFileFormat source in Enum.GetValues<DocumentFileFormat>())
        {
            foreach (DocumentFileFormatInfo target in ConversionCatalog.GetTargets(source))
            {
                ConversionCatalog.CreateConverter(source, target.Format).Should().NotBeNull();
            }
        }
    }

    [Fact]
    internal void CreateConverter_RejectsConvertingAFormatToItself()
    {
        Action act = () => ConversionCatalog.CreateConverter(DocumentFileFormat.Html, DocumentFileFormat.Html);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    internal void CreateConverter_RejectsPdfAsASource()
    {
        Action act = () => ConversionCatalog.CreateConverter(DocumentFileFormat.Pdf, DocumentFileFormat.Docx);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    internal void CreateConverter_RejectsLegacyDocAsATarget()
    {
        Action act = () => ConversionCatalog.CreateConverter(DocumentFileFormat.Docx, DocumentFileFormat.Doc);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    internal void GetInfo_ExposesTheExtensionUsedWhenWritingEachFormat()
    {
        ConversionCatalog.GetInfo(DocumentFileFormat.Markdown).Extension.Should().Be(".md");
        ConversionCatalog.GetInfo(DocumentFileFormat.Html).Extension.Should().Be(".html");
        ConversionCatalog.GetInfo(DocumentFileFormat.Pdf).Extension.Should().Be(".pdf");
    }

    [Fact]
    internal void GetInfo_ListsThePrimaryExtensionAmongTheAcceptedInputExtensions()
    {
        foreach (DocumentFileFormat format in Enum.GetValues<DocumentFileFormat>())
        {
            DocumentFileFormatInfo info = ConversionCatalog.GetInfo(format);
            info.InputExtensions.Should().Contain(info.Extension);
        }
    }

    [Theory]
    [InlineData(".csv", DocumentFileFormat.Csv)]
    [InlineData(".tsv", DocumentFileFormat.Tsv)]
    [InlineData(".tab", DocumentFileFormat.Tsv)]
    [InlineData(".xlsx", DocumentFileFormat.Xlsx)]
    [InlineData(".xls", DocumentFileFormat.Xlsx)]
    [InlineData(".pptx", DocumentFileFormat.Pptx)]
    [InlineData(".ppt", DocumentFileFormat.Pptx)]
    internal void TryGetFormat_ResolvesTheOfficeExtensions(string extension, DocumentFileFormat expected)
    {
        ConversionCatalog.TryGetFormat(extension, out DocumentFileFormat format).Should().BeTrue();
        format.Should().Be(expected);
    }

    [Fact]
    internal void GetTargets_ForASpreadsheet_StaysWithinTheSpreadsheetFamilyPlusPdf()
    {
        ConversionCatalog.GetTargets(DocumentFileFormat.Xlsx)
            .Select(info => info.Format)
            .Should()
            .Equal(DocumentFileFormat.Pdf, DocumentFileFormat.Csv, DocumentFileFormat.Tsv);
    }

    [Fact]
    internal void GetTargets_ForCsv_OffersTheOtherDelimiterAndAWorkbook()
    {
        ConversionCatalog.GetTargets(DocumentFileFormat.Csv)
            .Select(info => info.Format)
            .Should()
            .Equal(DocumentFileFormat.Pdf, DocumentFileFormat.Xlsx, DocumentFileFormat.Tsv);
    }

    [Fact]
    internal void GetTargets_ForAPresentation_OnlyOffersPdf()
    {
        ConversionCatalog.GetTargets(DocumentFileFormat.Pptx)
            .Select(info => info.Format)
            .Should()
            .Equal(DocumentFileFormat.Pdf);
    }

    [Fact]
    internal void GetTargets_NeverCrossesFamilies_ExceptToPdf()
    {
        foreach (DocumentFileFormat source in Enum.GetValues<DocumentFileFormat>())
        {
            DocumentFamily sourceFamily = ConversionCatalog.GetInfo(source).Family;

            foreach (DocumentFileFormatInfo target in ConversionCatalog.GetTargets(source))
            {
                // A spreadsheet is not a Markdown document; the only shared target across families is PDF.
                (target.Family == sourceFamily || target.Format == DocumentFileFormat.Pdf)
                    .Should()
                    .BeTrue($"'{source}' should not offer '{target.Format}'");
            }
        }
    }

    [Fact]
    internal void CreateConverter_RejectsCrossFamilyPairs()
    {
        Action act = () => ConversionCatalog.CreateConverter(DocumentFileFormat.Xlsx, DocumentFileFormat.Markdown);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(DocumentFileFormat.Csv, ",")]
    [InlineData(DocumentFileFormat.Tsv, "\t")]
    [InlineData(DocumentFileFormat.Xlsx, null)]
    [InlineData(DocumentFileFormat.Docx, null)]
    internal void GetDelimiter_OnlyCharacterSeparatedFormatsHaveOne(DocumentFileFormat format, string? expected)
    {
        ConversionCatalog.GetDelimiter(format).Should().Be(expected);
    }
}
