using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Northernlight.Core.Localization;

/// <summary>
///     Represents an instance of a
///     language that can contain keyed
///     localization entries.
/// </summary>
public sealed partial class Language
{
    internal readonly List<LocalizationCategory> categories = [];

    private static readonly Regex formatGenericsRegex = GenerateFormatRegex();

    /// <summary>
    ///     This language's identifying name.
    /// </summary>
    public string Name
    {
        get;
        private set;
    }

    /// <summary>
    ///     The entry text to provide if for some reason
    ///     an entry lookup yields nothing in the registry.
    /// </summary>
    public const string Fallback = "???";

    public Language(string name, List<LocalizationCategory> categories)
    {
        Name = name;
        this.categories = categories;
    }

    /// <summary>
    ///     Acquires and returns a given
    ///     localization entry for this language
    ///     with a given key.
    /// </summary>
    public string Get(string key, params string[] formatArguments)
    {
        bool success = false;
        LocalizationEntry? entry = null;
        foreach (LocalizationCategory category in categories)
        {
            if (category.entries.TryGetValue(key, out entry))
            {
                success = true;
                break;
            }
        }

        Debug.Assert(success);

        if (success && entry is not null)
        {
            entry.key = key;

            return formatGenericsRegex.Replace(entry.Text, match =>
            {
                string formatedText = match.Value.Replace("{", string.Empty).Replace("}", string.Empty);
                if (int.TryParse(formatedText, CultureInfo.InvariantCulture, out int index) && index < formatArguments.Length)
                    return formatArguments[index];

                return match.Value;
            });
        }

        return Fallback;
    }

    /// <summary>
    ///     Sets a given localization entry for
    ///     this language with a given key.
    /// </summary>
    public void Set(string key, string text, LocalizationStatus statusAssumption = LocalizationStatus.RoughDraft)
    {
        LocalizationStatus status = statusAssumption;
        LocalizationCategory? usedCategory = null;

        foreach (LocalizationCategory category in categories)
        {
            if (category.entries.TryGetValue(key, out LocalizationEntry? existingEntry))
            {
                existingEntry.key = key;
                status = existingEntry.Status;
                usedCategory = category;
                break;
            }
        }

        usedCategory?.entries[key] = new LocalizationEntry(usedCategory.filePath)
        {
            Status = status,
            Text = text
        };
    }

    [GeneratedRegex(@"{(\d+)}")]
    private static partial Regex GenerateFormatRegex();
}
