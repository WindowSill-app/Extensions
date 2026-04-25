using System.ComponentModel.Composition;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Settings;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Calculates travel time by calling the WindowSill server-side routing proxy,
/// which in turn forwards the request to OpenRouteService using a server-held
/// API key. End users do not need to provide an API key.
/// </summary>
[Export(typeof(IRoutingService))]
internal sealed class WindowSillTravelTimeRoutingService : IRoutingService, IDisposable
{
#if DEBUG
    private const string WindowSillBaseUrl = "http://localhost:5180";
#else
    private const string WindowSillBaseUrl = "https://getwindowsill.app";
#endif

    private const string DirectionsEndpoint = WindowSillBaseUrl + "/api/TravelTimeRouting/directions";

    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSillTravelTimeRoutingService"/> class.
    /// </summary>
    [ImportingConstructor]
    public WindowSillTravelTimeRoutingService(ISettingsProvider settingsProvider)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;
        _httpClient = new HttpClient();
    }

    /// <inheritdoc/>
    public string ProviderName => "WindowSill";

    /// <inheritdoc/>
    public async Task<TravelTimeEstimateResult?> GetTravelTimeAsync(
        GeoCoordinate from,
        GeoCoordinate to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Settings.TravelMode travelMode = _settingsProvider.GetSetting(Settings.Settings.TravelMode);
            string profile = travelMode.ToRoutingProfile();

            string url = string.Create(
                CultureInfo.InvariantCulture,
                $"{DirectionsEndpoint}?fromLat={from.Latitude}&fromLon={from.Longitude}&toLat={to.Latitude}&toLon={to.Longitude}&profile={profile}");

            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RateLimited);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "WindowSill routing proxy returned {StatusCode}.",
                    (int)response.StatusCode);
                return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
            }

            TravelTimeRoutingDirectionsResponse? routing = await response.Content
                .ReadFromJsonAsync<TravelTimeRoutingDirectionsResponse>(cancellationToken);

            if (routing is null)
            {
                _logger.LogWarning("WindowSill routing proxy returned an empty body.");
                return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
            }

            return TravelTimeEstimateResult.FromProvider(
                TimeSpan.FromSeconds(routing.DurationSeconds),
                routing.DistanceMeters,
                ProviderName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "WindowSill routing proxy request failed.");
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error while calling the WindowSill routing proxy.");
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private sealed class TravelTimeRoutingDirectionsResponse
    {
        [JsonPropertyName("durationSeconds")]
        public double DurationSeconds { get; set; }

        [JsonPropertyName("distanceMeters")]
        public double DistanceMeters { get; set; }
    }
}
