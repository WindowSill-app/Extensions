using System.ComponentModel.Composition;
using System.Diagnostics;

using WindowSill.API;

namespace WindowSill.WebBrowser.Activators;

[Export(typeof(ISillProcessActivator))]
[ActivationType("Opera")]
internal sealed class OperaActivator : ISillProcessActivator
{
    public ValueTask<bool> GetShouldBeActivatedAsync(string applicationIdentifier, Process process, Version? version, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(applicationIdentifier.EndsWith("opera.exe"));
    }
}
