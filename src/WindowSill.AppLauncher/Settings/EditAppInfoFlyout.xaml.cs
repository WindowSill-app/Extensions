using Windows.Storage;
using Windows.Storage.Pickers;
using WindowSill.API;
using WindowSill.AppLauncher.Core.AppInfo;
using WinRT.Interop;

namespace WindowSill.AppLauncher.Settings;

public sealed partial class EditAppInfoFlyout : UserControl
{
    private readonly WindowFrameworkElement _parentWindow;

    internal EditAppInfoFlyout(WindowFrameworkElement parentWindow, AppInfo appInfo)
    {
        _parentWindow = parentWindow;
        ViewModel = new EditAppInfoFlyoutViewModel(appInfo);
        InitializeComponent();
    }

    internal EditAppInfoFlyoutViewModel ViewModel { get; }

    private void IconButton_Click(object sender, RoutedEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            openPicker.FileTypeFilter.Add(".ico");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".lnk");

            nint hwnd = WindowNative.GetWindowHandle(_parentWindow.UnderlyingWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                ViewModel.AppInfo.OverrideAppIconPath = file.Path;
                ViewModel.AppInfo.OnDeserialized();
            }
        }).ForgetSafely();
    }
}
