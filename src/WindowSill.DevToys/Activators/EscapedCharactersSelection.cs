using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using WindowSill.API;

namespace WindowSill.DevToys.Activators;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivationTypeName, PredefinedActivationTypeNames.TextSelection)]
internal sealed partial class EscapedCharactersSelection : ISillTextSelectionActivator
{
    internal const string ActivationTypeName = "escapedCharactersSelection";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(EscapeCharactersRegex().IsMatch(selectedText));
    }

    [GeneratedRegex(@"\\[nrtfb\\""]")]
    private static partial Regex EscapeCharactersRegex();
}
