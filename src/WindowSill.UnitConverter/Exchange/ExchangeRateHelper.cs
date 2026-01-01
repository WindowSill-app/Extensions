using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

internal static class ExchangeRateHelper
{
    // From https://github.com/fawazahmed0/exchange-api
    // Exchange rates are updated every 24 hours.
    private const string CurrencyListUrl = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies.min.json";
    private const string FallbackCurrencyListUrl = "https://latest.currency-api.pages.dev/v1/currencies.min.json";

    private const string ExchangeRateUrlTemplate = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/{0}.min.json";
    private const string FallbackExchangeRateUrlTemplate = "https://latest.currency-api.pages.dev/v1/currencies/{0}.min.json";

    private static readonly ILogger logger = typeof(ExchangeRateHelper).Log();
    private static Dictionary<string, string>? currencyNameMap;

    internal static async Task<IReadOnlyDictionary<string, double>?> LoadExchangeRatesAsync(string isoCurrency)
    {
        using var httpClient = new HttpClient();

        try
        {
            string url = string.Format(ExchangeRateUrlTemplate, isoCurrency);
            ExchangeRateResponse? response = await httpClient.GetFromJsonAsync<ExchangeRateResponse>(url);

            if (response?.Rates is not null && response.Rates.Count > 0)
            {
                return response.Rates;
            }
        }
        catch
        {
            // Try fallback URL
        }

        try
        {
            string fallbackUrl = string.Format(FallbackExchangeRateUrlTemplate, isoCurrency);
            ExchangeRateResponse? response = await httpClient.GetFromJsonAsync<ExchangeRateResponse>(fallbackUrl);

            if (response?.Rates is not null && response.Rates.Count > 0)
            {
                return response.Rates;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load exchange rates for currency {IsoCurrency}", isoCurrency);
        }

        return null;
    }

    internal static async Task<IReadOnlyDictionary<string, string>?> GetCurrencyNameMapAsync()
    {
        if (currencyNameMap is null)
        {
            await LoadCurrencyNameMapAsync();
        }

        return currencyNameMap;
    }

    internal static async Task<string?> GetCurrencyNameAsync(string isoCurrency)
    {
        // Load currency name map once
        if (currencyNameMap is null)
        {
            await LoadCurrencyNameMapAsync();
        }

        if (currencyNameMap is not null && currencyNameMap.TryGetValue(isoCurrency.ToLowerInvariant(), out string? name)
            && !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return null;
    }

    private static async Task LoadCurrencyNameMapAsync()
    {
        using var httpClient = new HttpClient();

        try
        {
            Dictionary<string, string>? currencyMap = await httpClient.GetFromJsonAsync<Dictionary<string, string>>(CurrencyListUrl);
            if (currencyMap is not null)
            {
                currencyNameMap = currencyMap.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToDictionary();
                return;
            }
        }
        catch
        {
            // Try fallback URL
        }

        try
        {
            Dictionary<string, string>? currencyMap = await httpClient.GetFromJsonAsync<Dictionary<string, string>>(FallbackCurrencyListUrl);
            if (currencyMap is not null)
            {
                currencyNameMap = currencyMap.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToDictionary();
            }
        }
        catch (Exception ex)
        {
            // If both attempts fail, leave the map as null
            currencyNameMap = new Dictionary<string, string>();

            logger.LogError(ex, "Failed to load currency name map.");
        }
    }

    private sealed class ExchangeRateResponse
    {
        private IReadOnlyDictionary<string, double>? _rates;

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; init; }

        [JsonIgnore]
        public IReadOnlyDictionary<string, double>? Rates
        {
            get
            {
                if (_rates is not null)
                {
                    return _rates;
                }

                if (ExtensionData is null)
                {
                    return null;
                }

                // Find the currency object (it's the non-"date" property)
                foreach (KeyValuePair<string, JsonElement> kvp in ExtensionData)
                {
                    if (kvp.Key != "date" && kvp.Value.ValueKind == JsonValueKind.Object)
                    {
                        var rates = new Dictionary<string, double>();
                        foreach (JsonProperty rate in kvp.Value.EnumerateObject())
                        {
                            if (rate.Value.ValueKind == JsonValueKind.Number)
                            {
                                rates[rate.Name] = rate.Value.GetDouble();
                            }
                        }

                        _rates = rates;
                        return _rates;
                    }
                }

                return null;
            }
        }
    }
}
