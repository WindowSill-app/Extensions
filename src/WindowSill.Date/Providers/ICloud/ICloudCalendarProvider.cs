using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Providers.CalDav;

namespace WindowSill.Date.Providers.ICloud;

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
    public override async Task<(CalendarAccount Account, Dictionary<string, string> AuthData)> ConnectAccountAsync(
        CancellationToken cancellationToken)
    {
        // iCloud uses app-specific passwords for CalDAV access.
        // TODO: Prompt user for Apple ID and app-specific password via UI.
        // Return authData with server_url=https://caldav.icloud.com, username, password.
        throw new NotImplementedException("iCloud account setup not yet implemented.");
    }

    /// <inheritdoc />
    public override ICalendarAccountClient CreateClient(
        CalendarAccount account,
        IReadOnlyDictionary<string, string> authData,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new ICloudCalendarAccountClient(account, authData);
    }
}
