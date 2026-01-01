using System.Diagnostics;
using System.Text.Json.Serialization;

namespace WindowSill.AppLauncher.Core.AppInfo;

internal sealed class FolderAppInfo : AppInfo, IJsonOnDeserialized, IEquatable<FolderAppInfo>
{
    [JsonPropertyName("folder_path")]
    public required string FolderPath { get; init; }

    public override bool Equals(object? obj)
    {
        return (obj is FolderAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(FolderAppInfo? other)
    {
        return base.Equals(other) && FolderPath == other?.FolderPath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), FolderPath);
    }

    public override void OnDeserialized()
    {
        if (string.IsNullOrEmpty(OverrideAppIconPath) && !string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath))
        {
            AppIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.GetIconFromFileOrFolderAsync(FolderPath), runTaskImmediately: false);
        }
        else
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new FolderAppInfo
        {
            DefaultDisplayName = this.DefaultDisplayName,
            DisplayName = this.DisplayName,
            OverrideAppIconPath = this.OverrideAppIconPath,
            FolderPath = this.FolderPath,
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }

    public override ValueTask LaunchAsync(bool asAdmin)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = FolderPath,
            UseShellExecute = true
        };

        Process.Start(startInfo);

        return ValueTask.CompletedTask;
    }

    public override void OpenLocation()
    {
        if (Directory.Exists(FolderPath))
        {
            OpenLocation(FolderPath);
        }
    }
}
