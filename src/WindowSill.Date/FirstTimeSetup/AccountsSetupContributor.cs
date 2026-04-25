using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.Date.Core;

namespace WindowSill.Date.FirstTimeSetup;

/// <summary>
/// First-time setup step that lets the user connect calendar accounts.
/// </summary>
internal sealed class AccountsSetupContributor : ObservableObject, IFirstTimeSetupContributor
{
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly string _contentDirectory;
    private readonly DateFirstTimeSetupState _setupState;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsSetupContributor"/> class.
    /// </summary>
    /// <param name="calendarAccountManager">The calendar account manager.</param>
    /// <param name="contentDirectory">The plugin content directory for resolving asset paths.</param>
    /// <param name="setupState">Shared state across setup steps.</param>
    internal AccountsSetupContributor(
        CalendarAccountManager calendarAccountManager,
        string contentDirectory,
        DateFirstTimeSetupState setupState)
    {
        _calendarAccountManager = calendarAccountManager;
        _contentDirectory = contentDirectory;
        _setupState = setupState;
    }

    /// <inheritdoc/>
    public bool CanContinue => true;

    /// <inheritdoc/>
    public FrameworkElement GetView()
    {
        return new AccountsSetupContributorView(_calendarAccountManager, _contentDirectory, _setupState);
    }
}
