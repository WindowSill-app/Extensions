using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core.Fakes;

/// <summary>
/// A fake <see cref="ICalendarProvider"/> for unit testing.
/// </summary>
internal sealed class FakeCalendarProvider : ICalendarProvider
{
    private readonly Func<CalendarAccount, Func<IReadOnlyDictionary<string, string>, CancellationToken, Task>, ICalendarAccountClient>? _clientFactory;

    public FakeCalendarProvider(
        CalendarProviderType providerType,
        Func<CalendarAccount, Func<IReadOnlyDictionary<string, string>, CancellationToken, Task>, ICalendarAccountClient>? clientFactory = null)
    {
        ProviderType = providerType;
        _clientFactory = clientFactory;
    }

    /// <inheritdoc />
    public CalendarProviderType ProviderType { get; }

    /// <inheritdoc />
    public string DisplayName => ProviderType.ToString();

    /// <inheritdoc />
    public string IconAssetFileName => $"{ProviderType.ToString().ToLowerInvariant()}.svg";

    /// <inheritdoc />
    public ConnectExperience CreateConnectExperience()
    {
        throw new NotSupportedException("Connect experience is not available in tests.");
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        if (_clientFactory is not null)
        {
            return _clientFactory(account, onAuthDataChanged);
        }

        return new FakeCalendarAccountClient(account);
    }
}
