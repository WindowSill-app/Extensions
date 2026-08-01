using System.Runtime.InteropServices;

namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Compares strings the way File Explorer does, so numbers inside a name sort numerically
/// (<c>page2</c> before <c>page10</c>) instead of lexicographically.
/// </summary>
/// <remarks>
/// This delegates to the same shell function Explorer uses, rather than reimplementing the rules, so a set of images
/// is combined in exactly the order the user sees them listed.
/// </remarks>
internal sealed class NaturalStringComparer : IComparer<string?>
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    internal static NaturalStringComparer Instance { get; } = new();

    private NaturalStringComparer()
    {
    }

    /// <inheritdoc />
    public int Compare(string? x, string? y)
        => (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => StrCmpLogicalW(x, y),
        };

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string x, string y);
}
