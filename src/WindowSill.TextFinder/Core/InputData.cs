using WindowSill.API;

namespace WindowSill.TextFinder.Core;

internal sealed record InputData
{
    public WindowTextSelection? TextSelection { get; init; }

    public string? DragAndDropActivatorTypeName { get; init; }

    public string? FilePath { get; init; }
}
