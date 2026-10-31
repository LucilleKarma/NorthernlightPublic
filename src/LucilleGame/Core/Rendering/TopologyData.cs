using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of static, load-time topology data.
/// </summary>
public sealed class TopologyData<TTopology> where TTopology : ITrailTopologySetting
{
    /// <summary>
    ///     Preset indices for use with this specific topology.
    /// </summary>
    public uint[] IndicesCache
    {
        get;
    }

    /// <summary>
    ///     A per-topology-data GPU-bound mesh that can be
    ///     used for rendering of data on a per-instance data.
    /// </summary>
    /// <remarks>
    ///     This should be used sparingly if possible, as it
    ///     is antithetical to batching principles.
    /// </remarks>
    public MeshStream<StandardVertex> MeshGpu
    {
        get;
    }

    /// <summary>
    ///     The static instance of this topology data.
    /// </summary>
    public static TopologyData<TTopology> Instance
    {
        get;
        internal set;
    }

    internal TopologyData(CommandBuffer commandBuffer)
    {
        TTopology.LoadData(commandBuffer, out uint[] indicesCache, out MeshStream<StandardVertex> meshGpu);
        IndicesCache = indicesCache;
        MeshGpu = meshGpu;
    }
}
