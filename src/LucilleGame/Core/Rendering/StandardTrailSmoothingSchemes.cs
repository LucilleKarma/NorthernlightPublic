using System.Numerics;
using Northernlight.Core.Utilities;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Provides selections of basic trail smoothing
///     schemes that can be used to determine how a trail's
///     control points should be smoothened.
/// </summary>
public abstract class StandardTrailSmoothingSchemes
{
    public delegate void SmoothingScheme(ReadOnlySpan<Vector3> controlPoints, Span<Vector3> rentedOutputArray, TransientTrailBuildState state);

    /// <summary>
    ///     A simple scheme that just returns the base control
    ///     points without any smoothing at all. If the point count
    ///     varies from the amount of control points, a simple
    ///     linear spline is used.
    /// </summary>
    public static readonly SmoothingScheme None = NoneFunction;

    private static void NoneFunction(ReadOnlySpan<Vector3> controlPoints, Span<Vector3> rentedOutputArray, TransientTrailBuildState state)
    {
        int pointCount = rentedOutputArray.Length;
        if (pointCount == controlPoints.Length)
        {
            controlPoints.CopyTo(rentedOutputArray);
            return;
        }

        int controlsCount = controlPoints.Length;
        for (int i = 0; i < pointCount; i++)
        {
            float pointProgress = i / (float)(pointCount - 1f);
            int currentIndex = (int)(pointProgress * (controlsCount - 1));
            Vector3 currentPoint = controlPoints[currentIndex];
            Vector3 nextPoint = controlPoints[int.Min(currentIndex + 1, controlsCount - 1)];
            rentedOutputArray[i] = Vector3.Lerp(currentPoint, nextPoint, pointProgress * (controlsCount - 1) % 0.99999f);
        }
    }

    /// <summary>
    ///     A scheme that creates intermediate points from the controls
    ///     based on a Hermite polynomial.
    /// </summary>
    public static readonly SmoothingScheme HermiteInterpolation = HermiteInterpolationFunction;

    private static void HermiteInterpolationFunction(ReadOnlySpan<Vector3> controlPoints, Span<Vector3> rentedOutputArray, TransientTrailBuildState state)
    {
        int pointCount = rentedOutputArray.Length;
        if (pointCount == 1)
        {
            rentedOutputArray[0] = controlPoints[0];
            return;
        }

        int lastControlIndex = controlPoints.Length - 1;
        for (int i = 0; i < pointCount; i++)
        {
            float pointProgress = i / (float)(pointCount - 1f);
            float startContinuous = pointProgress * lastControlIndex;

            int startDiscrete = (int)float.Floor(startContinuous);

            int startIndex = int.Clamp(startDiscrete, 0, int.Max(0, lastControlIndex - 1));
            int endIndex = startIndex + 1;

            float fractionalInterpolant = startContinuous - startIndex;

            Vector3 p0 = controlPoints[startIndex];
            Vector3 p1 = controlPoints[endIndex];
            Vector3 m0 = state.UnnormalizedTrailTangents[startIndex];
            Vector3 m1 = state.UnnormalizedTrailTangents[endIndex];

            rentedOutputArray[i] = Vector3.Hermite(p0, m0, p1, m1, fractionalInterpolant);
        }
    }
}
