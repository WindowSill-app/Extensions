using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnitTests.Date.Core.Fakes;

/// <summary>
/// Initializes a no-op Localizer for unit tests so that
/// <c>GetLocalizedString()</c> does not throw "Localizer isn't initialized yet".
/// Uses reflection and <see cref="RuntimeHelpers.GetUninitializedObject"/> to bypass
/// WinUI COM dependencies, then initializes critical internal fields.
/// </summary>
internal static class LocalizerSetup
{
    private static bool _initialized;

    /// <summary>
    /// Ensures the localizer is initialized. Safe to call multiple times.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        LoggingSetup.EnsureInitialized();

        Assembly apiAssembly = typeof(WindowSill.API.LocalizerExtensions).Assembly;
        Type localizerType = apiAssembly.GetType("WindowSill.API.Localizer")!;
        Type langDictType = apiAssembly.GetType("WindowSill.API.LanguageDictionary")!;

        // Create an uninitialized Localizer (bypasses constructor which subscribes to WinUI events).
        object localizer = RuntimeHelpers.GetUninitializedObject(localizerType);

        // Initialize _languageDictionaries to an empty dictionary so GetLocalizedString doesn't NRE.
        FieldInfo? dictField = localizerType.GetField(
            "_languageDictionaries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (dictField is not null)
        {
            // Create the exact generic Dictionary<string, LanguageDictionary> via reflection.
            Type dictType = dictField.FieldType;
            object emptyDict = RuntimeHelpers.GetUninitializedObject(dictType);
            // Initialize dictionary internals by calling the parameterless constructor via the base.
            ConstructorInfo? ctor = dictType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            ctor?.Invoke(emptyDict, null);
            dictField.SetValue(localizer, emptyDict);
        }

        // Initialize CurrentDictionary to an empty LanguageDictionary so GetCurrentLanguage works.
        // LanguageDictionary has no accessible constructor, so use uninitialized + field set.
        object emptyLangDict = RuntimeHelpers.GetUninitializedObject(langDictType);

        // Set the Language property/field to "en-US".
        PropertyInfo? langProp = langDictType.GetProperty(
            "Language",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (langProp?.CanWrite == true)
        {
            langProp.SetValue(emptyLangDict, "en-US");
        }
        else
        {
            // Try all instance fields for a string field that could be Language.
            foreach (FieldInfo fi in langDictType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (fi.FieldType == typeof(string))
                {
                    fi.SetValue(emptyLangDict, "en-US");
                    break;
                }
            }
        }
        PropertyInfo? currentDictProp = localizerType.GetProperty(
            "CurrentDictionary",
            BindingFlags.Instance | BindingFlags.NonPublic);
        currentDictProp?.SetValue(localizer, emptyLangDict);

        // Set the static Instance.
        MethodInfo? setMethod = localizerType.GetMethod(
            "Set",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        setMethod?.Invoke(null, [localizer]);

        _initialized = true;
    }
}
