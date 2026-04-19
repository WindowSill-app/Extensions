using FluentAssertions;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using Path = System.IO.Path;

namespace UnitTests.Date.Core;

public class CalendarDataStoreTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly CalendarDataStore _store;

    public CalendarDataStoreTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), $"WindowSillTests_{Guid.NewGuid():N}");
        _store = new CalendarDataStore(_tempFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
        {
            Directory.Delete(_tempFolder, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAllAsync_EmptyFolder_ReturnsEmpty()
    {
        CalendarAccount[] accounts = await _store.LoadAllAsync();

        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAllAsync_RoundTrips()
    {
        CalendarAccount account = CreateAccount("test_1", "Test User", "test@example.com", CalendarProviderType.Outlook);

        await _store.SaveAsync(account);
        CalendarAccount[] loaded = await _store.LoadAllAsync();

        loaded.Should().HaveCount(1);
        loaded[0].Id.Should().Be("test_1");
        loaded[0].DisplayName.Should().Be("Test User");
        loaded[0].Email.Should().Be("test@example.com");
        loaded[0].ProviderType.Should().Be(CalendarProviderType.Outlook);
    }

    [Fact]
    public async Task SaveAsync_WithAuthData_PreservesAuthData()
    {
        CalendarAccount account = CreateAccount("outlook_john", "John", "john@contoso.com", CalendarProviderType.Outlook,
            new Dictionary<string, string> { ["msal_cache"] = "base64data", ["extra"] = "value" });

        await _store.SaveAsync(account);
        CalendarAccount[] loaded = await _store.LoadAllAsync();

        loaded.Should().HaveCount(1);
        loaded[0].AuthData.Should().ContainKey("msal_cache").WhoseValue.Should().Be("base64data");
        loaded[0].AuthData.Should().ContainKey("extra").WhoseValue.Should().Be("value");
    }

    [Fact]
    public async Task SaveAsync_MultipleAccounts_LoadsAll()
    {
        await _store.SaveAsync(CreateAccount("outlook_a", "A", "a@test.com", CalendarProviderType.Outlook));
        await _store.SaveAsync(CreateAccount("google_b", "B", "b@test.com", CalendarProviderType.Google));
        await _store.SaveAsync(CreateAccount("caldav_c", "C", "c@test.com", CalendarProviderType.CalDav));

        CalendarAccount[] loaded = await _store.LoadAllAsync();

        loaded.Should().HaveCount(3);
        loaded.Select(a => a.Id).Should().BeEquivalentTo(["outlook_a", "google_b", "caldav_c"]);
    }

    [Fact]
    public async Task SaveAsync_OverwritesExisting()
    {
        CalendarAccount original = CreateAccount("test_1", "Original", "orig@test.com", CalendarProviderType.Google);
        await _store.SaveAsync(original);

        CalendarAccount updated = CreateAccount("test_1", "Updated", "updated@test.com", CalendarProviderType.Google,
            new Dictionary<string, string> { ["token"] = "new_value" });
        await _store.SaveAsync(updated);

        CalendarAccount[] loaded = await _store.LoadAllAsync();
        loaded.Should().HaveCount(1);
        loaded[0].DisplayName.Should().Be("Updated");
        loaded[0].Email.Should().Be("updated@test.com");
        loaded[0].AuthData.Should().ContainKey("token").WhoseValue.Should().Be("new_value");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccount()
    {
        await _store.SaveAsync(CreateAccount("to_delete", "Delete Me", "del@test.com", CalendarProviderType.CalDav));
        await _store.SaveAsync(CreateAccount("to_keep", "Keep Me", "keep@test.com", CalendarProviderType.Google));

        await _store.DeleteAsync("to_delete");

        CalendarAccount[] loaded = await _store.LoadAllAsync();
        loaded.Should().HaveCount(1);
        loaded[0].Id.Should().Be("to_keep");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentAccount_DoesNotThrow()
    {
        Func<Task> act = () => _store.DeleteAsync("nonexistent");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadAllAsync_CorruptFile_SkipsIt()
    {
        // Save a valid account.
        await _store.SaveAsync(CreateAccount("valid", "Valid", "valid@test.com", CalendarProviderType.Outlook));

        // Write a corrupt file.
        string corruptPath = Path.Combine(_tempFolder, "corrupt.dat");
        await File.WriteAllBytesAsync(corruptPath, [0xFF, 0xFE, 0x00, 0x01]);

        CalendarAccount[] loaded = await _store.LoadAllAsync();

        loaded.Should().HaveCount(1);
        loaded[0].Id.Should().Be("valid");
    }

    [Fact]
    public async Task SaveAsync_FileIsEncrypted()
    {
        await _store.SaveAsync(CreateAccount("encrypted_test", "Test", "test@test.com", CalendarProviderType.Outlook));

        string[] files = Directory.GetFiles(_tempFolder, "*.dat");
        files.Should().HaveCount(1);

        // The raw file should not contain plaintext.
        string rawContent = await File.ReadAllTextAsync(files[0]);
        rawContent.Should().NotContain("encrypted_test");
        rawContent.Should().NotContain("test@test.com");
    }

    [Fact]
    public async Task DeleteAsync_CleansUpTempFiles()
    {
        await _store.SaveAsync(CreateAccount("temp_test", "Test", "test@test.com", CalendarProviderType.Google));

        // Simulate a leftover .tmp file.
        string[] datFiles = Directory.GetFiles(_tempFolder, "*.dat");
        string tmpPath = datFiles[0] + ".tmp";
        await File.WriteAllTextAsync(tmpPath, "leftover");

        await _store.DeleteAsync("temp_test");

        Directory.GetFiles(_tempFolder).Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithHiddenCalendarIds_PreservesHiddenCalendarIds()
    {
        CalendarAccount account = new()
        {
            Id = "hidden_test",
            DisplayName = "Test",
            Email = "test@test.com",
            ProviderType = CalendarProviderType.Outlook,
            HiddenCalendarIds = ["cal_1", "cal_2"],
        };

        await _store.SaveAsync(account);
        CalendarAccount[] loaded = await _store.LoadAllAsync();

        loaded.Should().HaveCount(1);
        loaded[0].HiddenCalendarIds.Should().BeEquivalentTo(["cal_1", "cal_2"]);
    }

    private static CalendarAccount CreateAccount(
        string id, string displayName, string email, CalendarProviderType providerType,
        Dictionary<string, string>? authData = null)
    {
        return new CalendarAccount
        {
            Id = id,
            DisplayName = displayName,
            Email = email,
            ProviderType = providerType,
            AuthData = authData ?? new Dictionary<string, string>(),
        };
    }
}
