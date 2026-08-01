using FluentAssertions;

using UnitTests.Fakes;

using WindowSill.FileHelper.Core;

namespace UnitTests.FileHelper.Core;

/// <summary>
/// Tests for <see cref="PdfActionCatalog"/>: which actions a selection offers, and how many operations each action
/// expands into.
/// </summary>
/// <remarks>
/// These cover the catalog's decision-making only. The Syncfusion work behind each action (merging, splitting,
/// compressing) is verified end-to-end on the test VM rather than in this unit-test host.
/// </remarks>
public class PdfActionCatalogTests
{
    public PdfActionCatalogTests()
    {
        // Action labels, the compressed-file suffix and progress text all resolve through GetLocalizedString().
        LocalizerSetup.EnsureInitialized();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    internal void GetActions_IsEmpty_WhenNothingIsSelected(int fileCount)
    {
        PdfActionCatalog.GetActions(fileCount).Should().BeEmpty();
    }

    [Fact]
    internal void GetActions_ForASingleFile_OffersEverySingleDocumentAction()
    {
        PdfActionCatalog.GetActions(1)
            .Select(info => info.Action)
            .Should()
            .Equal(
                PdfAction.Extract,
                PdfAction.Split,
                PdfAction.SaveAsImages,
                PdfAction.Compress,
                PdfAction.Protect,
                PdfAction.Unlock);
    }

    [Fact]
    internal void CreatePasswordOperations_ProducesASingleOperation()
    {
        PdfActionCatalog.CreatePasswordOperations(@"C:\docs\report.pdf", "hunter2", protect: true)
            .Should()
            .ContainSingle();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    internal void GetPasswordProgressText_NamesTheFile(bool protect)
    {
        PdfActionCatalog.GetPasswordProgressText(@"C:\docs\report.pdf", protect)
            .Should()
            .Contain("report.pdf");
    }

    [Fact]
    internal void GetActions_MarksThePagePickerOrderAndPasswordActionsAsNeedingConfiguration()
    {
        // Merge needs an order and Extract needs a page selection; the rest start immediately.
        PdfActionCatalog.GetActions(1)
            .Where(info => info.RequiresConfiguration)
            .Select(info => info.Action)
            .Should()
            .Equal(PdfAction.Extract, PdfAction.Protect, PdfAction.Unlock);

        PdfActionCatalog.GetActions(3)
            .Where(info => info.RequiresConfiguration)
            .Select(info => info.Action)
            .Should()
            .Equal(PdfAction.Merge);
    }

    [Fact]
    internal void GetActions_NeverOffersExtract_ForMultipleFiles()
    {
        PdfActionCatalog.GetActions(3).Should().NotContain(info => info.Action == PdfAction.Extract);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    internal void GetActions_ForSeveralFiles_OffersMergeAndCompress(int fileCount)
    {
        PdfActionCatalog.GetActions(fileCount)
            .Select(info => info.Action)
            .Should()
            .Equal(PdfAction.Merge, PdfAction.Compress);
    }

    [Fact]
    internal void GetActions_NeverOffersMerge_ForASingleFile()
    {
        PdfActionCatalog.GetActions(1).Should().NotContain(info => info.Action == PdfAction.Merge);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    internal void GetActions_NeverOffersSplit_ForMultipleFiles(int fileCount)
    {
        PdfActionCatalog.GetActions(fileCount).Should().NotContain(info => info.Action == PdfAction.Split);
    }

    [Fact]
    internal void GetActions_PromotesMerge_WhenSeveralFilesAreSelected()
    {
        // The popup gives the first entry its own prominent button.
        PdfActionCatalog.GetActions(3)[0].Action.Should().Be(PdfAction.Merge);
    }

    [Fact]
    internal void CreateOperations_ForMerge_ProducesASingleOperationOverTheWholeSelection()
    {
        PdfActionCatalog.CreateOperations(PdfAction.Merge, ["a.pdf", "b.pdf", "c.pdf"])
            .Should()
            .ContainSingle();
    }

    [Fact]
    internal void CreateOperations_ForCompress_ProducesOneOperationPerFile()
    {
        PdfActionCatalog.CreateOperations(PdfAction.Compress, ["a.pdf", "b.pdf", "c.pdf"])
            .Should()
            .HaveCount(3);
    }

    [Fact]
    internal void CreateOperations_ForSplit_ProducesOneOperationPerFile()
    {
        PdfActionCatalog.CreateOperations(PdfAction.Split, ["a.pdf"])
            .Should()
            .ContainSingle();
    }

    [Fact]
    internal void CreateOperations_NamesEachPerFileOperationAfterItsFile()
    {
        PdfActionCatalog.CreateOperations(PdfAction.Compress, [@"C:\docs\report.pdf"])[0]
            .DisplayName
            .Should()
            .Be("report.pdf");
    }

    [Fact]
    internal void CreateOperations_RejectsAnUnknownAction()
    {
        Action act = () => PdfActionCatalog.CreateOperations((PdfAction)999, ["a.pdf"]);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    internal void CreateOperations_CanBuildEveryActionThatStartsImmediately()
    {
        foreach (int fileCount in (int[])[1, 2, 4])
        {
            string[] paths = [.. Enumerable.Range(1, fileCount).Select(i => $@"C:\docs\file{i}.pdf")];

            foreach (PdfActionInfo info in PdfActionCatalog.GetActions(fileCount).Where(a => !a.RequiresConfiguration))
            {
                PdfActionCatalog.CreateOperations(info.Action, paths).Should().NotBeEmpty();
            }
        }
    }

    [Fact]
    internal void CreateMergeOperations_ProducesASingleOperation_ForTheSuppliedOrder()
    {
        PdfActionCatalog.CreateMergeOperations(["b.pdf", "a.pdf", "c.pdf"])
            .Should()
            .ContainSingle();
    }

    [Fact]
    internal void CreateExtractOperations_ProducesASingleOperation()
    {
        PdfActionCatalog.CreateExtractOperations(@"C:\docs\report.pdf", [0, 2, 4])
            .Should()
            .ContainSingle();
    }

    [Fact]
    internal void CreateExtractOperations_NamesTheOperationAfterItsFile()
    {
        PdfActionCatalog.CreateExtractOperations(@"C:\docs\report.pdf", [0])[0]
            .DisplayName
            .Should()
            .Be("report.pdf");
    }

    [Fact]
    internal void GetExtractProgressText_CountsThePages()
    {
        PdfActionCatalog.GetExtractProgressText(4).Should().Contain("4");
    }

    [Fact]
    internal void GetProgressText_NamesTheFile_ForASinglePerFileAction()
    {
        PdfActionCatalog.GetProgressText(PdfAction.Compress, [@"C:\docs\report.pdf"])
            .Should()
            .Contain("report.pdf");
    }

    [Fact]
    internal void GetProgressText_CountsTheFiles_ForAMultiFileAction()
    {
        PdfActionCatalog.GetProgressText(PdfAction.Merge, ["a.pdf", "b.pdf", "c.pdf"])
            .Should()
            .Contain("3");
    }

    [Fact]
    internal void GetProgressText_RejectsAnUnknownAction()
    {
        Action act = () => PdfActionCatalog.GetProgressText((PdfAction)999, ["a.pdf"]);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
