using Windows.ApplicationModel.DataTransfer;
using Windows.System;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Settings;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Builds <see cref="MenuFlyout"/> instances for meeting sill items.
/// Layout follows the Dato-style design with Join action, meeting details,
/// Time Zones sub-menu, More sub-menu, Hide, and Show in Calendar.
/// </summary>
internal static class MeetingFlyoutBuilder
{
    /// <summary>
    /// Populates a <see cref="MenuFlyout"/> with meeting items.
    /// Called on every flyout open so content always reflects current state
    /// (travel time, countdown text, etc.).
    /// </summary>
    /// <param name="flyout">The flyout to populate (items are cleared first by the caller).</param>
    /// <param name="viewModel">The meeting view model.</param>
    /// <param name="worldClockService">The world clock service for time zone sub-menu.</param>
    /// <param name="settingsProvider">The settings provider for maps/travel preferences.</param>
    /// <param name="onHide">Callback invoked when the user chooses to hide the meeting.</param>
    internal static void PopulateItems(
        MenuFlyout flyout,
        MeetingSillItemViewModel viewModel,
        WorldClockService worldClockService,
        ISettingsProvider settingsProvider,
        Action onHide)
    {

        // ── Join action (top, if video call exists) ──
        if (viewModel.HasVideoCall)
        {
            string providerName = viewModel.VideoCallProviderName ?? "Meeting";
            var joinItem = new MenuFlyoutItem
            {
                Text = string.Format(
                    "/WindowSill.Date/Meetings/JoinWith".GetLocalizedString(),
                    providerName),
                Icon = new FontIcon { Glyph = "\uE714" },
                KeyboardAcceleratorTextOverride = "J",
            };
            joinItem.Click += (_, _) =>
            {
                if (viewModel.VideoCallUrl is not null)
                {
                    Launcher.LaunchUriAsync(viewModel.VideoCallUrl).AsTask().ForgetSafely();
                }
            };
            flyout.Items.Add(joinItem);
            flyout.Items.Add(new MenuFlyoutSeparator());
        }

        // ── Meeting details ──
        // Full meeting title (bold)
        var titleItem = new MenuFlyoutItem
        {
            Text = viewModel.Title,
            IsEnabled = false,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        };
        flyout.Items.Add(titleItem);

        // ── Countdown text (accent-colored) ──
        var countdownItem = new MenuFlyoutItem
        {
            Text = viewModel.CountdownText,
            IsEnabled = false,
        };
        flyout.Items.Add(countdownItem);

        // ── Date + time range ──
        var dateTimeItem = new MenuFlyoutItem
        {
            Text = $"{viewModel.DateText} at {viewModel.TimeRangeText}",
            IsEnabled = false,
        };
        flyout.Items.Add(dateTimeItem);

        // ── Location (if present) — clickable, opens maps ──
        if (viewModel.HasLocation)
        {
            var locationItem = new MenuFlyoutItem
            {
                Text = viewModel.Location!,
                Icon = new FontIcon { Glyph = "\uE707" }, // MapPin
            };
            locationItem.Click += (_, _) =>
            {
                MapsProvider provider = settingsProvider.GetSetting(Settings.Settings.PreferredMapsProvider);
                Uri mapsUrl = provider.BuildDirectionsUrl(viewModel.Location!);
                Launcher.LaunchUriAsync(mapsUrl).AsTask().ForgetSafely();
            };
            flyout.Items.Add(locationItem);
        }

        // ── Travel time (if estimated) ──
        if (viewModel.HasTravelTime)
        {
            Settings.TravelMode travelMode = settingsProvider.GetSetting(Settings.Settings.TravelMode);
            var travelItem = new MenuFlyoutItem
            {
                Text = viewModel.TravelTimeText!,
                IsEnabled = false,
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe UI Emoji"), Glyph = travelMode.ToIconGlyph() },
            };
            flyout.Items.Add(travelItem);
        }

        flyout.Items.Add(new MenuFlyoutSeparator());

        // ── Time Zones sub-menu ──
        IReadOnlyList<WorldClockEntry> worldClocks = worldClockService.GetEntries();
        if (worldClocks.Count > 0)
        {
            var timeZonesSubMenu = new MenuFlyoutSubItem
            {
                Text = "/WindowSill.Date/Meetings/TimeZones".GetLocalizedString(),
                Icon = new FontIcon { Glyph = "\uE81C" }, // Globe
            };

            foreach (WorldClockEntry clock in worldClocks)
            {
                NodaTime.DateTimeZone zone = worldClockService.GetTimeZone(clock.TimeZoneId);
                NodaTime.Instant meetingStart = NodaTime.Instant.FromDateTimeOffset(viewModel.Event.StartTime);
                NodaTime.ZonedDateTime remoteTime = meetingStart.InZone(zone);
                string formattedTime = remoteTime.ToDateTimeUnspecified()
                    .ToString("HH:mm", System.Globalization.CultureInfo.CurrentCulture);

                timeZonesSubMenu.Items.Add(new MenuFlyoutItem
                {
                    Text = $"{clock.DisplayName}    {formattedTime}",
                    IsEnabled = false,
                });
            }

            flyout.Items.Add(timeZonesSubMenu);
        }

        // ── More sub-menu ──
        var moreSubMenu = new MenuFlyoutSubItem
        {
            Text = "/WindowSill.Date/Meetings/More".GetLocalizedString(),
        };

        if (viewModel.HasLocation)
        {
            var moreLocationItem = new MenuFlyoutItem
            {
                Text = viewModel.Location!,
                Icon = new FontIcon { Glyph = "\uE707" }, // MapPin
            };
            moreLocationItem.Click += (_, _) =>
            {
                MapsProvider provider = settingsProvider.GetSetting(Settings.Settings.PreferredMapsProvider);
                Uri mapsUrl = provider.BuildDirectionsUrl(viewModel.Location!);
                Launcher.LaunchUriAsync(mapsUrl).AsTask().ForgetSafely();
            };
            moreSubMenu.Items.Add(moreLocationItem);
        }

        if (viewModel.OrganizerText is not null)
        {
            moreSubMenu.Items.Add(new MenuFlyoutItem
            {
                Text = viewModel.OrganizerText,
                IsEnabled = false,
                Icon = new FontIcon { Glyph = "\uE77B" }, // Contact
            });
        }

        if (viewModel.VideoCallUrl is not null)
        {
            var copyLinkItem = new MenuFlyoutItem
            {
                Text = "/WindowSill.Date/Meetings/CopyMeetingLink".GetLocalizedString(),
                Icon = new FontIcon { Glyph = "\uE8C8" }, // Copy
            };
            copyLinkItem.Click += (_, _) =>
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(viewModel.VideoCallUrl.ToString());
                Clipboard.SetContent(dataPackage);
            };
            moreSubMenu.Items.Add(copyLinkItem);
        }

