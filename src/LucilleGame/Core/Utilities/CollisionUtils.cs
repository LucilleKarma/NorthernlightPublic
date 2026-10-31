using System.Numerics;

namespace Northernlight.Core.Utilities;

/// <summary>
///     A simple service provider for complex
///     forms of collision logic.
/// </summary>
public static class CollisionUtils
{
    /// <summary>
    ///     Evaluates and returns the distance to the closest
    ///     meeting points for two line segments.
    /// </summary>
    public static float CalculateSegmentSegmentDistance(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        Vector2 d1 = q1 - p1;
        Vector2 d2 = q2 - p2;
        Vector2 r = p1 - p2;

        float a = Vector2.Dot(d1, d1);
        float e = Vector2.Dot(d2, d2);
        float f = Vector2.Dot(d2, r);

        float s;
        float t;

        if (a <= float.Epsilon && e <= float.Epsilon)
            return Vector2.Distance(p1, p2);

        if (a <= float.Epsilon)
        {
            s = 0f;
            t = float.Clamp(f / e, 0f, 1f);
        }
        else
        {
            float c = Vector2.Dot(d1, r);
            if (e <= float.Epsilon)
            {
                t = 0f;
                s = float.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                float b = Vector2.Dot(d1, d2);
                float denominator = a * e - b * b;

                if (denominator != 0f)
                    s = float.Clamp((b * f - c * e) / denominator, 0f, 1f);
                else
                    s = 0f;

                t = float.Clamp((b * s + f) / e, 0f, 1f);
                s = float.Clamp((b * t - c) / a, 0f, 1f);
            }
        }

        Vector2 c1 = p1 + d1 * s;
        Vector2 c2 = p2 + d2 * t;
        return Vector2.Distance(c1, c2);
    }
}
