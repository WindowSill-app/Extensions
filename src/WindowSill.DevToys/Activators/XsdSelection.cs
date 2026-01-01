using System.ComponentModel.Composition;
using System.Xml.Schema;
using WindowSill.API;

namespace WindowSill.DevToys.Activators;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivationTypeName, XmlSelection.ActivationTypeName)]
internal sealed partial class XsdSelection : ISillTextSelectionActivator
{
    internal const string ActivationTypeName = "xsdSelection";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(IsXsd(selectedText));
    }

    /// <summary>
    /// Detects whether the given string is a valid XSD or not.
    /// </summary>
    private static bool IsXsd(string? input)
    {
        try
        {
            using StringReader reader = new(input!);
            var xmlSchema = XmlSchema.Read(reader, null);
            return xmlSchema is not null;
        }
        catch 
        {
            return false;
        }
    }
}
