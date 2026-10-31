using System.Diagnostics;
using Newtonsoft.Json;
using Northernlight.Core.Resources;

namespace Northernlight.Core;

/// <summary>
///     A simpler provider for reloadable data
///     saved in JSON.
/// </summary>
public sealed class ReloadableData<T> : ReloadableResource
{
    /// <summary>
    ///     The underlying data associated with the JSON.
    /// </summary>
    public T Value
    {
        get;
        private set;
    }

    public ReloadableData(string? filePath) : base(filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new NullReferenceException(nameof(filePath));

        LoadFromFile(filePath);
    }

    private void LoadFromFile(string filePath, int depth = 0)
    {
        try
        {
            string text = File.ReadAllText(filePath);
            T? jsonObject = JsonConvert.DeserializeObject<T>(text);

            Debug.Assert(jsonObject is not null);
            if (jsonObject is not null)
                Value = jsonObject;
        }
        catch (IOException)
        {
            if (depth < 32)
                LoadFromFile(filePath, depth + 1);
        }
    }

    protected internal override void Reload(FileSystemEventArgs fileChangeData) =>
        LoadFromFile(fileChangeData.FullPath);
}
