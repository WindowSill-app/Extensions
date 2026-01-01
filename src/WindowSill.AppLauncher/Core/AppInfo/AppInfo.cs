using System.Diagnostics;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowSill.AppLauncher.Core.AppInfo;

[JsonDerivedType(typeof(ExeAppInfo), typeDiscriminator: "exe")]
[JsonDerivedType(typeof(ShortcutAppInfo), typeDiscriminator: "shortcut")]
[JsonDerivedType(typeof(UwpAppInfo), typeDiscriminator: "uwp")]
[JsonDerivedType(typeof(FolderAppInfo), typeDiscriminator: "folder")]
[JsonDerivedType(typeof(FileAppInfo), typeDiscriminator: "file")]
[DebuggerDisplay("{DisplayName}")]
internal abstract partial class AppInfo : ObservableObject, IJsonOnDeserialized, IEquatable<AppInfo>
{
    [JsonPropertyName("default_display_name")]
    public required string DefaultDisplayName { get; init; }

    [JsonPropertyName("display_name")]
    [ObservableProperty]
    public partial string DisplayName { get; set; }

    [JsonPropertyName("override_app_icon_path")]
    [ObservableProperty]
    public partial string? OverrideAppIconPath { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    internal partial TaskCompletionNotifier<ImageSource?> AppIcon { get; set; }
        = new(() => Task.FromResult<ImageSource?>(null), runTaskImmediately: false);

    public virtual void OnDeserialized()
    {
        AppIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.GetIconFromFileOrFolderAsync(OverrideAppIconPath), runTaskImmediately: false);
    }

    public abstract ValueTask LaunchAsync(bool asAdmin);

    public abstract AppInfo Clone();

    public abstract void OpenLocation();

    protected void OpenLocation(string path)
    {
        try
        {
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Failed to open explorer
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is AppInfo other && Equals(other);
    }

    public bool Equals(AppInfo? other)
    {
        return DefaultDisplayName == other?.DefaultDisplayName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DefaultDisplayName);
    }
}
