using System.Numerics;
using System.Runtime.CompilerServices;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Core.Rendering;

/// <summary>
///     A trail mesh builder service for use with a mesh renderer.
/// </summary>
public static class TrailBuilder
{
    /// <summary>
    ///     The internal byte quantity stack allocation limit.
    /// </summary>
    private static uint StackAllocationLimit = DefaultStackAllocationLimit;

    /// <summary>
    ///     The fallback unit vector to use during
    ///     normal normalization for normals if the
    ///     vector cannot by normalized safely.
    /// </summary>
    private static readonly Vector3 NormalNormalizationFallback = Vector3.UnitX;

    /// <summary>
    ///     The fallback unit vector to use during
    ///     tangent normalization for normals if the
    ///     vector cannot by normalized safely.
    /// </summary>
    private static readonly Vector3 TangentNormalizationFallback = Vector3.UnitY;

    /// <summary>
    ///     The fallback unit vector to use during
    ///     binormal normalization for normals if the
    ///     vector cannot by normalized safely.
    /// </summary>
    private static readonly Vector3 BinormalNormalizationFallback = Vector3.UnitZ;

    /// <summary>
    ///     The fallback unit vector to use during
    ///     axial normalization for normals if the
    ///     vector cannot by normalized safely.
    /// </summary>
    private static readonly Vector3 AxisNormalizationFallback = Vector3.UnitZ;

    /// <summary>
    ///     The internal epsilon constant used for internal
    ///     calculation safety in extreme cases throughout this
    ///     system, such as the detection of extreme angles via
    ///     dot products.
    /// </summary>
    private const float Epsilon = 0.0001f;

    /// <summary>
    ///     The default stack internal allocation limit.
    ///     Refer to <see cref="ConfigureStackAllocationLimit"/> for details.
    /// </summary>
    public const uint DefaultStackAllocationLimit = 8192;

    // These unnormalized tangents are intended to be used by
    // smoothing schemes later, most notably in the case of Hermite
    // polynomial interpolation.

    /// <summary>
    ///     Calculates an unnormalized tangent vector at a given index
    ///     for a given set of control points, using finite difference methods
    ///     as a good estimate.
    /// </summary>
    private static Vector3 CalculateUnnormalizedTangent(ReadOnlySpan<Vector3> controlPoints, int index)
    {
        Vector3 unnormalizedDirection;
        Vector3 current = controlPoints[index];

        if (index >= controlPoints.Length - 1)
        {
            Vector3 previous = controlPoints[index - 1];
            unnormalizedDirection = current - previous;
        }
        else if (index >= 1)
        {
            Vector3 previous = controlPoints[index - 1];
            Vector3 ahead = controlPoints[index + 1];
            unnormalizedDirection = (ahead - previous) * 0.5f;
        }
        else
        {
            Vector3 ahead = controlPoints[index + 1];
            unnormalizedDirection = ahead - current;
        }

        return unnormalizedDirection;
    }

    /// <summary>
    ///     Calculates the intermediate normal orthogonal vector based
    ///     on a suggested value and current tangent, electing to use
    ///     the nearest basis vector if the suggestion would result in degenerate
    ///     vectors when performing cross products with the tangent later.
    /// </summary>
    private static Vector3 CalculateNormalOrthogonal<TTopology>(Vector3 tangent, TrailSettings<TTopology> settings)
         where TTopology : ITrailTopologySetting
    {
        if (float.Abs(Vector3.Dot(tangent, settings.SuggestedNormalOrthogonal)) <= 1f - Epsilon)
            return settings.SuggestedNormalOrthogonal;

        Vector3 fallback = Vector3.UnitX;
        float minOrthogonality = float.Abs(Vector3.Dot(tangent, fallback));
        float yOrthogonality = float.Abs(Vector3.Dot(tangent, Vector3.UnitY));
        if (yOrthogonality < minOrthogonality)
        {
            fallback = Vector3.UnitY;
            minOrthogonality = yOrthogonality;
        }

        float zOrthogonality = float.Abs(Vector3.Dot(tangent, Vector3.UnitZ));
        if (zOrthogonality < minOrthogonality)
            fallback = Vector3.UnitZ;

        return fallback;
    }

