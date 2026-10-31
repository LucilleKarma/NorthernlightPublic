using System.Numerics;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a per-index frame of a trail
///     as it's being constructed.
/// </summary>
public struct TrailFrame
{
    /// <summary>
    ///     This frame's normal vector.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    ///     This frame's tangent vector.
    /// </summary>
    public Vector3 Tangent;

    /// <summary>
    ///     This frame's binormal vector.
    /// </summary>
    public Vector3 Binormal;
}
