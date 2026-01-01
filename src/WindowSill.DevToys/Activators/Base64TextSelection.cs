using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.DevToys.Core;

namespace WindowSill.DevToys.Activators;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivationTypeName, PredefinedActivationTypeNames.TextSelection)]
internal sealed partial class Base64TextSelection : ISillTextSelectionActivator
{
    internal const string ActivationTypeName = "base64Selection";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(Base64Helper.IsBase64DataStrict(selectedText));
    }
}
