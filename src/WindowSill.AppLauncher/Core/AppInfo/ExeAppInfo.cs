using System.Diagnostics;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowSill.AppLauncher.Core.AppInfo;

internal sealed partial class ExeAppInfo : AppInfo, IJsonOnDeserialized, IEquatable<ExeAppInfo>
{
    [JsonPropertyName("exe_file_path")]
    public required string ExeFilePath { get; init; }

    [JsonPropertyName("arguments")]
    [ObservableProperty]
    public partial string? Arguments { get; set; }

    [JsonPropertyName("always_as_admin")]
    [ObservableProperty]
    public partial bool AlwaysRunAsAdmin { get; set; }

    public override bool Equals(object? obj)
    {
        return (obj is ExeAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(ExeAppInfo? other)
    {
        return base.Equals(other) && ExeFilePath == other?.ExeFilePath && Arguments == other?.Arguments;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ExeFilePath, Arguments);
    }

    public override void OnDeserialized()
    {
        if (string.IsNullOrEmpty(OverrideAppIconPath) && !string.IsNullOrWhiteSpace(ExeFilePath) && File.Exists(ExeFilePath))
        {
            AppIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.GetIconFromFileOrFolderAsync(ExeFilePath), runTaskImmediately: false);
        }
        else
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new ExeAppInfo
        {
            DefaultDisplayName = this.DefaultDisplayName,
            DisplayName = this.DisplayName,
            OverrideAppIconPath = this.OverrideAppIconPath,
            ExeFilePath = this.ExeFilePath,
            Arguments = this.Arguments,
            AlwaysRunAsAdmin = this.AlwaysRunAsAdmin
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }

    public override ValueTask LaunchAsync(bool asAdmin)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = ExeFilePath,
            UseShellExecute = true,
            Verb = asAdmin || AlwaysRunAsAdmin ? "runas" : null,
            WorkingDirectory = System.IO.Path.GetDirectoryName(ExeFilePath)
        };

        if (!string.IsNullOrWhiteSpace(Arguments))
        {
            processStartInfo.Arguments = Arguments;
        }

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
        if (File.Exists(ExeFilePath))
        {
            OpenLocation(ExeFilePath);
        }
    }
}
