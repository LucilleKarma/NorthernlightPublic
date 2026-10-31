using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a topology setting for a trail.
/// </summary>
public interface ITrailTopologySetting
{
    /// <summary>
    ///     The enumerated type that this topology
    ///     corresonds to.
    /// </summary>
    static abstract GPUPrimitiveType Type
    {
        get;
    }

    /// <summary>
    ///     Calculates the amount of indices for
    ///     a given amount of trail points.
    /// </summary>
    static abstract int CalculateIndexCount(int pointCount);

    /// <summary>
    ///     Calculates the amount of primitives for
    ///     a given amount of trail points.
    /// </summary>
    static abstract int CalculatePrimitiveCount(int pointCount);

    /// <summary>
    ///     Loads preliminary data for this
    ///     topology, including indices and a
    ///     mesh stream instance.
    /// </summary>
    static abstract void LoadData(CommandBuffer commandBuffer, out uint[] indicesCache, out MeshStream<StandardVertex> meshGpu);
}
