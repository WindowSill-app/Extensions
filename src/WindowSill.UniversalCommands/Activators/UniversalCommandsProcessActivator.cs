using System.ComponentModel.Composition;
using System.Diagnostics;
using WindowSill.API;
using WindowSill.UniversalCommands.Core;

namespace WindowSill.UniversalCommands.Activators;

/// <summary>
/// Process activator that matches against user-configured target application process names.
/// Returns <see langword="true"/> when the foreground process matches any action's
/// <see cref="Core.Models.QuickAction.TargetAppProcessName"/>.
/// </summary>
[Export(typeof(ISillProcessActivator))]
[ActivationType(UniversalCommandsProcessActivator.ActivatorType)]
internal sealed class UniversalCommandsProcessActivator : ISillProcessActivator
{
    internal const string ActivatorType = "UniversalCommandsProcessActivator";

    private readonly UniversalCommandsService _universalCommandsService;

    [ImportingConstructor]
    internal UniversalCommandsProcessActivator(UniversalCommandsService universalCommandsService)
    {
        _universalCommandsService = universalCommandsService;
    }

    public ValueTask<bool> GetShouldBeActivatedAsync(
        string applicationIdentifier,
        Process process,
        Version? version,
        CancellationToken cancellationToken)
    {
        // Check if any action targets this process.
        bool matches = _universalCommandsService.Commands.Any(action =>
            action.TargetAppProcessName is not null
            && (applicationIdentifier.EndsWith($"{action.TargetAppProcessName}.exe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(process.ProcessName, action.TargetAppProcessName, StringComparison.OrdinalIgnoreCase)));

        return ValueTask.FromResult(matches);
    }
}
