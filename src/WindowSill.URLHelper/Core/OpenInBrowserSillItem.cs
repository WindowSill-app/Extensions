using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WindowSill.API;

namespace WindowSill.URLHelper.Core;

/// <summary>
/// Creates the "Open in Browser" sill item with behavior that adapts based on the source application:
/// <list type="bullet">
///   <item>From a known browser: shows "Open in other browser" menu flyout, excluding the current browser.</item>
///   <item>From a known browser with no alternatives: no sill item is created.</item>
///   <item>From a non-browser with one detected browser: button only, no right-click menu.</item>
///   <item>From a non-browser with multiple browsers: button + right-click menu flyout.</item>
/// </list>
/// </summary>
internal static class OpenInBrowserSillItem
{
    private static readonly ILogger _logger = typeof(OpenInBrowserSillItem).Log();

    /// <summary>
    /// Creates the "Open in Browser" sill item for the given selection, or <c>null</c> if
    /// the URL comes from a browser and no alternative browsers are available.
    /// </summary>
    internal static SillListViewItem? Create(WindowTextSelection currentSelection)
    {
        string url = currentSelection.SelectedText;
        IReadOnlyList<BrowserInfo> allBrowsers = BrowserDetector.GetInstalledBrowsers();
        string? sourceAppId = currentSelection.ApplicationIdentifier;
        bool isFromBrowser = BrowserMatcher.IsKnownBrowser(sourceAppId);

        if (isFromBrowser)
        {
            // Filter out the source browser.
            List<BrowserInfo> otherBrowsers = allBrowsers
                .Where(b => !BrowserMatcher.IsMatchingBrowser(b.ExecutablePath, sourceAppId))
                .ToList();

            if (otherBrowsers.Count == 0)
            {
                return null;
            }

            var menuFlyout = new MenuFlyout();
            foreach (BrowserInfo browser in otherBrowsers)
            {
                menuFlyout.Items.Add(CreateBrowserMenuEntry(url, browser));
            }

            return new SillListViewMenuFlyoutItem(
                "/WindowSill.URLHelper/OpenInBrowser/OpenInOtherBrowser".GetLocalizedString(),
                null,
                menuFlyout);
        }
        else
        {
            var sillItem = new SillListViewButtonItem(
                "/WindowSill.URLHelper/OpenInBrowser/Title".GetLocalizedString(),
                null,
                () => OpenInDefaultBrowserAsync(url));

            if (allBrowsers.Count > 1)
            {
                sillItem.ContextFlyout = BuildBrowserMenuFlyout(url, allBrowsers);
            }

            return sillItem;
        }
    }

    private static MenuFlyout BuildBrowserMenuFlyout(string url, IReadOnlyList<BrowserInfo> browsers)
    {
        var menuFlyout = new MenuFlyout();

        var defaultItem = new MenuFlyoutItem
        {
            Text = "/WindowSill.URLHelper/OpenInBrowser/DefaultBrowser".GetLocalizedString(),
            Icon = new FontIcon { Glyph = "\uE774" },
        };
        defaultItem.Click += (_, _) => OpenInDefaultBrowserAsync(url).ForgetSafely();
        menuFlyout.Items.Add(defaultItem);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        foreach (BrowserInfo browser in browsers)
        {
            menuFlyout.Items.Add(CreateBrowserMenuEntry(url, browser));
        }

        return menuFlyout;
    }

    /// <summary>
    /// Creates a menu entry for a browser. If private mode is supported, returns a
    /// <see cref="MenuFlyoutSubItem"/> with "Open" and private mode sub-items;
    /// otherwise returns a simple <see cref="MenuFlyoutItem"/>.
    /// </summary>
    private static MenuFlyoutItemBase CreateBrowserMenuEntry(string url, BrowserInfo browser)
    {
        string exePath = browser.ExecutablePath;

        if (browser.PrivateModeFlag is not null && browser.PrivateModeName is not null)
        {
            var subItem = new MenuFlyoutSubItem { Text = browser.Name };
            LoadSubItemIconAsync(subItem, exePath).ForgetSafely();

            var openItem = new MenuFlyoutItem
            {
                Text = "/WindowSill.URLHelper/OpenInBrowser/Open".GetLocalizedString(),
                Icon = new FontIcon { Glyph = "\uE774" },
            };
            openItem.Click += (_, _) => OpenInBrowserAsync(url, exePath).ForgetSafely();

            string privateFlag = browser.PrivateModeFlag;
            var privateItem = new MenuFlyoutItem
            {
                Text = browser.PrivateModeName,
                Icon = new FontIcon { Glyph = "\uE727" },
            };
            privateItem.Click += (_, _) => OpenInBrowserAsync(url, exePath, privateFlag).ForgetSafely();

            subItem.Items.Add(openItem);
            subItem.Items.Add(privateItem);
            return subItem;
        }
        else
        {
            var item = new MenuFlyoutItem { Text = browser.Name };
            item.Click += (_, _) => OpenInBrowserAsync(url, exePath).ForgetSafely();
            LoadIconAsync(item, exePath).ForgetSafely();
            return item;
        }
    }

    private static async Task LoadSubItemIconAsync(MenuFlyoutSubItem item, string exePath)
    {
        ImageSource? icon = await BrowserIconExtractor.GetIconForExeAsync(exePath);
        if (icon is not null)
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                item.Icon = new ImageIcon { Source = icon };
            });
        }
    }

    private static async Task LoadIconAsync(MenuFlyoutItem item, string exePath)
    {
        ImageSource? icon = await BrowserIconExtractor.GetIconForExeAsync(exePath);
        if (icon is not null)
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                item.Icon = new ImageIcon { Source = icon };
            });
        }
    }

    private static async Task OpenInDefaultBrowserAsync(string url)
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL '{Url}' in default browser.", url);
        }
    }

    private static Task OpenInBrowserAsync(string url, string browserExePath, string? extraFlag = null)
    {
        try
        {
            string arguments = extraFlag is not null ? $"{extraFlag} {url}" : url;
            Process.Start(new ProcessStartInfo
            {
                FileName = browserExePath,
                Arguments = arguments,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL '{Url}' in browser '{Browser}'.", url, browserExePath);
        }

        return Task.CompletedTask;
    }
}
