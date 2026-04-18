using System.ComponentModel.Composition;
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
    private readonly ICalendarCredentialStore _credentialStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ICloudCalendarProvider"/> class.
    /// </summary>
    /// <param name="credentialStore">The credential store for persisting credentials.</param>
    internal ICloudCalendarProvider(ICalendarCredentialStore credentialStore)
        : base(credentialStore)
    {
        _credentialStore = credentialStore;
    }

    /// <inheritdoc />
    public override CalendarProviderType ProviderType => CalendarProviderType.ICloud;

    /// <inheritdoc />
    public override string DisplayName => "Apple iCloud";

    /// <inheritdoc />
    public override async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        // iCloud uses app-specific passwords for CalDAV access.
        // TODO: Prompt user for Apple ID and app-specific password via UI.
        // 1. User generates app-specific password at appleid.apple.com.
        // 2. Store Apple ID + app-specific password via _credentialStore.
        // 3. Store server URL (https://caldav.icloud.com) via _credentialStore.
        // 4. Validate by doing a PROPFIND on the principal URL.
        throw new NotImplementedException("iCloud account setup not yet implemented.");
    }

    /// <inheritdoc />
    public override ICalendarAccountClient CreateClient(CalendarAccount account)
    {
        return new ICloudCalendarAccountClient(account, _credentialStore);
    }
}
