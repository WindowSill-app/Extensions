using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Services;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Shared test helpers and fakes for InlineTerminal unit tests.
/// </summary>
internal static class TestHelpers
{
    internal static ShellInfo CreateDummyShell(string name = "TestShell")
    {
        return new ShellInfo(
            name,
            "/usr/bin/test",
            escapeCommand: c => c,
            buildArguments: c => $"-c {c}",
            buildElevatedArguments: (c, f) => $"-c {c} > {f}");
    }

    internal static CommandService CreateCommandService()
    {
        return new CommandService(new FakePluginInfo(), new FakeProcessInteractionService());
    }

    internal sealed class FakePluginInfo : IPluginInfo
    {
        public string GetPluginContentDirectory() => System.IO.Path.GetTempPath();
        public string GetPluginDataFolder() => System.IO.Path.GetTempPath();
        public string GetPluginTempFolder() => System.IO.Path.GetTempPath();
    }

    internal sealed class FakeProcessInteractionService : IProcessInteractionService
    {
        public Task SimulateKeysOnWindow(WindowInfo window, params Windows.System.VirtualKey[] keys)
            => Task.CompletedTask;
        public Task SimulateKeysOnLastActiveWindow(params Windows.System.VirtualKey[] keys)
            => Task.CompletedTask;
    }

    internal sealed class FakeSettingsProvider : ISettingsProvider
    {
        private readonly Dictionary<string, object> _settings = [];

        public event Windows.Foundation.TypedEventHandler<ISettingsProvider, SettingChangedEventArgs>? SettingChanged;

        public T GetSetting<T>(SettingDefinition<T> settingDefinition)
        {
            string key = settingDefinition.GetType().FullName + settingDefinition.GetHashCode();
            if (_settings.TryGetValue(key, out object? value))
            {
                return (T)value;
            }

            return default!;
        }

        public void SetSetting<T>(SettingDefinition<T> settingDefinition, T value)
        {
            string key = settingDefinition.GetType().FullName + settingDefinition.GetHashCode();
            _settings[key] = value!;
        }

        public void ResetSetting<T>(SettingDefinition<T> settingDefinition)
        {
            string key = settingDefinition.GetType().FullName + settingDefinition.GetHashCode();
            _settings.Remove(key);
        }

        public bool IsActivelyControlledByAdmin<T>(SettingDefinition<T> settingDefinition) => false;

        public void OpenSettingsPageForSill(string internalSillName, string? sillSettingViewTitle) { }
    }
}