    /// <summary>
    ///     Calculates a normal vector at a given index for a given
    ///     trail build state in accordance with parallel transport frames, generally
    ///     handling extreme edge cases in potential trail mesh geometry.
    /// </summary>
    private static Vector3 CalculateNormal<TTopology>(int index, TrailSettings<TTopology> settings, TransientTrailBuildState state)
         where TTopology : ITrailTopologySetting
    {
        TrailFrame currentFrame = state.TrailFrame[index];
        Vector3 tangent = currentFrame.Tangent;
        Vector3 normalOrthogonal = CalculateNormalOrthogonal(tangent, settings);
        Vector3 intermediateCross = Vector3.Cross(tangent, normalOrthogonal);
        Vector3 basicOrthogonal = Vector3.Cross(tangent, intermediateCross).SafeNormalize(NormalNormalizationFallback);

        if (index <= 0)
            return basicOrthogonal;

        // Calculate the sine and cosine of the angle between
        // the previous and current tangent, and then apply
        // the same rotation to the previous normal to calculate the
        // new one, thus better handling extreme angles than the
        // naive approach calculated by basicOrthogonal.
        TrailFrame previousFrame = state.TrailFrame[index - 1];
        Vector3 previousNormal = previousFrame.Normal;
        Vector3 previousTangent = previousFrame.Tangent;
        Vector3 rotationAxis = Vector3.Cross(previousTangent, tangent);
        Vector3 normalizedRotationAxis = rotationAxis.SafeNormalize(AxisNormalizationFallback);
        float sinAngularOffset = float.Clamp(rotationAxis.Length(), 0f, 1f);
        float cosAngularOffset = float.Clamp(Vector3.Dot(previousTangent, tangent), -1f, 1f);
        bool noticeableAngularDeviation = sinAngularOffset >= Epsilon;

        // Handle the edge case of near-complete flips in the
        // trail frame that manage to evade the detection by the
        // above sine calculation, since sin(0.0001) and sin(0.9999pi) for example
        // both yield values very, very close to zero, when in practice the angular
        // disparites of something near 0 degrees and near 180 degrees are quite
        // different.
        // This should be extremely rare in practice, but I'll elect to account
        // for it regardless.
        if (cosAngularOffset <= Epsilon - 1f)
        {
            // This should be more consistent than the
            // originally calculated axis, since in this edge-case the cross product
            // is definitionally extremely close to zero, and thus potentially
            // unstable.
            normalizedRotationAxis = previousFrame.Binormal;

            // This is a simplified form of the more
            // general Rodrigues rotation formula below, with
            // a turning angle of pi.
            // This reduces the sine term to 0, the cosine term to -1, and
            // creates the mathematical reduction below.
            return -previousNormal + normalizedRotationAxis * Vector3.Dot(normalizedRotationAxis, previousNormal) * 2f;
        }

        if (noticeableAngularDeviation)
        {
            float angularOffset = float.Atan2(sinAngularOffset, cosAngularOffset);
            return Vector3.RodriguesRotation(normalizedRotationAxis, previousNormal, angularOffset);
        }

        return previousNormal;
    }

    /// <summary>
    ///     Applies and returns a sanity-check orthogonalization step
    ///     to a given index in a trail build state, so as to address potential
    ///     floating point drift in larger trails.
    /// </summary>
    private static Vector3 StrictOrthogonalizeNormal(int index, TransientTrailBuildState state)
    {
        ref TrailFrame frame = ref state.TrailFrame[index];
        Vector3 originalNormal = frame.Normal;
        Vector3 tangent = frame.Tangent;
        return (originalNormal - tangent * Vector3.Dot(originalNormal, tangent)).SafeNormalize(NormalNormalizationFallback);
    }

    /// <summary>
    ///     Constructs tangent and (bi)normal data for a trail build
    ///     state from a set of control points and trail construction settings.
    /// </summary>
    private static void ConstructTangentsAndNormals<TTopology>(ReadOnlySpan<Vector3> controlPoints, TransientTrailBuildState state, TrailSettings<TTopology> settings)
         where TTopology : ITrailTopologySetting
    {
        for (int i = 0; i < controlPoints.Length; i++)
        {
            ref TrailFrame frame = ref state.TrailFrame[i];
            state.UnnormalizedTrailTangents[i] = CalculateUnnormalizedTangent(controlPoints, i);
            frame.Tangent = state.UnnormalizedTrailTangents[i].SafeNormalize(TangentNormalizationFallback);
        }
        for (int i = 0; i < controlPoints.Length; i++)
        {
            ref TrailFrame frame = ref state.TrailFrame[i];
            frame.Normal = CalculateNormal(i, settings, state);
            frame.Normal = StrictOrthogonalizeNormal(i, state);
            frame.Binormal = Vector3.Cross(frame.Tangent, frame.Normal).SafeNormalize(BinormalNormalizationFallback);
        }
    }

