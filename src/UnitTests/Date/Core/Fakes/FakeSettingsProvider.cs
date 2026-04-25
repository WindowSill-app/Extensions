using WindowSill.API;

namespace UnitTests.Date.Core.Fakes;

/// <summary>
/// A fake <see cref="ISettingsProvider"/> that stores settings in memory
/// and properly returns default values from <see cref="SettingDefinition{T}"/>.
/// </summary>
internal sealed class FakeSettingsProvider : ISettingsProvider
{
    private readonly Dictionary<string, object> _settings = [];

    public event Windows.Foundation.TypedEventHandler<ISettingsProvider, SettingChangedEventArgs>? SettingChanged;

    public T GetSetting<T>(SettingDefinition<T> settingDefinition)
    {
        string key = settingDefinition.Name;
        if (_settings.TryGetValue(key, out object? value))
        {
            return (T)value;
        }

        return settingDefinition.DefaultValue;
    }

    public void SetSetting<T>(SettingDefinition<T> settingDefinition, T value)
    {
        string key = settingDefinition.Name;
        _settings[key] = value!;
        SettingChanged?.Invoke(this, new SettingChangedEventArgs(key, value));
    }

    public void ResetSetting<T>(SettingDefinition<T> settingDefinition)
    {
        _settings.Remove(settingDefinition.Name);
    }

    public bool IsActivelyControlledByAdmin<T>(SettingDefinition<T> settingDefinition) => false;

    public void OpenSettingsPageForSill(string internalSillName, string? sillSettingViewTitle) { }
}
