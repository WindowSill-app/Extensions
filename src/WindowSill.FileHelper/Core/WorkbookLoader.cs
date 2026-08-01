using Syncfusion.XlsIO;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Opens spreadsheets — workbooks and character-separated text alike — with Syncfusion XlsIO.
/// </summary>
/// <remarks>
/// CSV and TSV are only distinguished by their delimiter, so both are loaded through the same delimiter-aware
/// overload; a <see langword="null"/> delimiter means the file is a real workbook.
/// </remarks>
internal static class WorkbookLoader
{
    /// <summary>
    /// Opens the given file as a workbook.
    /// </summary>
    /// <param name="engine">The XlsIO engine that owns the workbook.</param>
    /// <param name="sourcePath">Path to the file to open.</param>
    /// <param name="delimiter">The field delimiter for character-separated sources, or <see langword="null"/> for a workbook.</param>
    /// <returns>The opened workbook.</returns>
    internal static IWorkbook Open(ExcelEngine engine, string sourcePath, string? delimiter)
    {
        IApplication application = engine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;

        using FileStream input = File.OpenRead(sourcePath);
        return delimiter is null
            ? application.Workbooks.Open(input)
            : application.Workbooks.Open(input, delimiter);
    }
}
