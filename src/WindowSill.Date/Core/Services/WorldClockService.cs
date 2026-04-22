using System.ComponentModel.Composition;
using System.Text.Json;

using NodaTime;
using NodaTime.TimeZones;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Manages world clock entries — CRUD, persistence, and timezone city search via NodaTime TZDB.
/// </summary>
[Export]
internal sealed class WorldClockService
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISettingsProvider _settingsProvider;
    private readonly IDateTimeZoneProvider _tzProvider;
    private readonly TzdbZoneLocation[] _allLocations;

    private List<WorldClockEntry>? _cachedEntries;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockService"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for persisting world clock entries.</param>
    [ImportingConstructor]
    public WorldClockService(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
        _tzProvider = DateTimeZoneProviders.Tzdb;

        // Cache all zone locations for city search.
        TzdbDateTimeZoneSource source = TzdbDateTimeZoneSource.Default;
        _allLocations = source.ZoneLocations?.ToArray() ?? [];
    }

    /// <summary>
    /// Raised when the world clock entries list changes.
    /// </summary>
    public event EventHandler? EntriesChanged;

    /// <summary>
    /// Gets the current list of world clock entries.
    /// </summary>
    /// <returns>The list of configured world clock entries.</returns>
    public IReadOnlyList<WorldClockEntry> GetEntries()
    {
        _cachedEntries ??= LoadEntries();
        return _cachedEntries;
    }

    /// <summary>
    /// Gets only the entries that are pinned to the sill bar.
    /// </summary>
    /// <returns>The entries where <see cref="WorldClockEntry.ShowInBar"/> is <see langword="true"/>.</returns>
    public IReadOnlyList<WorldClockEntry> GetPinnedEntries()
    {
        return GetEntries().Where(e => e.ShowInBar).ToList();
    }

    /// <summary>
    /// Sets whether a world clock entry is pinned to the sill bar.
    /// </summary>
    /// <param name="timeZoneId">The IANA timezone identifier.</param>
    /// <param name="showInBar">Whether to show this clock in the bar.</param>
    public void SetShowInBar(string timeZoneId, bool showInBar)
    {
        List<WorldClockEntry> entries = GetMutableEntries();
        WorldClockEntry? entry = entries.FirstOrDefault(e => e.TimeZoneId == timeZoneId);

        if (entry is null || entry.ShowInBar == showInBar)
        {
            return;
        }

        entry.ShowInBar = showInBar;
        SaveEntries(entries);
        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Searches for cities matching the given query string.
    /// Returns IANA timezone IDs with friendly city names.
    /// </summary>
    /// <param name="query">The search text to match against city/zone names.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>A list of matching timezone locations.</returns>
    public IReadOnlyList<CitySearchResult> SearchCities(string query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return [];
        }

        var existingZones = GetEntries()
            .Select(e => e.TimeZoneId)
            .ToHashSet(StringComparer.Ordinal);

        return _allLocations
            .Where(loc => !existingZones.Contains(loc.ZoneId)
                && MatchesQuery(loc, query))
            .OrderBy(loc => GetCityName(loc.ZoneId))
            .Take(maxResults)
            .Select(loc =>
            {
                DateTimeZone zone = _tzProvider[loc.ZoneId];
                Offset offset = zone.GetUtcOffset(SystemClock.Instance.GetCurrentInstant());
                return new CitySearchResult(
                    loc.ZoneId,
                    GetCityName(loc.ZoneId),
                    loc.CountryName,
                    offset);
            })
            .ToList();
    }

    /// <summary>
    /// Adds a new world clock entry.
    /// </summary>
    /// <param name="timeZoneId">The IANA timezone identifier.</param>
    /// <param name="customDisplayName">An optional custom display name.</param>
    public void AddEntry(string timeZoneId, string? customDisplayName = null)
    {
        List<WorldClockEntry> entries = GetMutableEntries();

        if (entries.Any(e => e.TimeZoneId == timeZoneId))
        {
            return;
        }

        entries.Add(new WorldClockEntry
        {
            TimeZoneId = timeZoneId,
            CustomDisplayName = string.IsNullOrWhiteSpace(customDisplayName) ? null : customDisplayName,
        });

        SaveEntries(entries);
        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes a world clock entry by timezone ID.
    /// </summary>
    /// <param name="timeZoneId">The IANA timezone identifier to remove.</param>
    public void RemoveEntry(string timeZoneId)
    {
        List<WorldClockEntry> entries = GetMutableEntries();
        int removed = entries.RemoveAll(e => e.TimeZoneId == timeZoneId);

        if (removed > 0)
        {
            SaveEntries(entries);
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Updates the custom display name for a world clock entry.
    /// </summary>
    /// <param name="timeZoneId">The IANA timezone identifier.</param>
    /// <param name="customDisplayName">The new custom name, or <see langword="null"/> to revert to default.</param>
    public void UpdateDisplayName(string timeZoneId, string? customDisplayName)
    {
        List<WorldClockEntry> entries = GetMutableEntries();
        WorldClockEntry? entry = entries.FirstOrDefault(e => e.TimeZoneId == timeZoneId);

        if (entry is null)
        {
            return;
        }

        entry.CustomDisplayName = string.IsNullOrWhiteSpace(customDisplayName) ? null : customDisplayName;
        SaveEntries(entries);
        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reorders entries to match the given timezone ID order.
    /// Called after a drag-and-drop reorder in the UI.
    /// </summary>
    /// <param name="orderedTimeZoneIds">The timezone IDs in the desired order.</param>
    public void ReorderEntries(IReadOnlyList<string> orderedTimeZoneIds)
    {
        List<WorldClockEntry> entries = GetMutableEntries();
        var lookup = entries.ToDictionary(e => e.TimeZoneId);

        var reordered = new List<WorldClockEntry>(orderedTimeZoneIds.Count);
        foreach (string id in orderedTimeZoneIds)
        {
            if (lookup.TryGetValue(id, out WorldClockEntry? entry))
            {
                reordered.Add(entry);
            }
        }

        SaveEntries(reordered);
        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the NodaTime <see cref="DateTimeZone"/> for a given IANA timezone ID.
    /// </summary>
    /// <param name="timeZoneId">The IANA timezone identifier.</param>
    /// <returns>The resolved timezone, or UTC if not found.</returns>
    public DateTimeZone GetTimeZone(string timeZoneId)
    {
        try
        {
            return _tzProvider[timeZoneId];
        }
        catch
        {
            return DateTimeZone.Utc;
        }
    }

    private List<WorldClockEntry> GetMutableEntries()
    {
        _cachedEntries ??= LoadEntries();
        return _cachedEntries;
    }

    private List<WorldClockEntry> LoadEntries()
    {
        string json = _settingsProvider.GetSetting(Settings.Settings.WorldClockEntries);

        try
        {
            return JsonSerializer.Deserialize<List<WorldClockEntry>>(json, jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SaveEntries(List<WorldClockEntry> entries)
    {
        _cachedEntries = entries;
        string json = JsonSerializer.Serialize(entries, jsonOptions);
        _settingsProvider.SetSetting(Settings.Settings.WorldClockEntries, json);
    }

    private static bool MatchesQuery(TzdbZoneLocation location, string query)
    {
        string cityName = GetCityName(location.ZoneId);
        return cityName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || location.ZoneId.Contains(query, StringComparison.OrdinalIgnoreCase)
            || location.CountryName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCityName(string zoneId)
    {
        int lastSlash = zoneId.LastIndexOf('/');
        string city = lastSlash >= 0 ? zoneId[(lastSlash + 1)..] : zoneId;
        return city.Replace('_', ' ');
    }
}
