using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.FileHelper.Fakes;

/// <summary>
/// Sets up a no-op <see cref="ILoggerFactory"/> on the WindowSill.API LoggingExtensions
/// so that <c>this.Log()</c> works in unit tests without the host application.
/// </summary>
/// <remarks>
/// Mirrors <c>UnitTests.Date.Core.Fakes.LoggingSetup</c>, duplicated here (rather than shared) so that
/// <c>WindowSill.FileHelper</c>'s tests don't take an incidental dependency on the <c>WindowSill.Date</c> test
/// fixtures.
/// </remarks>
internal static class LoggingSetup
{
    private static bool s_initialized;

    /// <summary>
    /// Ensures the logger factory is initialized. Safe to call multiple times.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (s_initialized)
        {
            return;
        }

        Type loggingExtensions = typeof(WindowSill.API.LoggingExtensions);
        PropertyInfo? prop = loggingExtensions.GetProperty(
            "LoggerFactory",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        prop?.SetValue(null, NullLoggerFactory.Instance);
        s_initialized = true;
    }
}
