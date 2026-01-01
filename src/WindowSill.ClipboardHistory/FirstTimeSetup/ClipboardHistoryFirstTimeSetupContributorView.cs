using CommunityToolkit.WinUI.Controls;

namespace WindowSill.ClipboardHistory.FirstTimeSetup;

internal sealed class ClipboardHistoryFirstTimeSetupContributorView : UserControl
{
    private readonly ClipboardHistoryFirstTimeSetupContributor _contributor;
    private readonly SettingsCard _enableClipboardHistoryCard = new();

    public ClipboardHistoryFirstTimeSetupContributorView(ClipboardHistoryFirstTimeSetupContributor contributor)
    {
        _contributor = contributor;

        this.Content(
            new StackPanel()
                .Spacing(16)
                .Children(
                    new TextBlock()
                        .TextWrapping(TextWrapping.WrapWholeWords)
                        .Text("WindowSill's clipboard history leverage Windows Clipboard history feature to work. The Windows Clipboard history needs to be enabled."),

                    _enableClipboardHistoryCard
                        .Header("Enable Windows Clipboard history")
                        .HeaderIcon(
                            new FontIcon()
                                .Glyph("\uE713")
                        )
                        .ActionIcon(
                            new FontIcon()
                                .Glyph("\uE8A7")
                        )
                        .IsClickEnabled(true)
                )
        );

        _enableClipboardHistoryCard.Click += OpenWindowsClipboardHistorySettingsCard_Click;
    }

    private async void OpenWindowsClipboardHistorySettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new System.Uri("ms-settings:clipboard"));
    }
}
