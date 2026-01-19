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

    private const int MaxRetries = 3;
    private static readonly TimeSpan initialRetryDelay = TimeSpan.FromSeconds(1);

    private static readonly ILogger logger = typeof(ExchangeRateHelper).Log();
    private static Dictionary<string, string>? currencyNameMap;

    internal static async Task<IReadOnlyDictionary<string, double>?> LoadExchangeRatesAsync(string isoCurrency)
    {
        using var httpClient = new HttpClient();

        string url = string.Format(ExchangeRateUrlTemplate, isoCurrency);
        ExchangeRateResponse? response = await ExecuteWithRetryAsync(
            () => httpClient.GetFromJsonAsync<ExchangeRateResponse>(url),
            $"load exchange rates from primary URL for {isoCurrency}");

        if (response?.Rates is not null && response.Rates.Count > 0)
        {
            return response.Rates;
        }

        string fallbackUrl = string.Format(FallbackExchangeRateUrlTemplate, isoCurrency);
        response = await ExecuteWithRetryAsync(
            () => httpClient.GetFromJsonAsync<ExchangeRateResponse>(fallbackUrl),
            $"load exchange rates from fallback URL for {isoCurrency}");

        if (response?.Rates is not null && response.Rates.Count > 0)
        {
            return response.Rates;
        }

        logger.LogWarning("Failed to load exchange rates for currency {IsoCurrency} from both primary and fallback URLs.", isoCurrency);
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

        Dictionary<string, string>? currencyMap = await ExecuteWithRetryAsync(
            () => httpClient.GetFromJsonAsync<Dictionary<string, string>>(CurrencyListUrl),
            "load currency name map from primary URL");

        if (currencyMap is not null)
        {
            currencyNameMap = currencyMap.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToDictionary();
            return;
        }

        currencyMap = await ExecuteWithRetryAsync(
            () => httpClient.GetFromJsonAsync<Dictionary<string, string>>(FallbackCurrencyListUrl),
            "load currency name map from fallback URL");

        if (currencyMap is not null)
        {
            currencyNameMap = currencyMap.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToDictionary();
            return;
        }

        // If both attempts fail, leave the map as null
        currencyNameMap = null;
        logger.LogWarning("Failed to load currency name map from both primary and fallback URLs.");
    }

    private static async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> operation, string operationDescription)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;

                if (attempt < MaxRetries)
                {
                    TimeSpan delay = initialRetryDelay * Math.Pow(2, attempt);
                    logger.LogWarning(
                        ex,
                        "Attempt {Attempt} of {MaxAttempts} to {Operation} failed. Retrying in {Delay}ms...",
                        attempt + 1,
                        MaxRetries + 1,
                        operationDescription,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;

                if (attempt < MaxRetries)
                {
                    TimeSpan delay = initialRetryDelay * Math.Pow(2, attempt);
                    logger.LogWarning(
                        ex,
                        "Attempt {Attempt} of {MaxAttempts} to {Operation} timed out. Retrying in {Delay}ms...",
                        attempt + 1,
                        MaxRetries + 1,
                        operationDescription,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                // Non-transient exception, don't retry
                logger.LogWarning(ex, "Non-transient error during {Operation}. Will not retry.", operationDescription);
                return default;
            }
        }

        logger.LogWarning(
            lastException,
            "All {MaxAttempts} attempts to {Operation} failed.",
            MaxRetries + 1,
            operationDescription);

        return default;
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
