using System.Diagnostics;
using System.Text.Json.Serialization;

namespace WindowSill.AppLauncher.Core.AppInfo;

internal sealed class FileAppInfo : AppInfo, IJsonOnDeserialized, IEquatable<FileAppInfo>
{
    [JsonPropertyName("file_path")]
    public required string FilePath { get; init; }

    public override bool Equals(object? obj)
    {
        return (obj is FileAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(FileAppInfo? other)
    {
        return base.Equals(other) && FilePath == other?.FilePath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), FilePath);
    }

    public override void OnDeserialized()
    {
        if (string.IsNullOrEmpty(OverrideAppIconPath) && !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
        {
            AppIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.GetIconFromFileOrFolderAsync(FilePath), runTaskImmediately: false);
        }
        else
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new FileAppInfo
        {
            DefaultDisplayName = this.DefaultDisplayName,
            DisplayName = this.DisplayName,
            OverrideAppIconPath = this.OverrideAppIconPath,
            FilePath = this.FilePath,
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }

    public override ValueTask LaunchAsync(bool asAdmin)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = FilePath,
            UseShellExecute = true,
            WorkingDirectory = System.IO.Path.GetDirectoryName(FilePath)
        };

        Process.Start(processStartInfo);

        return ValueTask.CompletedTask;
    }

    public override void OpenLocation()
    {
        if (File.Exists(FilePath))
        {
            OpenLocation(FilePath);
        }
    }
}
