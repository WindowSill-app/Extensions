using FluentAssertions;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using UnitTests.Date.Core.Fakes;

namespace UnitTests.Date.Core;

public class TravelTimeEstimatorTests
{
    private readonly FakeSettingsProvider _settings;

    public TravelTimeEstimatorTests()
    {
        LoggingSetup.EnsureInitialized();
        _settings = new FakeSettingsProvider();
        _settings.SetSetting(WindowSill.Date.Settings.Settings.OpenRouteServiceApiKey, "test-key");
    }

    private static CalendarEvent CreateEvent(string? location = "123 Main St, Seattle WA")
        => new()
        {
            Id = "1",
            CalendarId = "cal",
            AccountId = "acc",
            Title = "Test Meeting",
            StartTime = DateTimeOffset.Now.AddHours(2),
            EndTime = DateTimeOffset.Now.AddHours(3),
            Location = location,
            ProviderType = CalendarProviderType.Outlook,
        };

    #region No meeting address

    [Fact]
    public async Task EstimateTravelTimeAsync_NoLocation_ReturnsNoMeetingAddress()
    {
        var estimator = CreateEstimator();
        CalendarEvent evt = CreateEvent(location: null);

        TravelTimeEstimateResult result = await estimator.EstimateTravelTimeAsync(evt);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(TravelTimeFailureReason.NoMeetingAddress);
    }

    [Fact]
    public async Task EstimateTravelTimeAsync_EmptyLocation_ReturnsNoMeetingAddress()
    {
        var estimator = CreateEstimator();
        CalendarEvent evt = CreateEvent(location: "   ");

        TravelTimeEstimateResult result = await estimator.EstimateTravelTimeAsync(evt);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(TravelTimeFailureReason.NoMeetingAddress);
    }

    #endregion

    #region Routing succeeds

    [Fact]
    public async Task EstimateTravelTimeAsync_AllSucceed_ReturnsProviderResult()
    {
        var estimator = CreateEstimator(
            userLocation: new GeoCoordinate(47.6, -122.3),
            meetingLocation: new GeoCoordinate(47.64, -122.13),
            routeResult: TravelTimeEstimateResult.FromProvider(
                TimeSpan.FromMinutes(18), 15000, "TestRouter"));
        CalendarEvent evt = CreateEvent();

        TravelTimeEstimateResult result = await estimator.EstimateTravelTimeAsync(evt);

        result.IsSuccess.Should().BeTrue();
        result.UsedFallback.Should().BeFalse();
        result.Duration.Should().Be(TimeSpan.FromMinutes(18));
        result.Provider.Should().Be("TestRouter");
        result.DistanceMeters.Should().Be(15000);
    }

    #endregion

    #region Model tests

    [Fact]
    public void GeoCoordinate_ToString_FormatsCorrectly()
    {
        var coord = new GeoCoordinate(47.606210, -122.332071);

        coord.ToString().Should().Be("47.606210,-122.332071");
    }

    [Fact]
    public void TravelTimeEstimateResult_FromProvider_IsSuccess()
    {
        var result = TravelTimeEstimateResult.FromProvider(TimeSpan.FromMinutes(20), 10000, "Test");

        result.IsSuccess.Should().BeTrue();
        result.UsedFallback.Should().BeFalse();
        result.Provider.Should().Be("Test");
    }

    [Fact]
    public void TravelTimeEstimateResult_Failed_IsNotSuccess()
    {
        var result = TravelTimeEstimateResult.Failed(TravelTimeFailureReason.NoUserLocation);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(TravelTimeFailureReason.NoUserLocation);
    }

    #endregion

    // ── Test helpers ──

    private TravelTimeEstimator CreateEstimator(
        GeoCoordinate? userLocation = null,
        GeoCoordinate? meetingLocation = null,
        TravelTimeEstimateResult? routeResult = null,
        bool useDefaults = true)
    {
        if (useDefaults)
        {
            userLocation ??= new GeoCoordinate(47.6, -122.3);
            meetingLocation ??= new GeoCoordinate(47.64, -122.13);
        }

        routeResult ??= TravelTimeEstimateResult.FromProvider(
            TimeSpan.FromMinutes(18), 15000, "TestRouter");

        return new TravelTimeEstimator(
            new FakeGeoLocationService(userLocation),
            new FakeGeocodingService(meetingLocation),
            new FakeRoutingService(routeResult),
            _settings);
    }

    private sealed class FakeGeoLocationService(GeoCoordinate? result) : IGeoLocationService
    {
        public Task<GeoCoordinate?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class FakeGeocodingService(GeoCoordinate? result) : IGeocodingService
    {
        public Task<GeoCoordinate?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class FakeRoutingService(TravelTimeEstimateResult? result) : IRoutingService
    {
        public string ProviderName => "FakeRouter";
        public Task<TravelTimeEstimateResult?> GetTravelTimeAsync(
            GeoCoordinate from, GeoCoordinate to, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
