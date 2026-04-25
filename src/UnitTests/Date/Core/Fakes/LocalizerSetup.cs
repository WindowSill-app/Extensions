using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using Path = System.IO.Path;

namespace UnitTests.Date.Core.Fakes;

/// <summary>
/// Initializes a no-op Localizer for unit tests so that
/// <c>GetLocalizedString()</c> does not throw "Localizer isn't initialized yet".
/// Uses reflection and <see cref="RuntimeHelpers.GetUninitializedObject"/> to bypass
/// WinUI COM dependencies, then initializes critical internal fields.
/// Optionally loads <c>.resw</c> resource files so that localized string lookups
/// return real values instead of empty strings.
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
        Type itemType = langDictType.GetNestedType("Item", BindingFlags.NonPublic | BindingFlags.Public)!;

        // Create an uninitialized Localizer (bypasses constructor which subscribes to WinUI events).
        object localizer = RuntimeHelpers.GetUninitializedObject(localizerType);

        // Create a real LanguageDictionary("en-US") via its internal constructor.
        ConstructorInfo langDictCtor = langDictType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: [typeof(string)],
            modifiers: null)!;
        object langDict = langDictCtor.Invoke(["en-US"]);

        // Load .resw files and populate the dictionary.
        LoadReswFiles(langDict, langDictType, itemType);

        // Initialize _languageDictionaries with our populated dictionary.
        FieldInfo? dictField = localizerType.GetField(
            "_languageDictionaries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (dictField is not null)
        {
            Type dictType = dictField.FieldType;
            object dictInstance = Activator.CreateInstance(dictType)!;
            MethodInfo addMethod = dictType.GetMethod("Add")!;
            addMethod.Invoke(dictInstance, ["en-US", langDict]);
            dictField.SetValue(localizer, dictInstance);
        }

        // Initialize _dependencyPropertyMap to an empty dictionary.
        FieldInfo? dpMapField = localizerType.GetField(
            "_dependencyPropertyMap",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (dpMapField is not null)
        {
            dpMapField.SetValue(localizer, Activator.CreateInstance(dpMapField.FieldType));
        }

        // Set CurrentDictionary.
        PropertyInfo? currentDictProp = localizerType.GetProperty(
            "CurrentDictionary",
            BindingFlags.Instance | BindingFlags.NonPublic);
        currentDictProp?.SetValue(localizer, langDict);

        // Set the static Instance.
        MethodInfo? setMethod = localizerType.GetMethod(
            "Set",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        setMethod?.Invoke(null, [localizer]);

        _initialized = true;
    }

    /// <summary>
    /// Loads all <c>.resw</c> files from the WindowSill.Date extension's Strings/en-US
    /// directory and adds each entry to the <see cref="LanguageDictionary"/>.
    /// The resource UID format is <c>/WindowSill.Date/{Category}/{Key}</c>, matching
    /// the convention used by <c>GetLocalizedString()</c>.
    /// </summary>
    private static void LoadReswFiles(object langDict, Type langDictType, Type itemType)
    {
        MethodInfo addItemMethod = langDictType.GetMethod(
            "AddItem",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
        ConstructorInfo itemCtor = itemType.GetConstructors()[0];

        // Walk up from the test assembly output dir to find the source tree.
        string? stringsDir = FindStringsDirectory();
        if (stringsDir is null)
        {
            return;
        }

        foreach (string reswPath in Directory.GetFiles(stringsDir, "*.resw"))
        {
            string category = Path.GetFileNameWithoutExtension(reswPath);
            XDocument doc = XDocument.Load(reswPath);

            foreach (XElement dataElement in doc.Descendants("data"))
            {
                string? name = dataElement.Attribute("name")?.Value;
                string? value = dataElement.Element("value")?.Value;
                if (name is null || value is null)
                {
                    continue;
                }

                // Split name into UID and dependency property for XAML-style keys
                // like "EnableTravelTime.HeaderProperty", or use as-is for code keys
                // like "ProviderMicrosoftTeams".
                string uid;
                string depPropName;

                int dotIndex = name.IndexOf('.');
                if (dotIndex >= 0)
                {
                    uid = $"/WindowSill.Date/{category}/{name[..dotIndex]}";
                    depPropName = name[(dotIndex + 1)..];
                }
                else
                {
                    uid = $"/WindowSill.Date/{category}/{name}";
                    depPropName = string.Empty;
                }

                // Item(string Uid, string DependencyPropertyName, string Value, string StringResourceItemName)
                object item = itemCtor.Invoke([uid, depPropName, value, name]);
                addItemMethod.Invoke(langDict, [item]);
            }
        }
    }

    /// <summary>
    /// Walks up from the test assembly directory to find the Strings/en-US directory.
    /// </summary>
    private static string? FindStringsDirectory()
    {
        string? dir = Path.GetDirectoryName(typeof(LocalizerSetup).Assembly.Location);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "WindowSill.Date", "Strings", "en-US");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            // Also try src/ subdirectory (when running from repo root).
            candidate = Path.Combine(dir, "src", "WindowSill.Date", "Strings", "en-US");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}
