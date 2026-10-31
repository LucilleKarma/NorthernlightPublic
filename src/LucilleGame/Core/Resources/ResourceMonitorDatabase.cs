using System.Collections.Concurrent;

namespace Northernlight.Core.Resources;

/// <summary>
///     A resource database maintained by a <see cref="ResourceMonitor"/> instance.
/// </summary>
internal sealed class ResourceMonitorDatabase
{
    /// <summary>
    ///     The set of all maintained resources.
    /// </summary>
    internal ConcurrentDictionary<int, ReloadableResource> resources = [];

    /// <summary>
    ///     The set of all free keys created due to returned resources.
    /// </summary>
    internal ConcurrentQueue<int> freeKeys = [];

    /// <summary>
    ///     Rents an index in the central dictionary to a given resource.
    /// </summary>
    internal void RentIndex(ReloadableResource resource)
    {
        ResourceMonitor.CreateWatcherIfNecessary(resource.FilePath);

        int key = resources.Count;
        if (freeKeys.TryDequeue(out int freeKey))
            key = freeKey;

        while (!resources.TryAdd(key, resource))
            key++;

        resource.Id = key;
    }

    /// <summary>
    ///     Returns a resource's index to the central dictionary.
    /// </summary>
    internal void ReturnIndex(ReloadableResource resource)
    {
        resources.TryRemove(resource.Id, out _);
        freeKeys.Enqueue(resource.Id);
    }
}
