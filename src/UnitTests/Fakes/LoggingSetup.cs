using System.Reflection;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Fakes;

/// <summary>
/// Configures WindowSill.API logging for unit tests.
/// </summary>
internal static class LoggingSetup
{
    private static readonly Lazy<bool> s_initialization = new(
        Initialize,
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Ensures the no-op logger factory is configured exactly once.
    /// </summary>
    public static void EnsureInitialized()
    {
        _ = s_initialization.Value;
    }

    private static bool Initialize()
    {
        Type loggingExtensions = typeof(WindowSill.API.LoggingExtensions);
        PropertyInfo loggerFactoryProperty = loggingExtensions.GetProperty(
            "LoggerFactory",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MissingMemberException(loggingExtensions.FullName, "LoggerFactory");

        loggerFactoryProperty.SetValue(null, NullLoggerFactory.Instance);
        return true;
    }
}
