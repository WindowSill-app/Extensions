using System.ComponentModel.Composition;

using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivatorName, baseName: null)]
internal sealed class ExchangeActivator : ISillTextSelectionActivator
{
    internal const string ActivatorName = "Exchange";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return new ValueTask<bool>(UnitHelper.TryDetectCurrency(selectedText, cancellationToken, out _));
    }
}
