namespace WindowSill.Date.Views;

/// <summary>
/// Credential input form for CalDAV account connections.
/// Collects server URL, username, and password.
/// </summary>
internal sealed partial class CalDavConnectContent : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalDavConnectContent"/> class.
    /// </summary>
    public CalDavConnectContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the validity of the form fields changes.
    /// </summary>
    public event EventHandler? FormValidityChanged;

    /// <summary>
    /// Gets the server URL entered by the user.
    /// </summary>
    public string ServerUrl => ServerUrlTextBox.Text.Trim();

    /// <summary>
    /// Gets the username entered by the user.
    /// </summary>
    public string Username => UsernameTextBox.Text.Trim();

    /// <summary>
    /// Gets the password entered by the user.
    /// </summary>
    public string Password => PasswordBox.Password;

    /// <summary>
    /// Gets a value indicating whether all required fields have been filled.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(ServerUrl)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrEmpty(Password);

    /// <summary>
    /// Shows an error message in the form.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void ShowError(string message)
    {
        ErrorInfoBar.Message = message;
        ErrorInfoBar.IsOpen = true;
    }

    /// <summary>
    /// Hides any displayed error message.
    /// </summary>
    public void HideError()
    {
        ErrorInfoBar.IsOpen = false;
    }

    private void OnFieldChanged(object sender, object e)
    {
        HideError();
        FormValidityChanged?.Invoke(this, EventArgs.Empty);
    }
}
