using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using WindowSill.API;
using WindowSill.Terminal.ViewModels;
using WinRT.Interop;

namespace WindowSill.Terminal.Views;

/// <summary>
/// Configuration page for setting up command execution parameters.
/// </summary>
internal sealed partial class ConfigurePage : Page
{
    [DllImport("user32.dll")]
    private static extern nint GetActiveWindow();

    public ConfigurePage()
    {
        InitializeComponent();
    }

    internal CommandItemViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (CommandItemViewModel)e.Parameter;
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
