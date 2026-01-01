using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

internal sealed partial class ExchangeSettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    internal ExchangeSettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;

        IsLoading = true;
        LoadFavoriteCurrenciesAsync().ForgetSafely();
    }

    internal ObservableCollection<CurrencyValue> FavoriteIsoCurrencyCodes { get; } = [];

    [ObservableProperty]
    internal partial bool IsLoading { get; set; }

    internal void DeleteCurrency(CurrencyValue currencyValue)
    {
        FavoriteIsoCurrencyCodes.Remove(currencyValue);
    }

    private void SaveSettings()
    {
        string[] newOrdering = FavoriteIsoCurrencyCodes.Select(s => s.IsoCurrency).ToArray();
        _settingsProvider.SetSetting(ExchangeSettings.FavoriteIsoCurrencyCodes, newOrdering);
    }

    private async Task LoadFavoriteCurrenciesAsync()
    {
        await Task.Run(async () =>
        {
            string[] favoriteCurrencyCodes = _settingsProvider.GetSetting(ExchangeSettings.FavoriteIsoCurrencyCodes) ?? [];

            var loadedCurrencies = new List<CurrencyValue>();
            for (int i = 0; i < favoriteCurrencyCodes.Length; i++)
            {
                string isoCode = favoriteCurrencyCodes[i];
                if (!string.IsNullOrWhiteSpace(isoCode))
                {
                    string? currencyName = await ExchangeRateHelper.GetCurrencyNameAsync(isoCode);
                    loadedCurrencies.Add(new CurrencyValue(1.0, currencyName ?? isoCode, isoCode.ToUpper()));
                }
            }

            await ThreadHelper.RunOnUIThreadAsync(async () =>
            {
                FavoriteIsoCurrencyCodes.Clear();
                foreach (CurrencyValue currency in loadedCurrencies)
                {
                    FavoriteIsoCurrencyCodes.Add(currency);
                }

                FavoriteIsoCurrencyCodes.CollectionChanged += FavoriteIsoCurrencyCodes_CollectionChanged;

                if ((await ExchangeRateHelper.GetCurrencyNameMapAsync()) is not null)
                {
                    IsLoading = false;
                }
            });
        });
    }

    private void FavoriteIsoCurrencyCodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SaveSettings();
    }
}
