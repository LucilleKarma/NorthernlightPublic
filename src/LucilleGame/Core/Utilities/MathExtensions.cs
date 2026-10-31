using System.Numerics;
using Northernlight.Core.Mathematics;

namespace Northernlight.Core.Utilities;

/// <summary>
///     A simple collection of math extension utility methods.
/// </summary>
public static class MathExtensions
{
    /// <summary>
    ///     Squares a value and returns the result.
    /// </summary>
    public static float Squared(this float x) => x * x;

    /// <summary>
    ///     Cubes a value and returns the result.
    /// </summary>
    public static float Cubed(this float x) => x * x * x;

    extension(float)
    {
        /// <summary>
        ///     Converts a 0-1 interpolant to a quadratic 0-1-0 bump range.
        /// </summary>
        public static float Bump(float x)
        {
            x = float.Clamp(x, 0f, 1f);
            return (x - 1f) * x * -4f;
        }

        /// <summary>
        ///     Applies a linear interpolation between
        ///     two values in a manner that adheres to and
        ///     mathematically respects the time step.
        /// </summary>
        public static float LerpDt(float a, float b, float sharpness, Time time)
        {
            float interpolant = 1f - float.Pow(0.5f, time.DeltaTime * sharpness);
            return float.Lerp(a, b, interpolant);
        }

        /// <summary>
        ///     The inverse of <see cref="float.Lerp(float, float, float)"/>.
        /// </summary>
        public static float InverseLerp(float from, float to, float x, bool clamp = true)
        {
            bool dontDivideByZeroPlease = float.Abs(to - from) < float.Epsilon;
            if (dontDivideByZeroPlease)
                return 0f;

            float interpolant = (x - from) / (to - from);
            if (!clamp)
                return interpolant;

            return float.Clamp(interpolant, 0f, 1f);
        }

        /// <summary>
        ///     Performs a linear bump across a spectrum of two in/out values.
        /// </summary>
        /// <param name="start1">The value at which the output should rise from 0 to 1.</param>
        /// <param name="start2">The value at which the output start bumping at 1.</param>
        /// <param name="end1">The value at which the output cease bumping at 1.</param>
        /// <param name="end2">The value at which the output should descent from 1 to 0.</param>
        /// <param name="x">The input interpolant.</param>
        /// <returns>
        ///     0 when <paramref name="x"/> is less than or equal to <paramref name="start1"/>.
        ///     <br></br>
        ///     Anywhere between 0 and 1, ascending, when <paramref name="x"/> is greater than <paramref name="start1"/> but less than <paramref name="start2"/>.
        ///     <br></br>
        ///     1 when <paramref name="x"/> is between <paramref name="start2"/> and <paramref name="end1"/>.
        ///     <br></br>
        ///     Anywhere between 0 and 1, descending, when <paramref name="x"/> is greater than <paramref name="end1"/> but less than <paramref name="end2"/>.
        ///     <br></br>
        ///     0 when <paramref name="x"/> is greater than or equal to <paramref name="end2"/>.
        /// </returns>
        public static float InverseLerpBump(float start1, float start2, float end1, float end2, float x) =>
            float.InverseLerp(start1, start2, x) * float.InverseLerp(end2, end1, x);

        /// <summary>
        ///     Returns a sine with a 0-1 range instead of a -1 to 1 range.
        /// </summary>
        public static float Sin01(float x) => float.Clamp(float.Sin(x) * 0.5f + 0.5f, 0f, 1f);

        /// <summary>
        ///     Returns a cosine with a 0-1 range instead of a -1 to 1 range.
        /// </summary>
        public static float Cos01(float x) => float.Clamp(float.Cos(x) * 0.5f + 0.5f, 0f, 1f);

        /// <summary>
        ///     Calculates the result of a Hermite spline from two input
        ///     points and two tangents.
        /// </summary>
        public static float Hermite(float a, float tangentA, float b, float tangentB, float interpolant)
        {
            if (interpolant <= 0f)
                return a;
            if (interpolant >= 1f)
                return b;

            float sSquared = interpolant * interpolant;
            float sCubed = sSquared * interpolant;
            float result =
                    ((a * 2f - b * 2f + tangentB + tangentA) * sCubed) +
                    ((b * 3f - a * 3f - tangentA * 2f - tangentB) * sSquared) +
                    (tangentA * interpolant) +
                    a;

            return result;
        }