    /// <summary>
    ///     Calculates UVWs for use with trail vertex data according to use-selected
    ///     trail construction settings.
    /// </summary>
    private static Vector3 CalculateUVW<TTopology>(TrailSettings<TTopology> settings, TransientTrailBuildState state, float trailProgress, float width, float arcLengthSum, bool leftSide)
         where TTopology : ITrailTopologySetting
    {
        float v;

        // A default value of one ensures innate compatibility with the
        // pixel shader hack AND the normal side calculation
        // method at the same time. The underlying equation for such
        // when calculating the real, pixel-shader-granulated
        // UVs is v = (v - 0.5) / w + 0.5
        // For the normal side variant, where w is one, this
        // naturally reduces to no change at all after some
        // easy algebraic manipulations.
        float w = 1f;

        switch (settings.SideUvCalculationMethod)
        {
            case TrailSideUvCalculation.Normal:
            default:
                v = leftSide ? 0f : 1f;
                break;
            case TrailSideUvCalculation.PixelShaderWidthHack:
                v = width * -0.5f * (leftSide ? 1f : -1f) + 0.5f;
                w = width;
                break;
        }

        float u = settings.ForwardUvCalculationMethod switch
        {
            TrailForwardUvCalculation.ArcLength => state.ArcLength >= Epsilon ? arcLengthSum / state.ArcLength : 0f,
            _ => trailProgress,
        };

        return new Vector3(u, v, w);
    }

    /// <summary>
    ///     Constructs vertex data for a given trail mesh based on
    ///     a constructed trail build state, interpolated control
    ///     points, and trail construction settings.
    /// </summary>
    private static void ConstructVertices<TTopology>(ReadOnlySpan<Vector3> interpolatedPoints, TransientTrailBuildState state, TrailSettings<TTopology> settings)
         where TTopology : ITrailTopologySetting
    {
        float rectifiedArcLengthSum = 0f;
        for (int i = 0; i < interpolatedPoints.Length; i++)
        {
            float trailProgress = i / (float)(interpolatedPoints.Length - 1f);
            float width = float.Max(settings.WidthFunction(trailProgress), Epsilon);
            float halfWidth = width * 0.5f;
            Color color = settings.ColorFunction(trailProgress);
            Vector4 colorVector = color.AsVector4();
            Vector3 normal = state.TrailFrame[i].Normal;

            Vector3 center = interpolatedPoints[i];
            Vector3 leftPosition = center - normal * halfWidth;
            Vector3 rightPosition = center + normal * halfWidth;

            int vertexBaseIndex = i * 2;
            ref StandardVertex left = ref state.Vertices[vertexBaseIndex];
            left.Position = leftPosition;
            left.Color = colorVector;
            left.Uvw = CalculateUVW(settings, state, trailProgress, width, rectifiedArcLengthSum, true);
            left.Normal = normal;

            ref StandardVertex right = ref state.Vertices[vertexBaseIndex + 1];
            right.Position = rightPosition;
            right.Color = colorVector;
            right.Uvw = CalculateUVW(settings, state, trailProgress, width, rectifiedArcLengthSum, false);
            right.Normal = normal;

            if (i < interpolatedPoints.Length - 1)
                rectifiedArcLengthSum += Vector3.Distance(interpolatedPoints[i], interpolatedPoints[i + 1]);
        }
    }

    /// <summary>
    ///     Internally builds a complete trail mesh, emitting
    ///     its constructed vertex data into the <paramref name="vertices"/> cache.
    /// </summary>
    private static void BuildMesh<TTopology>(ReadOnlySpan<Vector3> controlPoints, TrailSettings<TTopology> settings, int pointCount, bool preferStack, Span<StandardVertex> vertices, out int primitiveCount)
         where TTopology : ITrailTopologySetting
    {
        if (controlPoints.Length <= 1)
            throw new InvalidOperationException("Two or more control points are required.");
        if (pointCount <= 1)
            throw new InvalidOperationException("Two or more output points are required.");

        primitiveCount = TTopology.CalculatePrimitiveCount(pointCount);

        Span<Vector3> interpolatedPoints = preferStack ? stackalloc Vector3[pointCount] : new Vector3[pointCount];
        Span<Vector3> unnormalizedTrailTangents = preferStack ? stackalloc Vector3[controlPoints.Length] : new Vector3[controlPoints.Length];
        Span<TrailFrame> trailFrame = preferStack ? stackalloc TrailFrame[controlPoints.Length] : new TrailFrame[controlPoints.Length];
        TransientTrailBuildState state = new TransientTrailBuildState(vertices, interpolatedPoints, unnormalizedTrailTangents, trailFrame);

        ConstructTangentsAndNormals(controlPoints, state, settings);
        settings.Smoothing(controlPoints, interpolatedPoints, state);

        for (int i = 0; i < interpolatedPoints.Length - 1; i++)
            state.ArcLength += Vector3.Distance(interpolatedPoints[i], interpolatedPoints[i + 1]);

        // Reconstruct tangents and normals around the interpolated points.
        // They will be needed for the construction of the vertices.
        unnormalizedTrailTangents = preferStack ? stackalloc Vector3[interpolatedPoints.Length] : new Vector3[interpolatedPoints.Length];
        trailFrame = preferStack ? stackalloc TrailFrame[interpolatedPoints.Length] : new TrailFrame[interpolatedPoints.Length];
        state = new TransientTrailBuildState(vertices, interpolatedPoints, unnormalizedTrailTangents, trailFrame) with
        {
            ArcLength = state.ArcLength
        };
        ConstructTangentsAndNormals(interpolatedPoints, state, settings);

        ConstructVertices(interpolatedPoints, state, settings);
    }

