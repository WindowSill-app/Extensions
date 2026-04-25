using WindowSill.API;
using WindowSill.Date.Core.Services;

namespace WindowSill.Date.FirstTimeSetup;

/// <summary>
/// View for the travel time step of the first-time setup experience.
/// Offers to enable travel time estimation and requests location access when toggled on.
/// </summary>
internal sealed partial class TravelTimeSetupContributorView : UserControl
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IGeoLocationService _geoLocationService;
    private readonly DateFirstTimeSetupState _setupState;
    private readonly MeetingStateService _meetingStateService;
    private bool _isUpdatingToggle;

    internal TravelTimeSetupContributorView(
        ISettingsProvider settingsProvider,
        IGeoLocationService geoLocationService,
        DateFirstTimeSetupState setupState,
        MeetingStateService meetingStateService)
    {
        _settingsProvider = settingsProvider;
        _geoLocationService = geoLocationService;
        _setupState = setupState;
        _meetingStateService = meetingStateService;

        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateVisibility();
        _setupState.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DateFirstTimeSetupState.HasAddedAccount))
            {
                UpdateVisibility();
            }
        };
    }

    private void UpdateVisibility()
    {
        bool hasAccounts = _setupState.HasAddedAccount;
        TravelTimePanel.Visibility = hasAccounts ? Visibility.Visible : Visibility.Collapsed;
        NoAccountsPanel.Visibility = hasAccounts ? Visibility.Collapsed : Visibility.Visible;
    }

    private void TravelTimeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingToggle)
        {
            return;
        }

        if (TravelTimeToggle.IsOn)
        {
            RequestLocationAndEnableAsync().ForgetSafely();
        }
        else
        {
            _settingsProvider.SetSetting(Settings.Settings.EnableTravelTime, false);
            LocationDeniedInfoBar.IsOpen = false;
        }
    }

    private async Task RequestLocationAndEnableAsync()
    {
        LocationDeniedInfoBar.IsOpen = false;

        var location = await _geoLocationService.GetCurrentLocationAsync();

        if (location is not null)
        {
            _settingsProvider.SetSetting(Settings.Settings.EnableTravelTime, true);
            _meetingStateService.RequestRefresh();
        }
        else
        {
            // Location denied or unavailable — revert the toggle.
            _isUpdatingToggle = true;
            TravelTimeToggle.IsOn = false;
            _isUpdatingToggle = false;

            _settingsProvider.SetSetting(Settings.Settings.EnableTravelTime, false);
            LocationDeniedInfoBar.IsOpen = true;
        }
    }
}
