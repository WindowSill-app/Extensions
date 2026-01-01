using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using Windows.ApplicationModel.DataTransfer;

namespace WindowSill.ClipboardHistory.Settings;

internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    public SettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
        Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;
    }

    public int MaximumHistoryCount
    {
        get => _settingsProvider.GetSetting(Settings.MaximumHistoryCount);
        set => _settingsProvider.SetSetting(Settings.MaximumHistoryCount, value);
    }

    public bool HidePasswords
    {
        get => _settingsProvider.GetSetting(Settings.HidePasswords);
        set => _settingsProvider.SetSetting(Settings.HidePasswords, value);
    }

    public bool IsClipboardHistoryEnabled => Clipboard.IsHistoryEnabled();

    private void Clipboard_HistoryEnabledChanged(object? sender, object e)
    {
        OnPropertyChanged(nameof(IsClipboardHistoryEnabled));
    }
}
