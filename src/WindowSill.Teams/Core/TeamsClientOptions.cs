namespace WindowSill.Teams.Core;

internal sealed class TeamsClientOptions
{
    private const int Port = 8124;
    private const string App = "WindowSill.Teams";
    private const string Device = "WindowSill.Teams";
    private const string Manufacturer = "Etienne Baudoux";
    private const string AppVersion = "1.0.0";

    /// <summary>
    /// Create new TeamsSocketOptions
    /// </summary>
    /// <param name="token">Teams token from previous connection</param>
    /// <exception cref="ArgumentNullException"></exception>
    internal TeamsClientOptions(string? token = null)
    {
        //if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(Token), "Token is required!");
        Token = token ?? Guid.NewGuid().ToString();
    }

    internal Uri SocketUri => new Uri($"ws://localhost:{Port}?token={Token}&protocol-version=2.0.0&manufacturer={Manufacturer}&device={Device}&app={App}&app-version={AppVersion}");

    /// <summary>
    /// Teams API Token, Settings -> Privacy -> Manage API
    /// </summary>
    /// <seealso href="https://support.microsoft.com/en-us/office/connect-third-party-devices-to-teams-aabca9f2-47bb-407f-9f9b-81a104a883d6"/>
    internal string Token { get; set; }
}
