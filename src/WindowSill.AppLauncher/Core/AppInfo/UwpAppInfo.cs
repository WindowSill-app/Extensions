using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Management.Deployment;
using Windows.Storage.Streams;
using WindowSill.API;

namespace WindowSill.AppLauncher.Core.AppInfo;

internal sealed class UwpAppInfo : AppInfo, IEquatable<UwpAppInfo>
{
    [JsonPropertyName("app_user_model_id")]
    public required string AppUserModelId { get; init; }

    [JsonPropertyName("package_family_name")]
    public required string PackageFullName { get; init; }

    [JsonIgnore]
    internal Package? Package { get; set; }

    public override bool Equals(object? obj)
    {
        return (obj is UwpAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(UwpAppInfo? other)
    {
        return base.Equals(other) && PackageFullName == other?.PackageFullName && AppUserModelId == other?.AppUserModelId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), PackageFullName, AppUserModelId);
    }

    public override void OnDeserialized()
    {
        if (Package is null)
        {
            var packageManager = new PackageManager();
            Package
                = packageManager.FindPackageForUser(
                    userSecurityId: null,
                    packageFullName: PackageFullName);
            if (Package is null)
            {
                IEnumerable<Package> packages = packageManager.FindPackagesForUser("");
                foreach (Package package in packages)
                {
                    foreach (AppListEntry appListEntry in package.GetAppListEntries())
                    {
                        if (appListEntry.AppUserModelId == AppUserModelId)
                        {
                            Package = package;
                            break;
                        }
                    }

                    if (Package is not null)
                    {
                        break;
                    }
                }
            }
        }

        if (Package is not null && string.IsNullOrEmpty(OverrideAppIconPath))
        {
            AppIcon = new TaskCompletionNotifier<ImageSource?>(GetUwpAppIconAsync, runTaskImmediately: false);
        }
        else
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new UwpAppInfo
        {
            DefaultDisplayName = this.DefaultDisplayName,
            DisplayName = this.DisplayName,
            OverrideAppIconPath = this.OverrideAppIconPath,
            AppUserModelId = this.AppUserModelId,
            PackageFullName = this.PackageFullName,
            Package = this.Package,
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }

    /// <summary>
    /// Retrieve the icon of a UWP app
    /// </summary>
    /// <param name="package">The UWP package</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task<ImageSource?> GetUwpAppIconAsync()
    {
        return await ThreadHelper.RunOnUIThreadAsync(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            async () =>
            {
                try
                {
                    Guard.IsNotNull(Package);
                    RandomAccessStreamReference appIcon
                        = Package.GetLogoAsRandomAccessStreamReference(
                            new Windows.Foundation.Size(
                                IconHelper.DefaultIconSize * 2,
                                IconHelper.DefaultIconSize * 2));

                    using IRandomAccessStreamWithContentType appIconStream = await appIcon.OpenReadAsync();

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(appIconStream);

                    using SoftwareBitmap softwareBitmap
                        = await decoder.GetSoftwareBitmapAsync(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied);

                    // Create WriteableBitmap instead of BitmapImage
                    var writeableBitmap = new WriteableBitmap(
                        softwareBitmap.PixelWidth,
                        softwareBitmap.PixelHeight);

                    // Copy pixel data from SoftwareBitmap to WriteableBitmap
                    softwareBitmap.CopyToBuffer(writeableBitmap.PixelBuffer);

                    return writeableBitmap;
                }
                catch (Exception ex)
                {
                    typeof(UwpAppInfo).Log().LogWarning(ex, "Failed to extract UWP app icon for: {Path}", PackageFullName);
                    return null;
                }
            });
    }

    public override async ValueTask LaunchAsync(bool asAdmin)
    {
        Guard.IsNotNull(Package, nameof(Package));

        try
        {
            IEnumerable<AppListEntry> appListEntries = Package.GetAppListEntries();

            // Find the matching app list entry by AppUserModelId
            AppListEntry? targetEntry = null;
            foreach (AppListEntry entry in appListEntries)
            {
                if (entry.AppUserModelId == AppUserModelId)
                {
                    targetEntry = entry;
                    break;
                }
            }

            if (targetEntry != null)
            {
                bool success = await targetEntry.LaunchAsync();
                if (!success)
                {
                    typeof(UwpAppInfo).Log().LogWarning(
                        "Failed to launch UWP app: {AppUserModelId}",
                        AppUserModelId);
                }
            }
            else
            {
                typeof(UwpAppInfo).Log().LogWarning(
                    "Could not find app list entry for AppUserModelId: {AppUserModelId}",
                    AppUserModelId);
            }
        }
        catch (Exception ex)
        {
            typeof(UwpAppInfo).Log().LogError(
                ex,
                "Exception while launching UWP app: {AppUserModelId}",
                AppUserModelId);
            throw;
        }
    }

    public override void OpenLocation()
    {
        throw new NotImplementedException();
    }
}
