using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowSill.AppLauncher.Core;

internal sealed partial class AppGroup : ObservableObject, IJsonOnDeserialized
{
    [JsonPropertyName("items")]
    public IReadOnlyList<AppInfo.AppInfo> Items { get; init; } = Array.Empty<AppInfo.AppInfo>();

    [JsonPropertyName("group_name")]
    public string GroupName { get; init; } = string.Empty;

    [JsonPropertyName("override_group_icon_path")]
    public string? OverrideGroupIconPath { get; init; }

    [JsonIgnore]
    [ObservableProperty]
    internal partial TaskCompletionNotifier<ImageSource?> GroupIcon { get; set; }
        = new(() => Task.FromResult<ImageSource?>(null), runTaskImmediately: false);

    public void OnDeserialized()
    {
        if (string.IsNullOrWhiteSpace(OverrideGroupIconPath) || !File.Exists(OverrideGroupIconPath))
        {
            GroupIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.CreateGridIconAsync(Items, selectedSize: 2), runTaskImmediately: false);
        }
        else
        {
            GroupIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.GetIconFromFileOrFolderAsync(OverrideGroupIconPath), runTaskImmediately: false);
        }
    }

    internal AppGroup Clone()
    {
        var appGroup = new AppGroup
        {
            Items = Items.Select(item => item.Clone()).ToList(),
            GroupName = GroupName,
            OverrideGroupIconPath = OverrideGroupIconPath,
        };
        appGroup.OnDeserialized();
        return appGroup;
    }
}
