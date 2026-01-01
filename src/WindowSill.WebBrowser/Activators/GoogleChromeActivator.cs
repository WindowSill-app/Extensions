using System.ComponentModel.Composition;
using System.Diagnostics;

using WindowSill.API;

namespace WindowSill.WebBrowser.Activators;

[Export(typeof(ISillProcessActivator))]
[ActivationType("Google Chrome")]
internal sealed class GoogleChromeActivator : ISillProcessActivator
{
    public ValueTask<bool> GetShouldBeActivatedAsync(string applicationIdentifier, Process process, Version? version, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(applicationIdentifier.EndsWith("chrome.exe"));
    }
}
