using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;
using WindowSill.API;

namespace WindowSill.AppLauncher.Core.AppInfo;

internal sealed class UrlAppInfo : AppInfo, IJsonOnDeserialized, IEquatable<UrlAppInfo>
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    public override bool Equals(object? obj)
    {
        return (obj is UrlAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(UrlAppInfo? other)
    {
        return base.Equals(other) && Url == other?.Url;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Url);
    }

    public override void OnDeserialized()
    {
        if (!string.IsNullOrEmpty(OverrideAppIconPath))
        {
            base.OnDeserialized();
            return;
        }

        if (Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            AppIcon = new TaskCompletionNotifier<ImageSource?>(() => GetFaviconAsync(Url), runTaskImmediately: false);
        }
        else
        {
            AppIcon = new TaskCompletionNotifier<ImageSource?>(() => Task.FromResult<ImageSource?>(null), runTaskImmediately: false);
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new UrlAppInfo
        {
            DefaultDisplayName = this.DefaultDisplayName,
            DisplayName = this.DisplayName,
            OverrideAppIconPath = this.OverrideAppIconPath,
            Url = this.Url,
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }

    public override ValueTask LaunchAsync(bool asAdmin)
    {
        if (Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri))
        {
            Launcher.LaunchUriAsync(uri).AsTask().ForgetSafely();
        }

        return ValueTask.CompletedTask;
    }

    public override void OpenLocation()
    {
        // For URLs, opening location means launching the URL itself
        LaunchAsync(asAdmin: false).AsTask().Forget();
    }

    private static async Task<ImageSource?> GetFaviconAsync(string url)
    {
        try
        {
            string faviconUrl = string.Format("https://www.google.com/s2/favicons?domain={0}&sz={1}", Uri.EscapeDataString(url), 32);
            return new BitmapImage(new Uri(faviconUrl));
        }
        catch
        {
            return null;
        }
    }
}
