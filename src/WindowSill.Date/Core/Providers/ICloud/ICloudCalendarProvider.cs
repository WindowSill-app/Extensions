using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Providers.CalDav;

namespace WindowSill.Date.Core.Providers.ICloud;

/// <summary>
/// Calendar provider for Apple iCloud Calendar, extending the CalDAV provider
/// with Apple-specific server endpoints and authentication.
/// </summary>
internal sealed class ICloudCalendarProvider : CalDavCalendarProvider
{
    /// <inheritdoc />
    public override CalendarProviderType ProviderType => CalendarProviderType.ICloud;

    /// <inheritdoc />
    public override string DisplayName => "Apple iCloud";

    /// <inheritdoc />
    public override async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        // TODO: Prompt user for Apple ID and app-specific password via UI.
        throw new NotImplementedException("iCloud account setup not yet implemented.");
    }

    /// <inheritdoc />
    public override ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new ICloudCalendarAccountClient(account, account.AuthData);
    }
}
