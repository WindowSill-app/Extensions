using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// Describes the UI and behavior for an account connection dialog.
/// OAuth providers (Outlook, Google) typically show a "please wait" spinner,
/// while credential providers (CalDAV, iCloud) show an input form with a submit button.
/// </summary>
/// <remarks>
/// The host (<see cref="Views.SettingsView"/>) uses this to build a <c>ContentDialog</c>:
/// <list type="bullet">
///   <item>If <see cref="PrimaryButtonText"/> is <see langword="null"/>, <see cref="ConnectAsync"/> is called
///   immediately and the dialog shows only a Cancel button.</item>
///   <item>If <see cref="PrimaryButtonText"/> is set, the dialog shows a primary "Submit" button.
///   <see cref="ConnectAsync"/> is called when the user clicks it.</item>
/// </list>
/// In both cases, the Cancel button triggers the <see cref="CancellationToken"/> passed to <see cref="ConnectAsync"/>.
/// </remarks>
public abstract class ConnectExperience
{
    /// <summary>
    /// Gets the visual content displayed inside the connection dialog.
    /// </summary>
    public abstract FrameworkElement Content { get; }

    /// <summary>
    /// Gets the text for the dialog's primary action button (e.g., "Sign in").
    /// Return <see langword="null"/> for OAuth flows that don't need a submit button.
    /// </summary>
    public virtual string? PrimaryButtonText => null;

    /// <summary>
    /// Gets a value indicating whether the primary button should be enabled.
    /// Only relevant when <see cref="PrimaryButtonText"/> is not <see langword="null"/>.
    /// </summary>
    public virtual bool CanSubmit => true;

    /// <summary>
    /// Raised when <see cref="CanSubmit"/> changes, allowing the host to update button state.
    /// </summary>
    public event EventHandler? CanSubmitChanged;

    /// <summary>
    /// Executes the account connection flow.
    /// For OAuth providers, this initiates the browser/WAM flow.
    /// For credential providers, this validates and submits the entered credentials.
    /// </summary>
    /// <param name="parentWindowHandle">
    /// The native window handle (HWND) of the parent window, required by
    /// broker-based authentication flows such as WAM.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that is cancelled when the user clicks the Cancel button.
    /// </param>
    /// <returns>The newly connected account.</returns>
    public abstract Task<CalendarAccount> ConnectAsync(IntPtr parentWindowHandle, CancellationToken cancellationToken);

    /// <summary>
    /// Raises the <see cref="CanSubmitChanged"/> event.
    /// </summary>
    protected void OnCanSubmitChanged()
    {
        CanSubmitChanged?.Invoke(this, EventArgs.Empty);
    }
}
