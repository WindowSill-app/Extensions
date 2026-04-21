using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Geocodes addresses to coordinates using the OpenStreetMap Nominatim API.
/// Caches results in-memory and throttles requests to 1 per second per Nominatim policy.
/// </summary>
[Export(typeof(IGeocodingService))]
internal sealed class NominatimGeocodingService : IGeocodingService, IDisposable
{
    private const string NominatimBaseUrl = "https://nominatim.openstreetmap.org/search";
    private const string UserAgent = "WindowSill-Date-Extension/1.0 (https://getwindowsill.app)";

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, GeoCoordinate?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _throttle = new(1, 1);

    private DateTimeOffset _lastRequestTime = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="NominatimGeocodingService"/> class.
    /// </summary>
    [ImportingConstructor]
    public NominatimGeocodingService()
    {
        _logger = this.Log();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    /// <inheritdoc/>
    public async Task<GeoCoordinate?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        // Check cache first.
        string normalizedAddress = address.Trim();
        if (_cache.TryGetValue(normalizedAddress, out GeoCoordinate? cached))
        {
            return cached;
        }

        await _throttle.WaitAsync(cancellationToken);
        try
        {
            // Re-check cache after acquiring the semaphore (another thread may have populated it).
            if (_cache.TryGetValue(normalizedAddress, out cached))
            {
                return cached;
            }

            // Enforce 1 req/sec per Nominatim policy.
            TimeSpan elapsed = DateTimeOffset.Now - _lastRequestTime;
            if (elapsed < TimeSpan.FromSeconds(1))
            {
                await Task.Delay(TimeSpan.FromSeconds(1) - elapsed, cancellationToken);
            }

            string url = $"{NominatimBaseUrl}?q={Uri.EscapeDataString(normalizedAddress)}&format=json&limit=1";
            _lastRequestTime = DateTimeOffset.Now;

            NominatimResult[]? results = await _httpClient.GetFromJsonAsync<NominatimResult[]>(url, cancellationToken);

            if (results is { Length: > 0 })
            {
                if (double.TryParse(results[0].Lat, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat)
                    && double.TryParse(results[0].Lon, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    var coordinate = new GeoCoordinate(lat, lon);
                    _cache[normalizedAddress] = coordinate;
                    return coordinate;
                }
            }

            // Cache the miss to avoid retrying the same bad address.
            _cache[normalizedAddress] = null;
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Nominatim geocoding failed for: {Address}", normalizedAddress);
            return null;
        }
        finally
        {
            _throttle.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
        _throttle.Dispose();
    }

    private sealed class NominatimResult
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
    }
}