        /// <summary>
        ///     Calculates the result of a Hermite spline from two input
        ///     points and two tangents in 2D space.
        /// </summary>
        public static Vector2 Hermite(Vector2 a, Vector2 tangentA, Vector2 b, Vector2 tangentB, float interpolant)
        {
            float x = float.Hermite(a.X, tangentA.X, b.X, tangentB.X, interpolant);
            float y = float.Hermite(a.Y, tangentA.Y, b.Y, tangentB.Y, interpolant);
            return new Vector2(x, y);
        }

        /// <summary>
        ///     Calculates the result of a Hermite spline from two input
        ///     points and two tangents in 3D space.
        /// </summary>
        public static Vector3 Hermite(Vector3 a, Vector3 tangentA, Vector3 b, Vector3 tangentB, float interpolant)
        {
            float x = float.Hermite(a.X, tangentA.X, b.X, tangentB.X, interpolant);
            float y = float.Hermite(a.Y, tangentA.Y, b.Y, tangentB.Y, interpolant);
            float z = float.Hermite(a.Z, tangentA.Z, b.Z, tangentB.Z, interpolant);
            return new Vector3(x, y, z);
        }

        /// <summary>
        ///     Interpolates between three <see cref="Vector2"/>-based points via a quadratic Bezier spline.
        /// </summary>
        public static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float interpolant)
        {
            Vector2 firstTerm = (1f - interpolant).Squared() * a;
            Vector2 secondTerm = (1f - interpolant) * interpolant * b * 2f;
            Vector2 thirdTerm = interpolant.Squared() * c;

            return firstTerm + secondTerm + thirdTerm;
        }

        /// <summary>
        ///     Applies a smoothstep curve to a given value.
        /// </summary>
        public static float Smoothstep(float x)
        {
            x = float.Clamp(x, 0f, 1f);

            float xSquared = x.Squared();
            float xCubed = xSquared * x;
            return xSquared * 3f - xCubed * 2f;
        }

