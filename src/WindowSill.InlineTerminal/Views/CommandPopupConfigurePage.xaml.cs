using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using WindowSill.API;
using WindowSill.InlineTerminal.ViewModels;
using WinRT.Interop;

namespace WindowSill.InlineTerminal.Views;

public sealed partial class CommandPopupConfigurePage : Page
{
    [DllImport("user32.dll")]
    private static extern nint GetActiveWindow();

    public CommandPopupConfigurePage()
    {
        InitializeComponent();
    }

    internal CommandViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (CommandViewModel)e.Parameter;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            folderPicker.FileTypeFilter.Add("*");

            nint hwnd = GetActiveWindow();
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder? folder = await folderPicker.PickSingleFolderAsync();
            if (folder is not null)
            {
                ViewModel.WorkingDirectory = folder.Path;
            }
        }).ForgetSafely();
    }
}
