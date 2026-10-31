namespace Northernlight.Core.Localization;

/// <summary>
///     Represents an instance of a
///     localization category that can contain keyed
///     localization entries and exists in a larger
///     language instance.
/// </summary>
public sealed class LocalizationCategory
{
    internal readonly Dictionary<string, LocalizationEntry> entries;

    internal readonly string filePath;

    /// <summary>
    ///     This category's identifying name.
    /// </summary>
    public string Name
    {
        get;
        private set;
    }

    public LocalizationCategory(string name, string filePath, Dictionary<string, LocalizationEntry> entries)
    {
        Name = name;
        this.filePath = filePath;
        this.entries = entries;
        foreach (var kv in entries)
            kv.Value.key = kv.Key;
    }
}
