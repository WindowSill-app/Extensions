using Windows.Storage;
using Windows.Storage.Pickers;
using WindowSill.API;
using WindowSill.AppLauncher.Core.AppInfo;
using WinRT.Interop;

namespace WindowSill.AppLauncher.Settings;

public sealed partial class AppSelectionFlyout : UserControl
{
    private readonly WindowFrameworkElement _parentWindow;
    private readonly EditGroupContentDialogViewModel _editGroupContentDialogViewModel;

    internal AppSelectionFlyout(WindowFrameworkElement parentWindow, EditGroupContentDialogViewModel editGroupContentDialogViewModel)
    {
        _parentWindow = parentWindow;
        _editGroupContentDialogViewModel = editGroupContentDialogViewModel;
        InitializeComponent();
    }

    internal AppSelectionFlyoutViewModel ViewModel { get; } = new();

    private void SearchBoxFocusShortcut_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        SearchBox.Focus(FocusState.Keyboard);
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AppInfo appInfo)
        {
            _editGroupContentDialogViewModel.AddAppInfo(appInfo);
        }
    }

    private void BrowseMoreAppButton_Click(object sender, RoutedEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            openPicker.FileTypeFilter.Add(".exe");
            openPicker.FileTypeFilter.Add(".lnk");

            nint hwnd = WindowNative.GetWindowHandle(_parentWindow.UnderlyingWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            for (int i = 0; i < files?.Count; i++)
            {
                _editGroupContentDialogViewModel.AddFile(files[i]);
            }
        }).ForgetSafely();
    }
}
