using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Northernlight.Core.Localization;

/// <summary>
///     The central localization system.
/// </summary>
public static class LocalizationSystem
{
    private static FrozenDictionary<string, Language> languages;

    /// <summary>
    ///     The active language.
    /// </summary>
    public static Language Active
    {
        get;
        private set;
    }

    [ModuleInitializer]
    public static void Load()
    {
        Dictionary<string, Language> workingLanguages = [];
        foreach (string directory in Directory.GetDirectories(Path.Combine(Program.ContentRoot, "Localization")))
        {
            string localizationName = new DirectoryInfo(directory).Name;
            string evaluatedDirectory = Path.Combine(Environment.CurrentDirectory, directory);
            string[] files = Directory.GetFiles(evaluatedDirectory, "*.json");
            List<LocalizationCategory> categories = [];
            foreach (string file in files)
            {
                string fileContents = File.ReadAllText(file);
                Dictionary<string, LocalizationEntry>? fileEntries = JsonConvert.DeserializeObject<Dictionary<string, LocalizationEntry>>(fileContents);
                if (fileEntries is not null)
                    categories.Add(new LocalizationCategory(Path.GetFileNameWithoutExtension(file), fileContents, fileEntries));
            }

            workingLanguages[localizationName] = new Language(localizationName, categories);
        }

        languages = workingLanguages.ToFrozenDictionary();
        Active = languages.First().Value;
    }

    /// <summary>
    ///     Tries to set the current language of the game.
    /// </summary>
    public static void TrySetLanguage(string language)
    {
        if (languages.TryGetValue(language, out Language? l))
            Active = l;
    }
}
