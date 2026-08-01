using Path = System.IO.Path;

namespace WindowSill.FileHelper.Helpers;

/// <summary>
/// Computes output file names using the Windows Explorer convention of appending a parenthesized counter
/// (e.g. "document.pdf", "document (1).pdf", "document (2).pdf", ...).
/// </summary>
/// <remarks>
/// Only the naming convention lives here. Deciding which candidate is actually free is deliberately left to
/// <see cref="Core.SafeOutputWriter"/>, which resolves collisions atomically at move time — a pre-check here would
/// be both racy and, for operations whose result is a folder rather than a file, wrong.
/// </remarks>
internal static class OutputPathHelper
{
    /// <summary>
    /// Computes the Explorer-style candidate path for the given base file name and counter, without touching the
    /// file system: counter 0 yields "{fileNameWithoutExtension}{extension}" and counter n &gt;= 1 yields
    /// "{fileNameWithoutExtension} (n){extension}".
    /// </summary>
    /// <param name="directory">The directory the candidate path is placed in.</param>
    /// <param name="fileNameWithoutExtension">The base file name, without extension or counter suffix.</param>
    /// <param name="extensionWithDot">The file extension, including the leading dot (e.g. ".pdf"). Pass an empty
    /// string to name a folder rather than a file.</param>
    /// <param name="counter">0 for the base name itself; 1, 2, 3, ... for "(1)", "(2)", "(3)", ...</param>
    internal static string GetCandidatePath(string directory, string fileNameWithoutExtension, string extensionWithDot, int counter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(counter);

        string fileName = counter == 0
            ? fileNameWithoutExtension + extensionWithDot
            : $"{fileNameWithoutExtension} ({counter}){extensionWithDot}";

        return Path.Combine(directory, fileName);
    }
}
