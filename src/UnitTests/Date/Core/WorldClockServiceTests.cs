using FluentAssertions;
using UnitTests.Date.Core.Fakes;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;

namespace UnitTests.Date.Core;

public class WorldClockServiceTests : IDisposable
{
    private readonly FakeSettingsProvider _settingsProvider;
    private readonly WorldClockService _service;

    public WorldClockServiceTests()
    {
        _settingsProvider = new FakeSettingsProvider();
        _service = new WorldClockService(_settingsProvider);
    }

    public void Dispose()
    {
        // No resources to clean up.
    }

    #region AddEntry

    [Fact]
    public void AddEntry_NewTimezone_AddsSuccessfully()
    {
        _service.AddEntry("America/New_York");

        IReadOnlyList<WorldClockEntry> entries = _service.GetEntries();
        entries.Should().HaveCount(1);
        entries[0].TimeZoneId.Should().Be("America/New_York");
    }

    [Fact]
    public void AddEntry_WithCustomName_StoresCustomName()
    {
        _service.AddEntry("America/New_York", "Boss");

        IReadOnlyList<WorldClockEntry> entries = _service.GetEntries();
        entries[0].CustomDisplayName.Should().Be("Boss");
        entries[0].DisplayName.Should().Be("Boss");
    }

    [Fact]
    public void AddEntry_WhitespaceCustomName_NormalizedToNull()
    {
        _service.AddEntry("America/New_York", "   ");

        IReadOnlyList<WorldClockEntry> entries = _service.GetEntries();
        entries[0].CustomDisplayName.Should().BeNull();
        entries[0].DisplayName.Should().Be("New York");
    }

    [Fact]
    public void AddEntry_DuplicateTimezone_IsIgnored()
    {
        _service.AddEntry("America/New_York");
        _service.AddEntry("America/New_York");

        _service.GetEntries().Should().HaveCount(1);
    }

    [Fact]
    public void AddEntry_MultipleTimezones_PreservesOrder()
    {
        _service.AddEntry("America/New_York");
        _service.AddEntry("Europe/Paris");
        _service.AddEntry("Asia/Tokyo");

        IReadOnlyList<WorldClockEntry> entries = _service.GetEntries();
        entries.Should().HaveCount(3);
        entries[0].TimeZoneId.Should().Be("America/New_York");
        entries[1].TimeZoneId.Should().Be("Europe/Paris");
        entries[2].TimeZoneId.Should().Be("Asia/Tokyo");
    }

    #endregion

    #region RemoveEntry

    [Fact]
    public void RemoveEntry_ExistingEntry_Removes()
    {
        _service.AddEntry("America/New_York");
        _service.AddEntry("Europe/Paris");

        _service.RemoveEntry("America/New_York");

        IReadOnlyList<WorldClockEntry> entries = _service.GetEntries();
        entries.Should().HaveCount(1);
        entries[0].TimeZoneId.Should().Be("Europe/Paris");
    }

    [Fact]
    public void RemoveEntry_NonExistent_IsNoOp()
    {
        _service.AddEntry("America/New_York");

        _service.RemoveEntry("Europe/Paris");

        _service.GetEntries().Should().HaveCount(1);
    }

    #endregion

    #region UpdateDisplayName

    [Fact]
    public void UpdateDisplayName_SetsCustomName()
    {
        _service.AddEntry("America/New_York");

        _service.UpdateDisplayName("America/New_York", "My City");

        _service.GetEntries()[0].CustomDisplayName.Should().Be("My City");
    }

    [Fact]
    public void UpdateDisplayName_WhitespaceClears_ToNull()
    {
        _service.AddEntry("America/New_York", "Boss");

        _service.UpdateDisplayName("America/New_York", "  ");

        _service.GetEntries()[0].CustomDisplayName.Should().BeNull();
    }

    [Fact]
    public void UpdateDisplayName_Null_ClearsCustomName()
    {
        _service.AddEntry("America/New_York", "Boss");

        _service.UpdateDisplayName("America/New_York", null);

        _service.GetEntries()[0].CustomDisplayName.Should().BeNull();
        _service.GetEntries()[0].DisplayName.Should().Be("New York");
    }

    #endregion

    #region ReorderEntries

