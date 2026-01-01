using System.ComponentModel.Composition;
using System.Xml;
using WindowSill.API;

namespace WindowSill.DevToys.Activators;

[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivationTypeName, PredefinedActivationTypeNames.TextSelection)]
internal sealed partial class XmlSelection : ISillTextSelectionActivator
{
    internal const string ActivationTypeName = "xmlSelection";

    public ValueTask<bool> GetShouldBeActivatedAsync(string selectedText, bool isReadOnly, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(IsXml(selectedText));
    }

    /// <summary>
    /// Detects whether the given string is a valid Xml or not.
    /// </summary>
    private static bool IsXml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (!ValidateFirstAndLastCharOfXml(input))
        {
            return false;
        }

        try
        {
            var xmlDocument = new XmlDocument();

            // If loading failed, it's not valid Xml.
            xmlDocument.LoadXml(input);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate that the XML starts with "<" and ends with ">", ignoring whitespace
    /// </summary>
    private static bool ValidateFirstAndLastCharOfXml(string input)
    {
        for (int i = 0; i < input.Length; ++i)
        {
            if (!char.IsWhiteSpace(input[i]))
            {
                if (input[i] == '<')
                {
                    break;
                }
                return false;
            }
        }

        for (int i = input.Length - 1; i >= 0; --i)
        {
            if (!char.IsWhiteSpace(input[i]))
            {
                if (input[i] == '>')
                {
                    return true;
                }
                return false;
            }
        }

        return false;
    }
}
