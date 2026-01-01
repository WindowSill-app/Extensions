using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using WindowSill.API;

namespace WindowSill.DevToys.Core;

internal static class XmlHelper
{
    private static readonly ILogger logger = typeof(XmlHelper).Log();

    /// <summary>
    /// Format a string to the specified Xml format.
    /// </summary>
    internal static string Format(string? input, Indentation indentationMode, bool newLineOnAttributes)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        StringBuilder stringBuilder = PooledStringBuilder.Instance.Get();

        try
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(input);

            if (xmlDocument.FirstChild == null)
            {
                return string.Empty;
            }

            var xmlWriterSettings = new XmlWriterSettings()
            {
                Async = true,
                OmitXmlDeclaration = xmlDocument.FirstChild.NodeType != XmlNodeType.XmlDeclaration,
                NewLineOnAttributes = newLineOnAttributes,
            };

            switch (indentationMode)
            {
                case Indentation.TwoSpaces:
                    xmlWriterSettings.Indent = true;
                    xmlWriterSettings.IndentChars = "  ";
                    break;
                case Indentation.FourSpaces:
                    xmlWriterSettings.Indent = true;
                    xmlWriterSettings.IndentChars = "    ";
                    break;
                case Indentation.OneTab:
                    xmlWriterSettings.Indent = true;
                    xmlWriterSettings.IndentChars = "\t";
                    break;
                case Indentation.Minified:
                    xmlWriterSettings.Indent = false;
                    break;
                default:
                    throw new NotSupportedException();
            }

            using (var xmlWriter = XmlWriter.Create(stringBuilder, xmlWriterSettings))
            {
                xmlDocument.Save(xmlWriter);
            }

            if (xmlDocument.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                Match match = Regex.Match(xmlDocument.FirstChild.InnerText, @"(?<=encoding\s*=\s*"")[^""]*", RegexOptions.None);
                if (match.Success)
                {
                    stringBuilder = stringBuilder.Replace("utf-16", match.Value);
                }
                else
                {
                    stringBuilder = stringBuilder.Replace("encoding=\"utf-16\"", "");
                }
            }

            return stringBuilder.ToString();
        }
        catch (XmlException ex)
        {
            return string.Empty;
        }
        catch (Exception ex) // some other exception
        {
            logger.LogError(ex, "Xml formatter. Indentation: {indentationMode}", indentationMode);
            return string.Empty;
        }
        finally
        {
            PooledStringBuilder.Instance.Return(stringBuilder);
        }
    }
}
