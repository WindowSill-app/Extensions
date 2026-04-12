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

        if (ViewModel.ShouldShowClickFixWarning())
        {
            ViewModel.ConfirmRunAsync = ShowClickFixWarningAsync;
        }
    }

    private async Task<bool> ShowClickFixWarningAsync()
    {
        if (!ViewModel.ShouldShowClickFixWarning())
        {
            return true;
        }

        var checkBox = new CheckBox
        {
            Content = "/WindowSill.InlineTerminal/ClickFixWarning/DoNotAskAgain".GetLocalizedString()
        };

        var dialog = new ContentDialog
        {
            Title = "/WindowSill.InlineTerminal/ClickFixWarning/Title".GetLocalizedString(),
            Content = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "/WindowSill.InlineTerminal/ClickFixWarning/Message".GetLocalizedString(),
                        TextWrapping = TextWrapping.Wrap
                    },
                    checkBox
                }
            },
            PrimaryButtonText = "/WindowSill.InlineTerminal/ClickFixWarning/Continue".GetLocalizedString(),
            CloseButtonText = "/WindowSill.InlineTerminal/ClickFixWarning/Cancel".GetLocalizedString(),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (checkBox.IsChecked == true)
            {
                ViewModel.DisableClickFixWarning();
            }

            return true;
        }

        return false;
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
