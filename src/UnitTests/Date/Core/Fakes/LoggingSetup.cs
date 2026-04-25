using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Date.Core.Fakes;

/// <summary>
/// Sets up a no-op <see cref="ILoggerFactory"/> on the WindowSill.API LoggingExtensions
/// so that <c>this.Log()</c> works in unit tests without the host application.
/// </summary>
internal static class LoggingSetup
{
    private static bool _initialized;

    /// <summary>
    /// Ensures the logger factory is initialized. Safe to call multiple times.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        Type loggingExtensions = typeof(WindowSill.API.LoggingExtensions);
        PropertyInfo? prop = loggingExtensions.GetProperty(
            "LoggerFactory",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        prop?.SetValue(null, NullLoggerFactory.Instance);
        _initialized = true;
    }
}
