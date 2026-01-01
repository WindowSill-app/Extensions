using System.ComponentModel.Composition;
using System.Diagnostics;

using WindowSill.API;

namespace WindowSill.WebBrowser.Activators;

[Export(typeof(ISillProcessActivator))]
[ActivationType("Brave")]
internal sealed class BraveActivator : ISillProcessActivator
{
    public ValueTask<bool> GetShouldBeActivatedAsync(string applicationIdentifier, Process process, Version? version, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(applicationIdentifier.EndsWith("brave.exe"));
    }
}
