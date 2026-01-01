using WindowSill.API;
using WindowSill.AppLauncher.Core;
using WindowSill.AppLauncher.Core.AppInfo;

namespace WindowSill.AppLauncher;

public sealed partial class AppGroupSillPopupContent : SillPopupContent
{
    private readonly ISettingsProvider _settingsProvider;

    internal AppGroupSillPopupContent(AppGroup group, ISettingsProvider settingsProvider)
    {
        DefaultStyleKey = typeof(AppGroupSillPopupContent);

        _settingsProvider = settingsProvider;
        AppGroup = group;
        InitializeComponent();
    }

    internal AppGroup AppGroup { get; }

    private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var element = (Border)sender;
        element.Background = Application.Current.Resources["ControlFillColorSecondaryBrush"] as Brush;
    }

    private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var element = (Border)sender;
        element.Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush;
    }

    private void Border_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var element = (Border)sender;
        element.Background = Application.Current.Resources["ControlFillColorTertiaryBrush"] as Brush;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsProvider.OpenSettingsPageForSill(AppLauncherSill.SillInternalName, null);
        Close();
    }

    private void LaunchAllButton_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            for (int i = 0; i < AppGroup.Items.Count; i++)
            {
                await AppGroup.Items[i].LaunchAsync(asAdmin: false);
            }
        });
        Close();
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AppInfo appInfo)
        {
            Task.Run(async () => await appInfo.LaunchAsync(asAdmin: false));
            Close();
        }
    }

    private void LaunchMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is AppInfo appInfo)
        {
            Task.Run(async () => await appInfo.LaunchAsync(asAdmin: false));
            Close();
        }
    }

    private void RunAsAdminMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is AppInfo appInfo)
        {
            Task.Run(async () => await appInfo.LaunchAsync(asAdmin: true));
            Close();
        }
    }

    private void OpenLocationMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is AppInfo appInfo)
        {
            appInfo.OpenLocation();
            Close();
        }
    }

    internal static bool IsExeOrShortcut(AppInfo app)
    {
        return app is ExeAppInfo or ShortcutAppInfo;
    }

    internal static bool IsNotUwpApp(AppInfo app)
    {
        return app is not UwpAppInfo;
    }
}
