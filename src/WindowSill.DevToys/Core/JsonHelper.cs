using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowSill.API;

namespace WindowSill.DevToys.Core;

internal static partial class JsonHelper
{
    private static readonly ILogger logger = typeof(JsonHelper).Log();

    /// <summary>
    /// Wrapper class to preserve the original JsonElement type information during sorting
    /// </summary>
    private sealed class JsonElementWrapper
    {
        public JsonElement Element { get; }

        public JsonElementWrapper(JsonElement element)
        {
            Element = element;
        }
    }

    /// <summary>
    /// Format a string to the specified JSON format.
    /// </summary>
    internal static string Format(string? input, Indentation indentationMode)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        try
        {
            // Parse the JSON input
            using var document = JsonDocument.Parse(input);
            return SerializeJsonDocument(document, indentationMode);
        }
        catch (JsonException ex)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid JSON format '{indentationMode}'", indentationMode);
            return string.Empty;
        }
    }

    internal static string Sort(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(input);
            object sortedElement = SortJsonElement(document.RootElement);

            // Detect indentation from input
            Indentation indentationMode = DetectIndentation(input);

            return SerializeObject(sortedElement, indentationMode);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON format for sorting.");
            return string.Empty;
        }
    }

    private static string SerializeJsonDocument(JsonDocument document, Indentation indentationMode)
    {
        using var stream = new MemoryStream();
        JsonWriterOptions writerOptions = CreateJsonWriterOptions(indentationMode);

        using (var writer = new Utf8JsonWriter(stream, writerOptions))
        {
            document.RootElement.WriteTo(writer);
        }

        string jsonString = Encoding.UTF8.GetString(stream.ToArray());
        return ApplyCustomIndentation(jsonString, indentationMode);
    }

    private static string SerializeObject(object sortedElement, Indentation indentationMode)
    {
        using var stream = new MemoryStream();
        JsonWriterOptions writerOptions = CreateJsonWriterOptions(indentationMode);

        using (var writer = new Utf8JsonWriter(stream, writerOptions))
        {
            WriteJsonElement(writer, sortedElement);
        }

        string jsonString = Encoding.UTF8.GetString(stream.ToArray());
        return ApplyCustomIndentation(jsonString, indentationMode);
    }

    private static JsonWriterOptions CreateJsonWriterOptions(Indentation indentationMode)
    {
        var writerOptions = new JsonWriterOptions();
        writerOptions.Indented = indentationMode switch
        {
            Indentation.TwoSpaces => true,
            Indentation.FourSpaces => true,
            Indentation.OneTab => true,
            Indentation.Minified => false,
            _ => false,
        };
        return writerOptions;
    }

    private static string ApplyCustomIndentation(string jsonString, Indentation indentationMode)
    {
        return indentationMode switch
        {
            Indentation.TwoSpaces => jsonString.Replace("  ", "  "),
            Indentation.FourSpaces => jsonString.Replace("  ", "    "),
            Indentation.OneTab => jsonString.Replace("  ", "\t"),
            _ => jsonString
        };
    }

    private static object SortJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var sortedObject = new Dictionary<string, object>();
                IOrderedEnumerable<JsonProperty> properties
                    = element.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                foreach (JsonProperty property in properties)
                {
                    sortedObject[property.Name] = SortJsonElement(property.Value);
                }
                return sortedObject;

            case JsonValueKind.Array:
                var sortedArray = new List<object>();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    sortedArray.Add(SortJsonElement(item));
                }
                return sortedArray;

            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;

            case JsonValueKind.Number:
                return new JsonElementWrapper(element);

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();

            case JsonValueKind.Null:
                return null!;

            default:
                return new JsonElementWrapper(element);
        }
    }

    private static void WriteJsonElement(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case Dictionary<string, object> dict:
                writer.WriteStartObject();
                foreach (KeyValuePair<string, object> kvp in dict)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteJsonElement(writer, kvp.Value);
                }
                writer.WriteEndObject();
                break;

            case List<object> list:
                writer.WriteStartArray();
                foreach (object item in list)
                {
                    WriteJsonElement(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonElementWrapper wrapper:
                wrapper.Element.WriteTo(writer);
                break;

            case string str:
                writer.WriteStringValue(str);
                break;

            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;

            case null:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteRawValue(value.ToString()!);
                break;
        }
    }

    private static Indentation DetectIndentation(string json)
    {
        string[] lines = json.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (line.Length > 0 && char.IsWhiteSpace(line[0]))
            {
                if (line.StartsWith("\t"))
                {
                    return Indentation.OneTab;
                }
                else if (line.StartsWith("    "))
                {
                    return Indentation.FourSpaces;
                }
                else if (line.StartsWith("  "))
                {
                    return Indentation.TwoSpaces;
                }
            }
        }

        return Indentation.Minified;
    }
}
