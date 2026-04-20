using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for a single world clock entry in the settings list.
/// Supports rename, delete via commands.
/// </summary>
internal sealed partial class WorldClockSettingsItemViewModel : ObservableObject
{
    private readonly WorldClockService _worldClockService;
    private readonly Action _onChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockSettingsItemViewModel"/> class.
    /// </summary>
    /// <param name="entry">The underlying world clock entry.</param>
    /// <param name="worldClockService">The service for persistence.</param>
    /// <param name="onChanged">Callback invoked after any mutation (remove, rename) to refresh the parent list.</param>
    public WorldClockSettingsItemViewModel(WorldClockEntry entry, WorldClockService worldClockService, Action onChanged)
    {
        _worldClockService = worldClockService;
        _onChanged = onChanged;

        TimeZoneId = entry.TimeZoneId;
        DefaultCityName = GetDefaultCityName(entry.TimeZoneId);
        EditableName = entry.CustomDisplayName ?? string.Empty;
    }

    /// <summary>
    /// Gets the IANA timezone identifier.
    /// </summary>
    public string TimeZoneId { get; }

    /// <summary>
    /// Gets the default city name derived from the timezone ID (e.g., "New York").
    /// </summary>
    public string DefaultCityName { get; }

    /// <summary>
    /// Gets the effective display name (custom name if set, otherwise default city name).
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(EditableName) ? DefaultCityName : EditableName;

    /// <summary>
    /// Gets or sets the editable custom display name. Empty string means "use default".
    /// </summary>
    [ObservableProperty]
    public partial string EditableName { get; set; }

    /// <summary>
    /// Saves the current <see cref="EditableName"/> as the custom display name.
    /// An empty value reverts to the default city name.
    /// </summary>
    [RelayCommand]
    private void SaveName()
    {
        string? customName = string.IsNullOrWhiteSpace(EditableName) ? null : EditableName;
        _worldClockService.UpdateDisplayName(TimeZoneId, customName);
        OnPropertyChanged(nameof(DisplayName));
        _onChanged();
    }

    /// <summary>
    /// Removes this world clock entry.
    /// </summary>
    [RelayCommand]
    private void Remove()
    {
        _worldClockService.RemoveEntry(TimeZoneId);
        _onChanged();
    }

    private static string GetDefaultCityName(string zoneId)
    {
        int lastSlash = zoneId.LastIndexOf('/');
        string city = lastSlash >= 0 ? zoneId[(lastSlash + 1)..] : zoneId;
        return city.Replace('_', ' ');
    }
}