    [Fact]
    public void ReorderEntries_ReordersToMatchIds()
    {
        _service.AddEntry("America/New_York");
        _service.AddEntry("Europe/Paris");
        _service.AddEntry("Asia/Tokyo");

        _service.ReorderEntries(["Asia/Tokyo", "America/New_York", "Europe/Paris"]);

        IReadOnlyList<WorldClockEntry> entries = _service.GetEntries();
        entries[0].TimeZoneId.Should().Be("Asia/Tokyo");
        entries[1].TimeZoneId.Should().Be("America/New_York");
        entries[2].TimeZoneId.Should().Be("Europe/Paris");
    }

    [Fact]
    public void ReorderEntries_PreservesCustomNames()
    {
        _service.AddEntry("America/New_York", "Boss");
        _service.AddEntry("Europe/Paris", "Mom");

        _service.ReorderEntries(["Europe/Paris", "America/New_York"]);

        IReadOnlyList<WorldClockEntry> entries = _service.GetEntries();
        entries[0].CustomDisplayName.Should().Be("Mom");
        entries[1].CustomDisplayName.Should().Be("Boss");
    }

    #endregion

    #region Persistence

    [Fact]
    public void Persistence_RoundTrip_PreservesData()
    {
        _service.AddEntry("America/New_York", "Boss");
        _service.AddEntry("Europe/Paris");
        _service.AddEntry("Asia/Tokyo", "Tokyo Office");

        // Create a new service instance reading from the same settings.
        var service2 = new WorldClockService(_settingsProvider);
        IReadOnlyList<WorldClockEntry> entries = service2.GetEntries();

        entries.Should().HaveCount(3);
        entries[0].TimeZoneId.Should().Be("America/New_York");
        entries[0].CustomDisplayName.Should().Be("Boss");
        entries[1].TimeZoneId.Should().Be("Europe/Paris");
        entries[1].CustomDisplayName.Should().BeNull();
        entries[2].TimeZoneId.Should().Be("Asia/Tokyo");
        entries[2].CustomDisplayName.Should().Be("Tokyo Office");
    }

    [Fact]
    public void Persistence_CorruptJson_LoadsEmpty()
    {
        _settingsProvider.SetSetting(
            WindowSill.Date.Settings.Settings.WorldClockEntries,
            "{corrupt json!!!");

        var service = new WorldClockService(_settingsProvider);

        service.GetEntries().Should().BeEmpty();
    }

    #endregion

    #region SearchCities

    [Fact]
    public void SearchCities_ShortQuery_ReturnsEmpty()
    {
        _service.SearchCities("N").Should().BeEmpty();
    }

    [Fact]
    public void SearchCities_BlankQuery_ReturnsEmpty()
    {
        _service.SearchCities("").Should().BeEmpty();
        _service.SearchCities("  ").Should().BeEmpty();
    }

    [Fact]
    public void SearchCities_ValidQuery_ReturnsResults()
    {
        IReadOnlyList<CitySearchResult> results = _service.SearchCities("Paris");

        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.CityName == "Paris");
    }

    [Fact]
    public void SearchCities_ExcludesAlreadyConfigured()
    {
        _service.AddEntry("Europe/Paris");

        IReadOnlyList<CitySearchResult> results = _service.SearchCities("Paris");

        results.Should().NotContain(r => r.TimeZoneId == "Europe/Paris");
    }

    [Fact]
    public void SearchCities_MatchesByCountry()
    {
        IReadOnlyList<CitySearchResult> results = _service.SearchCities("France");

        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.CountryName == "France");
    }

    #endregion

    #region GetTimeZone

    [Fact]
    public void GetTimeZone_ValidId_ReturnsZone()
    {
        NodaTime.DateTimeZone zone = _service.GetTimeZone("America/New_York");

        zone.Id.Should().Be("America/New_York");
    }

    [Fact]
    public void GetTimeZone_InvalidId_ReturnsUtc()
    {
        NodaTime.DateTimeZone zone = _service.GetTimeZone("Invalid/Zone");

        zone.Id.Should().Be("UTC");
    }

    #endregion

    #region EntriesChanged event

    [Fact]
    public void AddEntry_RaisesEntriesChanged()
    {
        bool raised = false;
        _service.EntriesChanged += (_, _) => raised = true;

        _service.AddEntry("America/New_York");

        raised.Should().BeTrue();
    }

    [Fact]
    public void RemoveEntry_RaisesEntriesChanged()
    {
        _service.AddEntry("America/New_York");

        bool raised = false;
        _service.EntriesChanged += (_, _) => raised = true;
        _service.RemoveEntry("America/New_York");

        raised.Should().BeTrue();
    }

    #endregion
}
