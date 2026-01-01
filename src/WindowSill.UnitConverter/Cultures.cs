using Microsoft.Recognizers.Text;

namespace WindowSill.UnitConverter;

/// <summary>
/// This class provides the list of supported cultures in a specific order that we decided.
/// We have to do this because Microsoft.Recognizers.Text.Culture.SupportedCultures is read-only, doesn't
/// allow us to create our own instance of `Culture`, and its order is not the one we want.
/// The reason why the order is not the one we want is that the quality of detection for some cultures
/// isn't optimal. For example, in Culture.SupportedCultures, "Italian" comes before "Hindi", but "Hindi"
/// tends to provide more accurate results for certain inputs. Therefore, we define our own order.
/// </summary>
internal static class Cultures
{
    internal static Culture[] OrderedSupportedCultures { get; }

    static Cultures()
    {
        var orderMap = SupportedCultureNames
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index);

        OrderedSupportedCultures = Culture.SupportedCultures
            .OrderBy(culture => orderMap.TryGetValue(culture.CultureName, out int order) ? order : int.MaxValue)
            .ToArray();
    }

    public static readonly string[] SupportedCultureNames
        = [
            "Hindi",
            "Korean",
            "Chinese",
            "Arabic",
            "EnglishOthers",
            "English",
            "Spanish",
            "SpanishMexican",
            "Portuguese",
            "French",
            "German",
            "Japanese",
            "Dutch",
            "Swedish",
            "Bulgarian",
            "Turkish",
            "Italian",
        ];
}
