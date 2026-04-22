namespace WindowSill.Date.Views;

/// <summary>
/// A simple "please wait" content shown during OAuth authentication flows
/// that redirect to a browser or system dialog.
/// </summary>
internal sealed partial class OAuthConnectContent : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthConnectContent"/> class.
    /// </summary>
    /// <param name="message">The message to display (e.g., "Sign in using your browser...").</param>
    public OAuthConnectContent(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }
}
