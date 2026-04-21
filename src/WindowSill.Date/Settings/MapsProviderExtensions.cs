namespace WindowSill.Date.Settings;

/// <summary>
/// Extension methods for <see cref="MapsProvider"/>.
/// </summary>
internal static class MapsProviderExtensions
{
    /// <summary>
    /// Builds a web URL for directions to the given address.
    /// </summary>
    /// <param name="provider">The maps provider.</param>
    /// <param name="destinationAddress">The destination address text.</param>
    /// <returns>A URL that opens in the default browser.</returns>
    public static Uri BuildDirectionsUrl(this MapsProvider provider, string destinationAddress)
    {
        string encoded = Uri.EscapeDataString(destinationAddress);

        return provider switch
        {
            MapsProvider.GoogleMaps => new Uri($"https://www.google.com/maps/dir/?api=1&destination={encoded}"),
            MapsProvider.BingMaps => new Uri($"https://www.bing.com/maps?rtp=~adr.{encoded}"),
            MapsProvider.AppleMaps => new Uri($"https://maps.apple.com/?daddr={encoded}"),
            _ => new Uri($"https://www.google.com/maps/dir/?api=1&destination={encoded}"),
        };
    }
}
