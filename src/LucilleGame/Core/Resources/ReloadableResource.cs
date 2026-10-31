using Newtonsoft.Json;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a resource that can detect external
///     file modification and respond with on-the-fly resource value changes.
/// </summary>
public abstract class ReloadableResource
{
    /// <summary>
    ///     The file path associated with this resource.
    ///     If this is <see langword="null"/>, reloading will not apply.
    /// </summary>
    [JsonIgnore]
    public string? FilePath
    {
        get;
        private set;
    }

    /// <summary>
    ///     The identifier of this resource for the <see cref="ResourceMonitor"/>.
    /// </summary>
    [JsonIgnore]
    public int Id
    {
        get;
        internal set;
    }

    protected ReloadableResource(string? filePath)
    {
        if (filePath is not null)
            filePath = ResourceMonitor.CheckForSubdirectoryEquivalent(filePath);

        FilePath = filePath;
        if (FilePath is not null)
            ResourceMonitor.Database.RentIndex(this);
    }

    /// <summary>
    ///     Reloads the contents of this resource.
    /// </summary>
    protected internal abstract void Reload(FileSystemEventArgs fileChangeData);

    /// <summary>
    ///     Returns this resource's index, effectively removing it
    ///     from the <see cref="ResourceMonitor"/> and freeing it from
    ///     all reloading considerations.
    /// </summary>
    public void Return()
    {
        if (FilePath is not null)
            ResourceMonitor.Database.ReturnIndex(this);
    }
}
