using System.ComponentModel.Composition;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Calculates travel time using the OpenRouteService Directions API.
/// Requires the user to provide an API key in settings.
/// </summary>
[Export(typeof(IRoutingService))]
internal sealed class OpenRouteServiceRoutingService : IRoutingService, IDisposable
{
    private const string OrsBaseUrl = "https://api.openrouteservice.org/v2/directions/driving-car";

    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouteServiceRoutingService"/> class.
    /// </summary>
    [ImportingConstructor]
    public OpenRouteServiceRoutingService(ISettingsProvider settingsProvider)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;
        _httpClient = new HttpClient();
    }

    /// <inheritdoc/>
    public string ProviderName => "OpenRouteService";

    /// <inheritdoc/>
    public async Task<TravelTimeEstimateResult?> GetTravelTimeAsync(
        GeoCoordinate from,
        GeoCoordinate to,
        CancellationToken cancellationToken = default)
    {
        string apiKey = _settingsProvider.GetSetting(Settings.Settings.OpenRouteServiceApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.NoApiKey);
        }

        try
        {
            // ORS expects coordinates as lon,lat (not lat,lon).
            string url = $"{OrsBaseUrl}?start={from.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{from.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&end={to.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{to.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RateLimited);
            }

            response.EnsureSuccessStatusCode();

            OrsResponse? orsResponse = await response.Content.ReadFromJsonAsync<OrsResponse>(cancellationToken);

            if (orsResponse?.Features is { Length: > 0 }
                && orsResponse.Features[0].Properties?.Summary is { } summary)
            {
                return TravelTimeEstimateResult.FromProvider(
                    TimeSpan.FromSeconds(summary.Duration),
                    summary.Distance,
                    ProviderName);
            }

            _logger.LogWarning("ORS returned no route features.");
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ORS routing request failed.");
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error in ORS routing.");
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    // ── ORS response JSON model ──

    private sealed class OrsResponse
    {
        [JsonPropertyName("features")]
        public OrsFeature[]? Features { get; set; }
    }

    private sealed class OrsFeature
    {
        [JsonPropertyName("properties")]
        public OrsProperties? Properties { get; set; }
    }

    private sealed class OrsProperties
    {
        [JsonPropertyName("summary")]
        public OrsSummary? Summary { get; set; }
    }

    private sealed class OrsSummary
    {
        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }
}