    // This method should be updated if any of the stack-allocating
    // constructions used elsewhere are changed.
    /// <summary>
    ///     Calculates expected stack-allocated byte counts based on a given
    ///     quantity of interpolated points and base control points.
    /// </summary>
    internal static int CalculateStackByteCount(int interpolatedPointCount, int controlPointCount)
    {
        return
            Unsafe.SizeOf<StandardVertex>() * interpolatedPointCount * 2 + // Vertices.
            Unsafe.SizeOf<Vector3>() * interpolatedPointCount + // Interpolanted points.
            Unsafe.SizeOf<Vector3>() * controlPointCount + // Unnormalized trail tangents.
            Unsafe.SizeOf<TrailFrame>() * controlPointCount + // Base trail frame.
            Unsafe.SizeOf<Vector3>() * interpolatedPointCount + // Reconstructed unnormalized trail tangents.
            Unsafe.SizeOf<TrailFrame>() * interpolatedPointCount; // Reconstructed trail frame.
    }

    /// <summary>
    ///     Builds a trail mesh from a span of 3D control points.
    /// </summary>
    /// <remarks>
    ///     This method allocates vertex and index data as arrays once prepared.
    ///     It is advisable to use the primarily stack-allocating version of this method
    ///     where possible.
    /// </remarks>
    public static OwnedMesh<StandardVertex> BuildOwnedMesh<TTopology>(ReadOnlySpan<Vector3> controlPoints, TrailSettings<TTopology> settings)
         where TTopology : ITrailTopologySetting
    {
        int pointCount = settings.PointCountOverride ?? controlPoints.Length;

        int totalByteCount = CalculateStackByteCount(pointCount, controlPoints.Length);
        bool preferStack = !ExceedsStackAllocationLimit(totalByteCount);
        Span<StandardVertex> vertices = preferStack ? stackalloc StandardVertex[pointCount * 2] : new StandardVertex[pointCount * 2];
        Span<uint> indices = TopologyData<TTopology>.Instance.IndicesCache.AsSpan(0, TTopology.CalculateIndexCount(pointCount));
        BuildMesh(controlPoints, settings, pointCount, preferStack, vertices, out int primitiveCount);

        return new OwnedMesh<StandardVertex>(TTopology.Type, vertices.ToArray(), indices.ToArray(), primitiveCount);
    }

    /// <summary>
    ///     Builds a trail mesh from a span of 3D control points.
    /// </summary>
    public static ViewedMesh<StandardVertex> BuildViewedMesh<TTopology>(Span<StandardVertex> vertices, ReadOnlySpan<Vector3> controlPoints, TrailSettings<TTopology> settings)
         where TTopology : ITrailTopologySetting
    {
        int pointCount = settings.PointCountOverride ?? controlPoints.Length;
        int totalByteCount = CalculateStackByteCount(pointCount, controlPoints.Length);
        bool preferStack = !ExceedsStackAllocationLimit(totalByteCount);
        Span<uint> indices = TopologyData<TTopology>.Instance.IndicesCache.AsSpan(0, TTopology.CalculateIndexCount(pointCount));
        BuildMesh(controlPoints, settings, pointCount, preferStack, vertices, out int primitiveCount);

        return new ViewedMesh<StandardVertex>(TTopology.Type, vertices, indices, primitiveCount);
    }

    /// <summary>
    ///     Configures the stack allocation limit, the byte quantity value at which
    ///     internal caches, such as normals, tangents, vertices, etc.
    ///     get initialized as heap-allocated arrays instead of
    ///     stack-allocated spans.
    ///     
    ///     <br></br>
    ///     The limit is set at <see cref="DefaultStackAllocationLimit"/> by default. Changes applies
    ///     with this method should be <b>very</b> carefully considered.
    /// </summary>
    public static void ConfigureStackAllocationLimit(uint limit) => StackAllocationLimit = limit;

    /// <summary>
    ///     Determines whether a given stack allocation byte quantity
    ///     exceeds the stack allocation limit and should
    ///     thusly allocate on the heap as an array instead.
    /// </summary>
    /// <remarks>
    ///     The limit is set at <see cref="DefaultStackAllocationLimit"/> by default, but may be changed
    ///     with <see cref="ConfigureStackAllocationLimit(int)"/>.
    /// </remarks>
    public static bool ExceedsStackAllocationLimit(int byteCount) => byteCount > StackAllocationLimit;
}
