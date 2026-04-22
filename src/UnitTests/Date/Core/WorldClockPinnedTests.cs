using FluentAssertions;
using UnitTests.Date.Core.Fakes;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;

namespace UnitTests.Date.Core;

public class WorldClockPinnedTests : IDisposable
{
    private readonly FakeSettingsProvider _settingsProvider;
    private readonly WorldClockService _service;

    public WorldClockPinnedTests()
    {
        _settingsProvider = new FakeSettingsProvider();
        _service = new WorldClockService(_settingsProvider);
    }

    public void Dispose()
    {
    }

    [Fact]
    public void GetPinnedEntries_NoPinned_ReturnsEmpty()
    {
        _service.AddEntry("America/New_York");
        _service.AddEntry("Europe/London");

        _service.GetPinnedEntries().Should().BeEmpty();
    }

    [Fact]
    public void SetShowInBar_PinsEntry()
    {
        _service.AddEntry("America/New_York");

        _service.SetShowInBar("America/New_York", true);

        IReadOnlyList<WorldClockEntry> pinned = _service.GetPinnedEntries();
        pinned.Should().HaveCount(1);
        pinned[0].TimeZoneId.Should().Be("America/New_York");
    }

    [Fact]
    public void SetShowInBar_UnpinsEntry()
    {
        _service.AddEntry("America/New_York");
        _service.SetShowInBar("America/New_York", true);

        _service.SetShowInBar("America/New_York", false);

        _service.GetPinnedEntries().Should().BeEmpty();
    }

    [Fact]
    public void SetShowInBar_FiresEntriesChanged()
    {
        _service.AddEntry("America/New_York");
        bool fired = false;
        _service.EntriesChanged += (_, _) => fired = true;

        _service.SetShowInBar("America/New_York", true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void SetShowInBar_SameValue_DoesNotFireEvent()
    {
        _service.AddEntry("America/New_York");
        bool fired = false;
        _service.EntriesChanged += (_, _) => fired = true;

        _service.SetShowInBar("America/New_York", false); // Already false.

        fired.Should().BeFalse();
    }

    [Fact]
    public void SetShowInBar_NonexistentEntry_DoesNotThrow()
    {
        Action act = () => _service.SetShowInBar("Nonexistent/Zone", true);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetPinnedEntries_MixedPinState_ReturnsOnlyPinned()
    {
        _service.AddEntry("America/New_York");
        _service.AddEntry("Europe/London");
        _service.AddEntry("Asia/Tokyo");

        _service.SetShowInBar("America/New_York", true);
        _service.SetShowInBar("Asia/Tokyo", true);

        IReadOnlyList<WorldClockEntry> pinned = _service.GetPinnedEntries();
        pinned.Should().HaveCount(2);
        pinned.Select(e => e.TimeZoneId).Should().Contain("America/New_York");
        pinned.Select(e => e.TimeZoneId).Should().Contain("Asia/Tokyo");
    }

    [Fact]
    public void ShowInBar_SurvivesRoundtrip()
    {
        _service.AddEntry("America/New_York");
        _service.SetShowInBar("America/New_York", true);

        // Create a new service reading from the same settings (simulates restart).
        var service2 = new WorldClockService(_settingsProvider);

        service2.GetPinnedEntries().Should().HaveCount(1);
        service2.GetPinnedEntries()[0].TimeZoneId.Should().Be("America/New_York");
    }
}
