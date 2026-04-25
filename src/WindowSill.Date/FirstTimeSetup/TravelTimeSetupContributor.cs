using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.Date.Core.Services;

namespace WindowSill.Date.FirstTimeSetup;

/// <summary>
/// First-time setup step that offers travel time estimation and requests location access.
/// Only meaningful when the user has added at least one calendar account.
/// </summary>
internal sealed partial class TravelTimeSetupContributor : ObservableObject, IFirstTimeSetupContributor
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IGeoLocationService _geoLocationService;
    private readonly DateFirstTimeSetupState _setupState;
    private readonly MeetingStateService _meetingStateService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TravelTimeSetupContributor"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider.</param>
    /// <param name="geoLocationService">The geolocation service for requesting location access.</param>
    /// <param name="setupState">Shared state across setup steps.</param>
    /// <param name="meetingStateService">The meeting state service for triggering a sync.</param>
    internal TravelTimeSetupContributor(
        ISettingsProvider settingsProvider,
        IGeoLocationService geoLocationService,
        DateFirstTimeSetupState setupState,
        MeetingStateService meetingStateService)
    {
        _settingsProvider = settingsProvider;
        _geoLocationService = geoLocationService;
        _setupState = setupState;
        _meetingStateService = meetingStateService;
    }

    /// <inheritdoc/>
    public bool CanContinue => true;

    /// <inheritdoc/>
    public FrameworkElement GetView()
    {
        return new TravelTimeSetupContributorView(_settingsProvider, _geoLocationService, _setupState, _meetingStateService);
    }
}
