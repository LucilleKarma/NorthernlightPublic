using System.Numerics;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of a transient trail build
///     state.
/// </summary>
public ref struct TransientTrailBuildState(Span<StandardVertex> vertices, Span<Vector3> interpolatedPoints,
                                           Span<Vector3> unnormalizedTrailTangents, Span<TrailFrame> trailFrame)
{
    /// <summary>
    ///     The overall arc length of the entire
    ///     trail.
    /// </summary>
    public float ArcLength;

    /// <summary>
    ///     The vertex cache.
    /// </summary>
    public readonly Span<StandardVertex> Vertices = vertices;

    /// <summary>
    ///     The set of all interpolated points stored
    ///     by this state.
    /// </summary>
    public readonly Span<Vector3> InterpolatedPoints = interpolatedPoints;

    /// <summary>
    ///     The unnormalized tangents stored by this state.
    /// </summary>
    public readonly Span<Vector3> UnnormalizedTrailTangents = unnormalizedTrailTangents;

    /// <summary>
    ///     The frame stored by this state.
    /// </summary>
    public readonly Span<TrailFrame> TrailFrame = trailFrame;
}
