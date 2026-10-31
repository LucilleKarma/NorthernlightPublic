using System.Numerics;

namespace Northernlight.Core.Mathematics;

/// <summary>
///     Represents an instance of a triangle.
/// </summary>
public readonly struct Triangle
{
    /// <summary>
    ///     The first point.
    /// </summary>
    public readonly Vector2 A;

    /// <summary>
    ///     The second point.
    /// </summary>
    public readonly Vector2 B;

    /// <summary>
    ///     The third point.
    /// </summary>
    public readonly Vector2 C;

    public Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        A = a;
        B = b;
        C = c;
    }

    public Vector2 this[int i]
    {
        get
        {
            return i switch
            {
                1 => B,
                2 => C,
                _ => A,
            };
        }
    }
}
