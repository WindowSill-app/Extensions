using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.Date.Core;

namespace WindowSill.Date.ViewModels;

internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly CalendarAccountManager _calendarAccountManager;

    public SettingsViewModel(ISettingsProvider settingsProvider, CalendarAccountManager calendarAccountManager)
    {
        _settingsProvider = settingsProvider;
        _calendarAccountManager = calendarAccountManager;
    }
}
