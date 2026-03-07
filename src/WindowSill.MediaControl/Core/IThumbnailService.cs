using WindowSill.API;

namespace WindowSill.MediaControl.Core;

/// <summary>
/// Decodes and scales media thumbnail streams into display-ready image sources.
/// </summary>
internal interface IThumbnailService
{
    /// <summary>
    /// Creates scaled thumbnail image sources from a raw thumbnail stream.
    /// </summary>
    /// <param name="thumbnailStream">
    /// The raw thumbnail stream from the media session. This stream will be disposed by the caller.
    /// </param>
    /// <param name="sillLocation">
    /// The current sill location, used to determine the appropriate thumbnail size.
    /// </param>
    /// <returns>A tuple containing the small and large thumbnail image sources.</returns>
    Task<(ImageSource? Thumbnail, ImageSource? ThumbnailLarge)> CreateThumbnailsAsync(
        Stream? thumbnailStream,
        SillLocation sillLocation);
}
