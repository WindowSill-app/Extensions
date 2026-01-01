using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

internal static class ExchangeSettings
{
    /// <summary>
    /// The list of favorite ISO currency codes.
    /// </summary>
    internal static readonly SettingDefinition<string[]> FavoriteIsoCurrencyCodes
        = new(["usd", "eur"], typeof(ExchangeSettings).Assembly);
}
