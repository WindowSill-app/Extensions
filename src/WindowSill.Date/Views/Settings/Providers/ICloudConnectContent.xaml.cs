namespace WindowSill.Date.Views;

/// <summary>
/// Credential input form for iCloud account connections.
/// Collects Apple ID and app-specific password.
/// </summary>
internal sealed partial class ICloudConnectContent : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ICloudConnectContent"/> class.
    /// </summary>
    public ICloudConnectContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the validity of the form fields changes.
    /// </summary>
    public event EventHandler? FormValidityChanged;

    /// <summary>
    /// Gets the Apple ID entered by the user.
    /// </summary>
    public string AppleId => AppleIdTextBox.Text.Trim();

    /// <summary>
    /// Gets the app-specific password entered by the user.
    /// </summary>
    public string AppPassword => AppPasswordBox.Password;

    /// <summary>
    /// Gets a value indicating whether all required fields have been filled.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(AppleId)
        && !string.IsNullOrEmpty(AppPassword);

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
