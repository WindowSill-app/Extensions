using System.ComponentModel.Composition;
using WindowSill.API;

namespace WindowSill.DevToys.Activators;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivationTypeName, PredefinedActivationTypeNames.TextSelection)]
internal sealed class Base64ImageSelection : ISillTextSelectionActivator
{
    internal const string ActivationTypeName = "base64ImageSelection";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(IsBase64ImageString(selectedText));
    }

    private static bool IsBase64ImageString(string text)
    {
        ReadOnlySpan<char> trimmedData = text.AsSpan().Trim();

        return trimmedData.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase)
            || trimmedData.StartsWith("data:image/jpeg;base64,", StringComparison.OrdinalIgnoreCase)
            || trimmedData.StartsWith("data:image/bmp;base64,", StringComparison.OrdinalIgnoreCase)
            || trimmedData.StartsWith("data:image/gif;base64,", StringComparison.OrdinalIgnoreCase)
            || trimmedData.StartsWith("data:image/x-icon;base64,", StringComparison.OrdinalIgnoreCase)
            || trimmedData.StartsWith("data:image/svg+xml;base64,", StringComparison.OrdinalIgnoreCase)
            || trimmedData.StartsWith("data:image/webp;base64,", StringComparison.OrdinalIgnoreCase);
    }
}
