using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the World Clocks settings tab. Manages timezone search,
/// add/remove/reorder/rename operations.
/// </summary>
internal sealed partial class WorldClockSettingsViewModel : ObservableObject
{
    private readonly WorldClockService _worldClockService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockSettingsViewModel"/> class.
    /// </summary>
    /// <param name="worldClockService">The world clock service for CRUD operations.</param>
    public WorldClockSettingsViewModel(WorldClockService worldClockService)
    {
        _worldClockService = worldClockService;
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

    /// <summary>
    /// Persists the current order of <see cref="Entries"/> after a drag-and-drop reorder.
    /// The ListView has already mutated the ObservableCollection; this saves the new order.
    /// </summary>
    public void PersistCurrentOrder()
    {
        var orderedIds = Entries.Select(e => e.TimeZoneId).ToList();
        _worldClockService.ReorderEntries(orderedIds);
    }

    private void LoadEntries()
    {
        Entries.Clear();

        IReadOnlyList<WorldClockEntry> entries = _worldClockService.GetEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            Entries.Add(new WorldClockSettingsItemViewModel(entries[i], _worldClockService, LoadEntries));
        }

        HasNoEntries = Entries.Count == 0;
    }
}
