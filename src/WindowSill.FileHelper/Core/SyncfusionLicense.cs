using Syncfusion.Licensing;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Registers the Syncfusion Community license once per process so DocIO rendering produces watermark-free PDFs.
/// </summary>
/// <remarks>
/// The key is resolved from (1) the value baked in at build time (CI injects the <c>SYNCFUSION_LICENSE_KEY</c> GitHub
/// Actions secret; empty on contributor builds — see <c>SyncfusionLicense.targets</c>), falling back to (2) the
/// <c>SYNCFUSION_LICENSE_KEY</c> environment variable so a developer can test a local/VM build without a CI pipeline.
/// When no key is available Syncfusion runs in trial mode (a small watermark on the output) — the conversion itself
/// still works, so contributor builds are unaffected.
/// </remarks>
internal static class SyncfusionLicense
{
    private static readonly Lock s_gate = new();
    private static bool s_registered;

    /// <summary>
    /// The first time it is called, disables Syncfusion telemetry and registers the license; subsequent calls are
    /// cheap no-ops. Safe to call before any Syncfusion API is used.
    /// </summary>
    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        lock (s_gate)
        {
            if (s_registered)
            {
                return;
            }

            s_registered = true;

            // Disable Syncfusion's usage telemetry so the extension never sends data to a third party. Must run
            // before any Syncfusion API is exercised.
            Syncfusion.Telemetry.Telemetry.Disable();

            string? licenseKey = ResolveLicenseKey();
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
        }
    }

    private static string? ResolveLicenseKey()
    {
        // 1. Build-time value (CI bakes in the GitHub secret; empty string on contributor builds).
        string embedded = SyncfusionLicenseValues.LicenseKey;
        if (!string.IsNullOrWhiteSpace(embedded))
        {
            return embedded;
        }

        // 2. Runtime environment variable, for local/VM testing without a CI build.
        return Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
    }
}
