using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Linq;

using Path = System.IO.Path;

namespace UnitTests.Fakes;

/// <summary>
/// Configures a shared WindowSill.API localizer containing the resources needed by unit tests.
/// </summary>
internal static class LocalizerSetup
{
    private static readonly string[] s_extensionNames =
    [
        "WindowSill.Date",
        "WindowSill.FileHelper",
    ];

    private static readonly Lazy<bool> s_initialization = new(
        Initialize,
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Ensures the shared localizer is configured exactly once.
    /// </summary>
    public static void EnsureInitialized()
    {
        _ = s_initialization.Value;
    }

    private static bool Initialize()
    {
        LoggingSetup.EnsureInitialized();

        Assembly apiAssembly = typeof(WindowSill.API.LocalizerExtensions).Assembly;
        Type localizerType = apiAssembly.GetType("WindowSill.API.Localizer")
            ?? throw new TypeLoadException("WindowSill.API.Localizer was not found.");
        Type languageDictionaryType = apiAssembly.GetType("WindowSill.API.LanguageDictionary")
            ?? throw new TypeLoadException("WindowSill.API.LanguageDictionary was not found.");
        Type itemType = languageDictionaryType.GetNestedType(
            "Item",
            BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new TypeLoadException("WindowSill.API.LanguageDictionary.Item was not found.");

        object localizer = RuntimeHelpers.GetUninitializedObject(localizerType);
        ConstructorInfo languageDictionaryConstructor = languageDictionaryType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: [typeof(string)],
            modifiers: null)
            ?? throw new MissingMethodException(languageDictionaryType.FullName, ".ctor(string)");
        object languageDictionary = languageDictionaryConstructor.Invoke(["en-US"]);

        foreach (string extensionName in s_extensionNames)
        {
            LoadReswFiles(languageDictionary, languageDictionaryType, itemType, extensionName);
        }

        FieldInfo dictionariesField = localizerType.GetField(
            "_languageDictionaries",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(localizerType.FullName, "_languageDictionaries");
        object dictionaries = Activator.CreateInstance(dictionariesField.FieldType)
            ?? throw new InvalidOperationException("Unable to create the localizer language dictionary collection.");
        MethodInfo addDictionaryMethod = dictionariesField.FieldType.GetMethod("Add")
            ?? throw new MissingMethodException(dictionariesField.FieldType.FullName, "Add");
        addDictionaryMethod.Invoke(dictionaries, ["en-US", languageDictionary]);
        dictionariesField.SetValue(localizer, dictionaries);

        FieldInfo dependencyPropertyMapField = localizerType.GetField(
            "_dependencyPropertyMap",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(localizerType.FullName, "_dependencyPropertyMap");
        object dependencyPropertyMap = Activator.CreateInstance(dependencyPropertyMapField.FieldType)
            ?? throw new InvalidOperationException("Unable to create the localizer dependency property map.");
        dependencyPropertyMapField.SetValue(localizer, dependencyPropertyMap);

        PropertyInfo currentDictionaryProperty = localizerType.GetProperty(
            "CurrentDictionary",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(localizerType.FullName, "CurrentDictionary");
        currentDictionaryProperty.SetValue(localizer, languageDictionary);

        MethodInfo setMethod = localizerType.GetMethod(
            "Set",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MissingMethodException(localizerType.FullName, "Set");
        setMethod.Invoke(null, [localizer]);

        return true;
    }

    private static void LoadReswFiles(
        object languageDictionary,
        Type languageDictionaryType,
        Type itemType,
        string extensionName)
    {
        MethodInfo addItemMethod = languageDictionaryType.GetMethod(
            "AddItem",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MissingMethodException(languageDictionaryType.FullName, "AddItem");
        ConstructorInfo itemConstructor = itemType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: [typeof(string), typeof(string), typeof(string), typeof(string)],
            modifiers: null)
            ?? throw new MissingMethodException(itemType.FullName, ".ctor(string, string, string, string)");

        string stringsDirectory = FindStringsDirectory(extensionName);
        foreach (string reswPath in Directory.GetFiles(stringsDirectory, "*.resw"))
        {
            string category = Path.GetFileNameWithoutExtension(reswPath);
            XDocument document = XDocument.Load(reswPath);

            foreach (XElement dataElement in document.Descendants("data"))
            {
                string? name = dataElement.Attribute("name")?.Value;
                string? value = dataElement.Element("value")?.Value;
                if (name is null || value is null)
                {
                    continue;
                }

                int dotIndex = name.IndexOf('.');
                string resourceName = dotIndex >= 0 ? name[..dotIndex] : name;
                string dependencyPropertyName = dotIndex >= 0 ? name[(dotIndex + 1)..] : string.Empty;
                string uid = $"/{extensionName}/{category}/{resourceName}";
                object item = itemConstructor.Invoke([uid, dependencyPropertyName, value, name]);
                addItemMethod.Invoke(languageDictionary, [item]);
            }
        }
    }

    private static string FindStringsDirectory(string extensionName)
    {
        string? directory = Path.GetDirectoryName(typeof(LocalizerSetup).Assembly.Location);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory, extensionName, "Strings", "en-US");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(directory, "src", extensionName, "Strings", "en-US");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new DirectoryNotFoundException(
            $"Unable to find the en-US resource directory for {extensionName}.");
    }
}
