namespace WindowSill.ClipboardHistory.FirstTimeSetup;

/// <summary>
/// View for the first-time setup contributor that prompts the user to enable Windows clipboard history.
/// </summary>
internal sealed partial class ClipboardHistoryFirstTimeSetupContributorView : UserControl
{
    private readonly ClipboardHistoryFirstTimeSetupContributor _contributor;

    internal ClipboardHistoryFirstTimeSetupContributorView(ClipboardHistoryFirstTimeSetupContributor contributor)
    {
        _contributor = contributor;
        InitializeComponent();
    }

    private async void EnableClipboardHistoryCard_Click(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:clipboard"));
    }
}
