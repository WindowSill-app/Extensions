using CommunityToolkit.Diagnostics;

namespace WindowSill.UnitConverter.Exchange;

internal sealed record CurrencyValue
{
    public CurrencyValue(double value, string currency, string isoCurrency)
    {
        Guard.IsNotNullOrWhiteSpace(currency);
        Guard.IsNotNullOrWhiteSpace(isoCurrency);
        Value = value;
        Currency = currency;
        IsoCurrency = isoCurrency;
    }

    public double Value { get; init; }

    public string Currency { get; init; }

    public string IsoCurrency { get; init; }

    public string FormattedValue
    {
        get
        {
            string formattedValue = Value % 1 == 0 ? Value.ToString("N0") : Value.ToString("N2");
            return $"{formattedValue} {IsoCurrency.ToUpper()}";
        }
    }
}
