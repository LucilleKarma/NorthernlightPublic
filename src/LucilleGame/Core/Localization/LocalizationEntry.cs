using Newtonsoft.Json;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Localization;

/// <summary>
///     Represents an instance of a localization entry.
/// </summary>
public sealed class LocalizationEntry : ReloadableResource
{
    [JsonIgnore]
    internal string key;

    /// <summary>
    ///     The text associated with this localization
    ///     entry.
    /// </summary>
    public required string Text
    {
        get;
        set;
    }

    /// <summary>
    ///     The development status of this entry.
    /// </summary>
    public required LocalizationStatus Status
    {
        get;
        set;
    }

    public LocalizationEntry(string filePath) : base(filePath)
    { }

    protected internal override void Reload(FileSystemEventArgs fileChangeData)
    {
        if (FilePath is null || !File.Exists(FilePath))
            return;

        try
        {
            string fileText = File.ReadAllText(FilePath);
            Dictionary<string, LocalizationEntry>? fileEntries = JsonConvert.DeserializeObject<Dictionary<string, LocalizationEntry>>(fileText);
            if (fileEntries?.TryGetValue(key, out LocalizationEntry? newEntry) ?? false)
            {
                Text = newEntry.Text;
                Status = newEntry.Status;
            }
        }
        catch (IOException)
        {

        }
    }
}
