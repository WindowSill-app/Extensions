using Windows.Storage;
using Windows.Storage.Pickers;
using WindowSill.API;
using WindowSill.AppLauncher.Core;
using WindowSill.AppLauncher.Core.AppInfo;
using WinRT.Interop;

namespace WindowSill.AppLauncher.Settings;

public sealed partial class EditGroupContentDialog : UserControl
{
    internal static async Task<AppGroup?> NewGroupAsync(WindowFrameworkElement parentWindow)
    {
        var dialog = new ContentDialog
        {
            Title = "/WindowSill.AppLauncher/EditGroupContentDialog/NewGroup".GetLocalizedString(),
            PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
            PrimaryButtonText = "/WindowSill.AppLauncher/EditGroupContentDialog/Save".GetLocalizedString(),
            CloseButtonText = "/WindowSill.AppLauncher/EditGroupContentDialog/Cancel".GetLocalizedString(),
            XamlRoot = parentWindow.XamlRoot,
        };

        var viewModel = new EditGroupContentDialogViewModel(dialog, new AppGroup());

        dialog.Content
            = new EditGroupContentDialog(
                parentWindow,
                viewModel);

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return viewModel.ToAppGroup();
        }

        return null;
    }

    internal static async Task<AppGroup?> EditGroupAsync(WindowFrameworkElement parentWindow, AppGroup appGroup)
    {
        var dialog = new ContentDialog
        {
            Title = "/WindowSill.AppLauncher/EditGroupContentDialog/EditGroup".GetLocalizedString(),
            PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
            PrimaryButtonText = "/WindowSill.AppLauncher/EditGroupContentDialog/Save".GetLocalizedString(),
            CloseButtonText = "/WindowSill.AppLauncher/EditGroupContentDialog/Cancel".GetLocalizedString(),
            XamlRoot = parentWindow.XamlRoot,
        };

        var viewModel = new EditGroupContentDialogViewModel(dialog, appGroup.Clone());

        dialog.Content
            = new EditGroupContentDialog(
                parentWindow,
                viewModel);

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return viewModel.ToAppGroup();
        }

        return null;
    }

    private readonly WindowFrameworkElement _parentWindow;

    private EditGroupContentDialog(
        WindowFrameworkElement parentWindow,
        EditGroupContentDialogViewModel viewModel)
    {
        _parentWindow = parentWindow;
        ViewModel = viewModel;

        InitializeComponent();

        AddAppFlyout.Content = new AppSelectionFlyout(parentWindow, viewModel);
    }

    internal EditGroupContentDialogViewModel ViewModel { get; }

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

    private void AddFilesButton_Click(object sender, RoutedEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            openPicker.FileTypeFilter.Add("*");

            nint hwnd = WindowNative.GetWindowHandle(_parentWindow.UnderlyingWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            for (int i = 0; i < files?.Count; i++)
            {
                ViewModel.AddFile(files[i]);
            }
        }).ForgetSafely();
    }

    private void AddFoldersButton_Click(object sender, RoutedEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var openPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            nint hwnd = WindowNative.GetWindowHandle(_parentWindow.UnderlyingWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                ViewModel.AddFolder(folder);
            }
        }).ForgetSafely();
    }

    private void DeleteAppInfoButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var appInfo = (AppInfo)button.DataContext;
        ViewModel.Items.Remove(appInfo);
    }

    private void EditAppInfoButton_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        var button = (Button)sender;
        if (args.NewValue is AppInfo appInfo)
        {
            button.Flyout = new Flyout
            {
                Content = new EditAppInfoFlyout(_parentWindow, appInfo),
            };
        }
    }

    private void ChangeGroupIconMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
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
                ViewModel.OverrideGroupIconPath = file.Path;
                ViewModel.UpdateIcon();
            }
        }).ForgetSafely();
    }

    private void ResetGroupIconMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OverrideGroupIconPath = null;
        ViewModel.UpdateIcon();
    }
}
