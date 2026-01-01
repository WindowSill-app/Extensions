using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WindowSill.API;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace WindowSill.DevToys.Core;

internal static partial class YamlHelper
{
    private static readonly ILogger logger = typeof(YamlHelper).Log();

    /// <summary>
    /// Convert a Json string to Yaml
    /// </summary>
    internal static string ConvertFromJson(string? input, Indentation indentation)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        try
        {
            var replacements = new Dictionary<Guid, string>();
            bool hasEscape = UnicodeEscapeTextRegex().IsMatch(input);
            if (hasEscape)
            {
                input = UnicodeEscapeTextRegex().Replace(input, match =>
                {
                    var uuid = Guid.NewGuid();
                    replacements[uuid] = match.Value;
                    return uuid.ToString();
                });
            }
            var token = JsonNode.Parse(input, documentOptions: new() { CommentHandling = JsonCommentHandling.Skip });
            if (token is null)
            {
                return string.Empty;
            }

            object? jsonObject = ConvertJTokenToObject(token, 0);

            if (jsonObject is not null and not string)
            {
                int indent = 0;
                indent = indentation switch
                {
                    Indentation.TwoSpaces => 2,
                    Indentation.FourSpaces => 4,
                    _ => throw new NotSupportedException(),
                };
                var serializer
                    = Serializer.FromValueSerializer(
                        new SerializerBuilder().BuildValueSerializer(),
                        EmitterSettings.Default.WithBestIndent(indent).WithIndentedSequences());

                string? yaml = serializer.Serialize(jsonObject);
                if (string.IsNullOrWhiteSpace(yaml))
                {
                    return string.Empty;
                }

                if (hasEscape)
                {
                    yaml = replacements.Aggregate(yaml, (current, replacement) =>
                        current.Replace(replacement.Key.ToString(), replacement.Value));
                }

                return new(yaml);
            }
        }
        catch (JsonException ex)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yaml to Json Converter");
        }
        return string.Empty;
    }

    private static dynamic? ParseValue(JsonValue token)
    {
        var elem = (JsonElement)token.GetValue<object>();
        return elem.ValueKind switch
        {
            JsonValueKind.String => elem.GetString(),
            JsonValueKind.Number => elem.GetDecimal(),
            JsonValueKind.False => false,
            JsonValueKind.True => true,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined or
            JsonValueKind.Object or
            JsonValueKind.Array or
            _ => throw new NotSupportedException(),
        };
    }

    private static object? ConvertJTokenToObject(JsonNode? node, int level)
    {
        if (node is null)
        {
            return null;
        }

        if (level > 10)
        {
            throw new InvalidDataException($"Json structure is not supported: nested level in array is too deep. ({level}).");
        }
        return node switch
        {
            JsonValue val => ParseValue(val),
            JsonArray arr => arr.Select(o => ConvertJTokenToObject(o, level + 1)).ToList(),
            JsonObject obj => obj.AsObject().ToDictionary(x => x.Key, x => ConvertJTokenToObject(x.Value, level)),
            null => null,
            _ => throw new InvalidOperationException("Unexpected token: " + node)
        };
    }

    [GeneratedRegex(@"\\u([A-Fa-f0-9]{4})")]
    private static partial Regex UnicodeEscapeTextRegex();
}
