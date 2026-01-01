using System.Diagnostics;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowSill.AppLauncher.Core.AppInfo;

internal sealed partial class ShortcutAppInfo : AppInfo, IEquatable<ShortcutAppInfo>
{
    [JsonPropertyName("shortcut_file_path")]
    public required string ShortcutFilePath { get; init; }

    [JsonPropertyName("icon_path")]
    public required string IconPath { get; init; }

    [JsonPropertyName("target_path")]
    public required string TargetPath { get; init; }

    [JsonPropertyName("always_as_admin")]
    [ObservableProperty]
    public partial bool AlwaysRunAsAdmin { get; set; }

    public override bool Equals(object? obj)
    {
        return (obj is ShortcutAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(ShortcutAppInfo? other)
    {
        return base.Equals(other) && ShortcutFilePath == other?.ShortcutFilePath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ShortcutFilePath);
    }

    public override void OnDeserialized()
    {
        string iconPath = !string.IsNullOrEmpty(IconPath)
            ? IconPath
            : TargetPath;
        if (string.IsNullOrEmpty(OverrideAppIconPath) && !string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
        {
            AppIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.GetIconFromFileOrFolderAsync(iconPath), runTaskImmediately: false);
        }
        else
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new ShortcutAppInfo
        {
            DefaultDisplayName = this.DefaultDisplayName,
            DisplayName = this.DisplayName,
            OverrideAppIconPath = this.OverrideAppIconPath,
            ShortcutFilePath = this.ShortcutFilePath,
            IconPath = this.IconPath,
            TargetPath = this.TargetPath,
            AlwaysRunAsAdmin = this.AlwaysRunAsAdmin
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }

    public override ValueTask LaunchAsync(bool asAdmin)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = ShortcutFilePath,
            UseShellExecute = true,
            Verb = asAdmin || AlwaysRunAsAdmin ? "runas" : null
        };

        try
        {
            Process.Start(processStartInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined UAC prompt or other error occurred
        }

        return ValueTask.CompletedTask;
    }

    public override void OpenLocation()
    {
        if (File.Exists(ShortcutFilePath))
        {
            OpenLocation(ShortcutFilePath);
        }
    }
}
