using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using NodaTime;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the World Clocks settings tab. Manages timezone search,
/// add/remove/reorder/rename operations.
/// </summary>
internal sealed partial class WorldClockSettingsViewModel : ObservableObject
{
    private readonly WorldClockService _worldClockService;
    private readonly ISettingsProvider _settingsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockSettingsViewModel"/> class.
    /// </summary>
    /// <param name="worldClockService">The world clock service for CRUD operations.</param>
    /// <param name="settingsProvider">The settings provider.</param>
    public WorldClockSettingsViewModel(WorldClockService worldClockService, ISettingsProvider settingsProvider)
    {
        _worldClockService = worldClockService;
        _settingsProvider = settingsProvider;
        LoadEntries();
    }

    /// <summary>
    /// Gets the configured world clock entries.
    /// </summary>
    public ObservableCollection<WorldClockSettingsItemViewModel> Entries { get; } = [];

    /// <summary>
    /// Gets the city search results for the AutoSuggestBox.
    /// </summary>
    public ObservableCollection<CitySearchResult> SearchResults { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the entry list is empty.
    /// </summary>
    [ObservableProperty]
    public partial bool HasNoEntries { get; private set; } = true;

    /// <summary>
    /// Handles text changes in the city search box.
    /// </summary>
    /// <param name="query">The current search text.</param>
    public void OnSearchTextChanged(string query)
    {
        SearchResults.Clear();

        IReadOnlyList<CitySearchResult> results = _worldClockService.SearchCities(query);
        foreach (CitySearchResult result in results)
        {
            SearchResults.Add(result);
        }
    }

    /// <summary>
    /// Adds a world clock entry from a search result.
    /// </summary>
    /// <param name="result">The selected city search result.</param>
    public void AddFromSearchResult(CitySearchResult result)
    {
        _worldClockService.AddEntry(result.TimeZoneId);
        LoadEntries();
    }

    // ── Bar placement ──

    /// <summary>
    /// Gets the available world clock placement options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<WorldClockPlacement>> PlacementOptions { get; } =
    [
        new(WorldClockPlacement.BeforeDateSill, "/WindowSill.Date/WorldClocks/PlacementBefore".GetLocalizedString()),
        new(WorldClockPlacement.AfterDateSill, "/WindowSill.Date/WorldClocks/PlacementAfter".GetLocalizedString()),
        new(WorldClockPlacement.ByTimezone, "/WindowSill.Date/WorldClocks/PlacementByTimezone".GetLocalizedString()),
    ];

    /// <summary>
    /// Gets or sets the selected world clock placement.
    /// </summary>
    public FormatOptionItem<WorldClockPlacement>? SelectedPlacement
    {
        get => PlacementOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.WorldClockPlacement));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.WorldClockPlacement))
            {
                _settingsProvider.SetSetting(Settings.Settings.WorldClockPlacement, value.Value);
                OnPropertyChanged();
            }
        }
    }

    private void LoadEntries()
    {
        Entries.Clear();

        IReadOnlyList<WorldClockEntry> entries = _worldClockService.GetEntries();
        Instant now = SystemClock.Instance.GetCurrentInstant();

        // Sort by UTC offset so entries are ordered west-to-east.
        var sorted = entries
            .OrderBy(e => _worldClockService.GetTimeZone(e.TimeZoneId).GetUtcOffset(now).Milliseconds)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            Entries.Add(new WorldClockSettingsItemViewModel(sorted[i], _worldClockService, LoadEntries));
        }

        HasNoEntries = Entries.Count == 0;
    }
}
