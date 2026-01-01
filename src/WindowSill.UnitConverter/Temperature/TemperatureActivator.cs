using System.ComponentModel.Composition;

using WindowSill.API;

namespace WindowSill.UnitConverter.Temperature;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivatorName, baseName: null)]
internal sealed class TemperatureActivator : ISillTextSelectionActivator
{
    internal const string ActivatorName = "Temperature";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return new ValueTask<bool>(UnitHelper.TryDetectTemperature(selectedText, cancellationToken, out _));
    }
}
