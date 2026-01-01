using FuzzySharp;
using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

public sealed partial class ExchangeAddFavoriteCurrencyContentDialog : UserControl
{
    private readonly ContentDialog _contentDialog;
    private readonly string[] _favoriteCurrencyCodes;
    private readonly IReadOnlyList<CurrencyValue> _allCurrencies;

    public ExchangeAddFavoriteCurrencyContentDialog(ContentDialog contentDialog, string[] favoriteCurrencyCodes, IReadOnlyDictionary<string, string> currencyNameMap)
    {
        _contentDialog = contentDialog;
        _favoriteCurrencyCodes = favoriteCurrencyCodes.Select(item => item.ToLower()).ToArray();
        _allCurrencies = currencyNameMap
            .Select(kvp => new CurrencyValue(1.0, kvp.Value, kvp.Key))
            .ToList();

        InitializeComponent();
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Since selecting an item will also change the text,
        // only listen to changes caused by user entering text.
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            // simple search pass.
            string searchQuery = sender.Text;
            List<CurrencyValue> filteredShortcutList = PerformSimpleSearch(searchQuery);

            // Fuzzy search, if we didn't find a result initially.
            if (filteredShortcutList.Count == 0)
            {
                filteredShortcutList = PerformFuzzySearch(searchQuery);
            }

            if (_favoriteCurrencyCodes?.Length > 0)
            {
                filteredShortcutList
                    = filteredShortcutList
                    .Where(item =>
                    {
                        int index = _favoriteCurrencyCodes.IndexOf(item.IsoCurrency);
                        return index == -1;
                    }).ToList();
            }

            sender.ItemsSource = filteredShortcutList;
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        _contentDialog.Tag = args.SelectedItem;
        _contentDialog.IsPrimaryButtonEnabled = _contentDialog.Tag is not null;
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _contentDialog.Tag = args.ChosenSuggestion;
        if (_contentDialog.Tag is null && args.QueryText is not null)
        {
            List<CurrencyValue> filteredList = PerformSimpleSearch(args.QueryText);

            if (_favoriteCurrencyCodes?.Length > 0)
            {
                filteredList
                    = filteredList
                    .Where(item =>
                    {
                        int index = _favoriteCurrencyCodes.IndexOf(item.IsoCurrency);
                        return index == -1;
                    }).ToList();
            }

            _contentDialog.Tag = filteredList.FirstOrDefault(item => string.Equals(item.Currency, args.QueryText, StringComparison.CurrentCultureIgnoreCase));
        }

        _contentDialog.IsPrimaryButtonEnabled = _contentDialog.Tag is not null;
        if (_contentDialog.IsPrimaryButtonEnabled)
        {
            _contentDialog.Hide();
        }
    }

    private List<CurrencyValue> PerformSimpleSearch(string searchQuery)
    {
        List<CurrencyValue> filteredExchangeRates = [];
        if (_allCurrencies is not null)
        {
            for (int i = 0; i < _allCurrencies.Count; i++)
            {
                CurrencyValue currency = _allCurrencies[i];

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

    private List<CurrencyValue> PerformFuzzySearch(string searchQuery)
    {
        List<CurrencyValue> filteredExchangeRates = [];
        if (_allCurrencies is not null)
        {
            for (int i = 0; i < _allCurrencies.Count; i++)
            {
                CurrencyValue currency = _allCurrencies[i];

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
}