        /// <summary>
        ///     Applies a smoothstep interpolation to a given value range.
        /// </summary>
        public static float Smoothstep(float a, float b, float interpolant)
        {
            interpolant = Smoothstep(interpolant);
            return float.Lerp(a, b, interpolant);
        }
    }

    extension(Vector2)
    {
        /// <summary>
        ///     Applies a linear interpolation between
        ///     two 2D vectors in a manner that adheres to and
        ///     mathematically respects the time step.
        /// </summary>
        public static Vector2 LerpDt(Vector2 a, Vector2 b, float sharpness, Time time)
        {
            float x = float.LerpDt(a.X, b.X, sharpness, time);
            float y = float.LerpDt(a.Y, b.Y, sharpness, time);
            return new Vector2(x, y);
        }
    }

    extension(Vector3)
    {
        /// <summary>
        ///     Applies a linear interpolation between
        ///     two 3D vectors in a manner that adheres to and
        ///     mathematically respects the time step.
        /// </summary>
        public static Vector3 LerpDt(Vector3 a, Vector3 b, float sharpness, Time time)
        {
            float x = float.LerpDt(a.X, b.X, sharpness, time);
            float y = float.LerpDt(a.Y, b.Y, sharpness, time);
            float z = float.LerpDt(a.Z, b.Z, sharpness, time);
            return new Vector3(x, y, z);
        }

        /// <summary>
        ///     Applies the Rodrigues Rotation Formula of a given rotation on a given axis and vector.
        /// </summary>
        /// <remarks>
        ///     It is generally expected that the axis should be
        ///     a normalized vector when using this method.
        /// </remarks>
        public static Vector3 RodriguesRotation(Vector3 axis, Vector3 v, float theta)
        {
            float cosTheta = float.Cos(theta);
            float sinTheta = float.Sin(theta);
            return v * cosTheta + Vector3.Cross(axis, v) * sinTheta + axis * Vector3.Dot(axis, v) * (1f - cosTheta);
        }
    }

    /// <summary>
    ///     Converts a given angle to its corresponding
    ///     unit vector.
    /// </summary>
    public static Vector2 ToUnit(this float theta)
    {
        (float sin, float cos) = float.SinCos(theta);
        return new Vector2(cos, sin);
    }

    /// <summary>
    ///     Rotates a 2D given vector by a given angle.
    /// </summary>
    public static Vector2 RotatedBy(this Vector2 v, Angle theta)
    {
        (float sin, float cos) = float.SinCos(theta);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    /// <summary>
    ///     Safely normalizes a 2D vector, using a fallback if
    ///     anything would result NaNs or similar due to division
    ///     by zero, and returns the result.
    /// </summary>
    public static Vector2 SafeNormalize(this Vector2 v, Vector2 fallback = default)
    {
        float length = v.Length();
        if (length < 1e-7f)
            return fallback;

        return v / length;
    }

    /// <summary>
    ///     Safely normalizes a 3D vector, using a fallback if
    ///     anything would result NaNs or similar due to division
    ///     by zero, and returns the result.
    /// </summary>
    public static Vector3 SafeNormalize(this Vector3 v, Vector3 fallback = default)
    {
        float length = v.Length();
        if (length < 1e-7f)
            return fallback;

        return v / length;
    }

    /// <summary>
    ///     Generates a random 1/N roll.
    /// </summary>
    public static bool NextBool(this Random rng, int n = 2) => rng.Next(n) == 0;

    /// <summary>
    ///     Generates a random probability roll.
    /// </summary>
    public static bool NextBool(this Random rng, float probability) => rng.NextFloat() < probability;

    /// <summary>
    ///     Generates a random value in the range of [0, 1).
    /// </summary>
    public static float NextFloat(this Random rng) => (float)rng.NextDouble();

    /// <summary>
    ///     Generates a random circular vector with a
    ///     randomized radius, up to a point.
    /// </summary>
    public static Vector2 NextCircular(this Random rng, float maxRadius)
    {
        float theta = rng.NextFloat(float.Tau);
        float radius = float.Sqrt(rng.NextFloat()) * maxRadius;
        return theta.ToUnit() * radius;
    }

    /// <summary>
    ///     Generates a random circular vector with a
    ///     fixed radius.
    /// </summary>
    public static Vector2 NextCircularEdge(this Random rng, float radius)
    {
        float theta = rng.NextFloat(float.Tau);
        return theta.ToUnit() * radius;
    }

    /// <summary>
    ///     Generates a random value in the range of [0, max).
    /// </summary>
    public static float NextFloat(this Random rng, float max) => rng.NextFloat() * max;

    /// <summary>
    ///     Generates a random value between a range of two values.
    /// </summary>
    public static float NextFloat(this Random rng, float min, float max) => rng.NextFloat() * (max - min) + min;

    /// <summary>
    ///     Gives the <b>real</b> modulo of a divided by a divisor.
    ///     This method is necessary because the % operator in C# keeps the sign of the dividend.
    /// </summary>
    public static float Modulo(this float dividend, float divisor)
    {
        return dividend - float.Floor(dividend / divisor) * divisor;
    }

    /// <summary>
    ///     Calculates the quadratic Y position from an input X position
    ///     and three control points that the resulting polynomial
    ///     must pass through.
    /// </summary>
    public static float QuadraticInterpolation(Vector2 p1, Vector2 p2, Vector2 p3, float x)
    {
        // https://en.wikipedia.org/wiki/Newton_polynomial
        float c0 = p1.Y;
        float c1 = (p2.Y - p1.Y) / (p2.X - p1.X);
        float c2 = ((p3.Y - p2.Y) / (p3.X - p2.X) - c1) / (p3.X - p1.X);

        return c0 + c1 * (x - p1.X) + c2 * (x - p1.X) * (x - p2.X);
    }

    /// <summary>
    ///     Samples a random value from a Gaussian distribution.
    /// </summary>
    /// <param name="rng">The RNG to use for sampling.</param>
    /// <param name="standardDeviation">The standard deviation of the distribution.</param>
    /// <param name="mean">The mean of the distribution. Used for horizontally shifting the overall resulting graph.</param>
    public static float NextGaussian(this Random rng, float standardDeviation = 1f, float mean = 0f)
    {
        // Refer to the following link for an explanation of why this works:
        // https://blog.cupcakephysics.com/computational%20physics/2015/05/10/the-box-muller-algorithm.html
        float randomAngle = rng.NextFloat(float.Tau);
        float distributionInterpolant = rng.NextFloat(1e-6f, 1f);

        return float.Sqrt(float.Log(distributionInterpolant) * -2f) * float.Cos(randomAngle) * standardDeviation + mean;
    }

    /// <summary>
    ///     Generates a random point on a sphere's surface.
    /// </summary>
    public static Vector3 NextVector3Spherical(this Random rng)
    {
        float x = rng.NextGaussian();
        float y = rng.NextGaussian();
        float z = rng.NextGaussian();
        float lengthSquared = x.Squared() + y.Squared() + z.Squared();
        if (lengthSquared <= float.Epsilon)
            return Vector3.UnitZ;

        float inverseLength = 1f / float.Sqrt(lengthSquared);
        return new Vector3(x, y, z) * inverseLength;
    }
}