        var copyDetailsItem = new MenuFlyoutItem
        {
            Text = "/WindowSill.Date/Meetings/CopyEventDetails".GetLocalizedString(),
            Icon = new FontIcon { Glyph = "\uE8C8" }, // Copy
        };
        copyDetailsItem.Click += (_, _) =>
        {
            string details = $"{viewModel.Title}\n{viewModel.DateText} at {viewModel.TimeRangeText}";
            if (viewModel.HasLocation)
            {
                details += $"\n{viewModel.Location}";
            }
            if (viewModel.VideoCallUrl is not null)
            {
                details += $"\n{viewModel.VideoCallUrl}";
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(details);
            Clipboard.SetContent(dataPackage);
        };
        moreSubMenu.Items.Add(copyDetailsItem);

        flyout.Items.Add(moreSubMenu);

        flyout.Items.Add(new MenuFlyoutSeparator());

        // ── Hide This Reminder ──
        var hideItem = new MenuFlyoutItem
        {
            Text = "/WindowSill.Date/Meetings/HideReminder".GetLocalizedString(),
            Icon = new FontIcon { Glyph = "\uE7ED" }, // Mute
            KeyboardAcceleratorTextOverride = "H",
        };
        hideItem.Click += (_, _) => onHide();
        flyout.Items.Add(hideItem);

        // ── Show in Calendar ──
        if (viewModel.WebLink is not null)
        {
            var showInCalItem = new MenuFlyoutItem
            {
                Text = "/WindowSill.Date/Meetings/ShowInCalendar".GetLocalizedString(),
                Icon = new FontIcon { Glyph = "\uE8A7" }, // OpenInNewWindow
            };
            showInCalItem.Click += (_, _) =>
            {
                Launcher.LaunchUriAsync(viewModel.WebLink).AsTask().ForgetSafely();
            };
            flyout.Items.Add(showInCalItem);
        }
    }
}
