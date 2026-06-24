using System.Text;
using FluentAssertions;
using WindowSill.ClipboardHistory;
using WindowSill.ClipboardHistory.Core;
using Path = System.IO.Path;

namespace UnitTests.ClipboardHistory;

public class PinnedClipboardStoreTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly PinnedClipboardStore _store;

    public PinnedClipboardStoreTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), $"WindowSillPinTests_{Guid.NewGuid():N}");
        _store = new PinnedClipboardStore(_tempFolder);
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
        List<PinnedClipboardItem> items = await _store.LoadAllAsync();

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAllAsync_RoundTripsTextItem()
    {
        var item = new PinnedClipboardItem
        {
            Id = "pin1",
            DataType = DetectedClipboardDataType.Text,
            Text = "hello@example.com",
            ContentSignature = "sig-1",
        };

        await _store.SaveAsync(item);
        List<PinnedClipboardItem> loaded = await _store.LoadAllAsync();

        loaded.Should().HaveCount(1);
        loaded[0].Id.Should().Be("pin1");
        loaded[0].DataType.Should().Be(DetectedClipboardDataType.Text);
        loaded[0].Text.Should().Be("hello@example.com");
        loaded[0].ContentSignature.Should().Be("sig-1");
        loaded[0].HasImage.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithImage_RoundTripsImageBytes()
    {
        byte[] imageBytes = [1, 2, 3, 4, 5, 250, 128, 64];
        var item = new PinnedClipboardItem
        {
            Id = "pinImage",
            DataType = DetectedClipboardDataType.Image,
            HasImage = true,
            ImageBytes = imageBytes,
            ContentSignature = "img-sig",
        };

        await _store.SaveAsync(item);
        List<PinnedClipboardItem> loaded = await _store.LoadAllAsync();

        loaded.Should().HaveCount(1);
        loaded[0].HasImage.Should().BeTrue();
        loaded[0].ImageBytes.Should().Equal(imageBytes);
    }

    [Fact]
    public async Task SaveAsync_EncryptsContentOnDisk()
    {
        const string secret = "super-secret-clipboard-value";
        var item = new PinnedClipboardItem
        {
            Id = "pinSecret",
            DataType = DetectedClipboardDataType.Text,
            Text = secret,
            ContentSignature = "sig",
        };

        await _store.SaveAsync(item);

        string dataFile = Directory.GetFiles(Path.Combine(_tempFolder, "Pinned"), "*.dat").Single();
        byte[] raw = await File.ReadAllBytesAsync(dataFile);
        string asText = Encoding.UTF8.GetString(raw);

        asText.Should().NotContain(secret, "the payload must be encrypted at rest");
    }

    [Fact]
    public async Task DeleteAsync_RemovesDataAndImageFiles()
    {
        var item = new PinnedClipboardItem
        {
            Id = "pinDelete",
            DataType = DetectedClipboardDataType.Image,
            HasImage = true,
            ImageBytes = [9, 9, 9],
            ContentSignature = "sig",
        };

        await _store.SaveAsync(item);
        await _store.DeleteAsync("pinDelete");

        List<PinnedClipboardItem> loaded = await _store.LoadAllAsync();
        loaded.Should().BeEmpty();
        Directory.GetFiles(Path.Combine(_tempFolder, "Pinned")).Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAllAsync_OrdersByPinnedAtAscending()
    {
        var older = new PinnedClipboardItem
        {
            Id = "older",
            DataType = DetectedClipboardDataType.Text,
            Text = "a",
            ContentSignature = "a",
            PinnedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };
        var newer = new PinnedClipboardItem
        {
            Id = "newer",
            DataType = DetectedClipboardDataType.Text,
            Text = "b",
            ContentSignature = "b",
            PinnedAt = DateTimeOffset.UtcNow,
        };

        await _store.SaveAsync(newer);
        await _store.SaveAsync(older);

        List<PinnedClipboardItem> loaded = await _store.LoadAllAsync();

        loaded.Select(i => i.Id).Should().ContainInOrder("older", "newer");
    }

    [Fact]
    public async Task LoadAllAsync_SkipsCorruptFiles()
    {
        var item = new PinnedClipboardItem
        {
            Id = "good",
            DataType = DetectedClipboardDataType.Text,
            Text = "ok",
            ContentSignature = "sig",
        };
        await _store.SaveAsync(item);

        await File.WriteAllTextAsync(Path.Combine(_tempFolder, "Pinned", "garbage.dat"), "not encrypted json");

        List<PinnedClipboardItem> loaded = await _store.LoadAllAsync();

        loaded.Should().ContainSingle(i => i.Id == "good");
    }
}
