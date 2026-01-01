using System.ComponentModel.Composition;
using System.Text.Json;
using WindowSill.API;

namespace WindowSill.DevToys.Activators;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivationTypeName, PredefinedActivationTypeNames.JsonSelection)]
internal sealed class JsonArraySelection : ISillTextSelectionActivator
{
    internal const string ActivationTypeName = "jsonArraySelection";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(IsJsonArray(selectedText));
    }

    private static bool IsJsonArray(string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            return document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            // Exception in parsing json. It likely mean the text isn't a JSON.
            return false;
        }
        catch
        {
            return false;
        }
    }
}
