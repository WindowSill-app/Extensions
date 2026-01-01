using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using FuzzySharp;
using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

[Export]
internal sealed partial class ExchangeConverterViewModel : ViewModelBase
{
    [Import]
    private ISettingsProvider _settingsProvider = null!;

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    private DateTime _latestFetchTime = DateTime.MinValue;
    private IReadOnlyDictionary<string, double>? _cachedExchangeRates;
    private IReadOnlyList<CurrencyValue>? _exchangeRates;
    private string? _cachedBaseCurrency;

    internal SillListViewItem GetView(WindowTextSelection currentSelection)
    {
        Guard.IsTrue(UnitHelper.TryDetectCurrency(currentSelection.SelectedText, CancellationToken.None, out CurrencyValue? currency));
        Guard.IsNotNull(currency);

        IsLoading = true;
        _exchangeRates = null;
        FilteredExchangeRates.Clear();
        SearchQuery = string.Empty;

        var sillPopupItem
            = new SillListViewPopupItem(
                currency.FormattedValue,
                null,
                new ExchangeConverterSillPopupContent(this));

        Task.Run(async () =>
        {
            _exchangeRates = await GetExchangeRatesAsync(currency);
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                // Show favorite currency value as the display text of the sill.
                if (_exchangeRates is not null)
                {
                    string[] favoriteIsoCodes = _settingsProvider.GetSetting(ExchangeSettings.FavoriteIsoCurrencyCodes);
                    if (favoriteIsoCodes?.Length > 0)
                    {
                        string favoriteIsoCode = favoriteIsoCodes[0];
                        if (string.Equals(favoriteIsoCode, currency.IsoCurrency, StringComparison.OrdinalIgnoreCase)
                            && favoriteIsoCodes.Length >= 2)
                        {
                            // The first favorite currency is the same as the source currency, use the second one so we show a different currency (and therefore a conversion).
                            favoriteIsoCode = favoriteIsoCodes[1];
                        }

                        CurrencyValue? favoriteCurrency = _exchangeRates.FirstOrDefault(c => string.Equals(c.IsoCurrency, favoriteIsoCode, StringComparison.OrdinalIgnoreCase));
                        if (favoriteCurrency is not null)
                        {
                            sillPopupItem.Content = favoriteCurrency.FormattedValue;
                        }
                    }
                }

                // Trigger search to populate the list of the popup, even if the user didn't open it yet.
                OnSearchQueryChanged(SearchQuery);

                IsLoading = false;
            });
        }).ForgetSafely();

        return sillPopupItem;
    }

    [ObservableProperty]
    internal partial bool IsLoading { get; set; }

    internal ObservableCollection<CurrencyValue> FilteredExchangeRates { get; } = new();

    [ObservableProperty]
    internal partial string SearchQuery { get; set; }

    partial void OnSearchQueryChanged(string value)
    {
        // simple search pass.
        string searchQuery = value;
        IReadOnlyList<CurrencyValue>? exchangeRates = _exchangeRates?.ToArray(); // Make a copy to avoid threading issues.
        List<CurrencyValue> filteredShortcutList = PerformSimpleSearch(searchQuery, exchangeRates);

        // Fuzzy search, if we didn't find a result initially.
        if (filteredShortcutList.Count == 0)
        {
            filteredShortcutList = PerformFuzzySearch(searchQuery, exchangeRates);
        }

        string[] favoriteIsoCodes = _settingsProvider.GetSetting(ExchangeSettings.FavoriteIsoCurrencyCodes);
        if (favoriteIsoCodes?.Length > 0)
        {
            favoriteIsoCodes = favoriteIsoCodes.Select(code => code.ToLower()).ToArray();
            filteredShortcutList
                = filteredShortcutList
                .OrderBy(item =>
                {
                    int index = favoriteIsoCodes.IndexOf(item.IsoCurrency);
                    return index >= 0 ? index : int.MaxValue;
                })
                .ThenBy(item => item.Currency)
                .ToList();
        }

        FilteredExchangeRates.SynchronizeWith(filteredShortcutList);
    }

    private static List<CurrencyValue> PerformSimpleSearch(string searchQuery, IReadOnlyList<CurrencyValue>? exchangeRates)
    {
        List<CurrencyValue> filteredExchangeRates = [];
        if (exchangeRates is not null)
        {
            for (int i = 0; i < exchangeRates.Count; i++)
            {
                CurrencyValue currency = exchangeRates[i];

                bool match = false;
                if (string.IsNullOrEmpty(searchQuery))
                {
                    match = true;
                }
                else if (currency.Currency.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }
                else if (searchQuery.Contains(currency.Currency, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }
                else if (currency.IsoCurrency.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }
                else if (searchQuery.Contains(currency.IsoCurrency, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }

                if (match)
                {
                    filteredExchangeRates.Add(currency);
                }
            }
        }

        return filteredExchangeRates;
    }

    private static List<CurrencyValue> PerformFuzzySearch(string searchQuery, IReadOnlyList<CurrencyValue>? exchangeRates)
    {
        List<CurrencyValue> filteredExchangeRates = [];
        if (exchangeRates is not null)
        {
            for (int i = 0; i < exchangeRates.Count; i++)
            {
                CurrencyValue currency = exchangeRates[i];

                bool match = false;
                if (Fuzz.TokenInitialismRatio(searchQuery, currency.Currency) >= 75)
                {
                    match = true;
                }
                else if (Fuzz.WeightedRatio(searchQuery, currency.Currency) >= 50)
                {
                    match = true;
                }
                else if (Fuzz.TokenInitialismRatio(searchQuery, currency.IsoCurrency) >= 75)
                {
                    match = true;
                }
                else if (Fuzz.WeightedRatio(searchQuery, currency.IsoCurrency) >= 50)
                {
                    match = true;
                }

                if (match)
                {
                    filteredExchangeRates.Add(currency);
                }
            }
        }

        return filteredExchangeRates;
    }

    private async Task<IReadOnlyList<CurrencyValue>?> GetExchangeRatesAsync(CurrencyValue currency)
    {
        string isoCurrency = currency.IsoCurrency.ToLowerInvariant();
        Guard.IsNotNullOrWhiteSpace(isoCurrency);

        // Check if we need to refresh the cache
        bool needsRefresh
            = _cachedExchangeRates is null
            || _cachedBaseCurrency != isoCurrency
            || DateTime.UtcNow - _latestFetchTime >= _cacheExpiration;

        if (needsRefresh)
        {
            IReadOnlyDictionary<string, double>? newExchangeRates = await ExchangeRateHelper.LoadExchangeRatesAsync(isoCurrency);
            if (newExchangeRates is not null)
            {
                _cachedExchangeRates = newExchangeRates;
                _cachedBaseCurrency = isoCurrency;
                _latestFetchTime = DateTime.UtcNow;
            }
        }

        if (_cachedExchangeRates is null)
        {
            return null;
        }

        // Convert the base currency value to all other currencies
        var results = new List<CurrencyValue>(_cachedExchangeRates.Count);
        foreach ((string? targetCurrency, double rate) in _cachedExchangeRates)
        {
            double convertedValue = currency.Value * rate;
            string currencyName = await ExchangeRateHelper.GetCurrencyNameAsync(targetCurrency) ?? targetCurrency;
            if (!string.IsNullOrWhiteSpace(currencyName) && !string.IsNullOrWhiteSpace(targetCurrency))
            {
                results.Add(new CurrencyValue(convertedValue, currencyName, targetCurrency));
            }
        }

        return results.OrderBy(item => item.Currency).ToArray();
    }
}
