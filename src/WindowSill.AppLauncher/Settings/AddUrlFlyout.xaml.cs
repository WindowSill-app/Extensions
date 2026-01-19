namespace WindowSill.AppLauncher.Settings;

public sealed partial class AddUrlFlyout : UserControl
{
    private readonly EditGroupContentDialogViewModel _editGroupContentDialogViewModel;

    internal AddUrlFlyout(EditGroupContentDialogViewModel editGroupContentDialogViewModel)
    {
        _editGroupContentDialogViewModel = editGroupContentDialogViewModel;
        InitializeComponent();
    }

    private void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        AddButton.IsEnabled = Uri.TryCreate(UrlTextBox.Text.Trim(), UriKind.Absolute, out _);
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        string url = UrlTextBox.Text.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return;
        }

        string displayName = DisplayNameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = url;
        }

        _editGroupContentDialogViewModel.AddUrl(url, displayName);

        UrlTextBox.Text = string.Empty;
        DisplayNameTextBox.Text = string.Empty;
    }
}
