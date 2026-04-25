using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowSill.Date.FirstTimeSetup;

/// <summary>
/// Shared state passed between first-time setup contributors so later steps
/// can react to choices made in earlier steps.
/// </summary>
internal sealed partial class DateFirstTimeSetupState : ObservableObject
{
    /// <summary>
    /// Gets or sets a value indicating whether at least one calendar account
    /// was added during the setup flow.
    /// </summary>
    [ObservableProperty]
    public partial bool HasAddedAccount { get; set; }
}
