using System.ComponentModel.Composition;

using WindowSill.API;

namespace WindowSill.UnitConverter.Dimension;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivatorName, baseName: null)]
internal sealed class DimensionActivator : ISillTextSelectionActivator
{
    internal const string ActivatorName = "Dimension";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return new ValueTask<bool>(UnitHelper.TryDetectDimension(selectedText, cancellationToken, out _));
    }
}
