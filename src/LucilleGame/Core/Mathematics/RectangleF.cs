using System.Numerics;

namespace Northernlight.Core.Mathematics;

/// <summary>
///     Represents an instance of a floating point
///     rectangle in 2D context.
/// </summary>
public readonly struct RectangleF
{
    /// <summary>
    ///     The left X position of this rectangle.
    /// </summary>
    public float X
    {
        get;
    }

    /// <summary>
    ///     The top Y position of this rectangle.
    /// </summary>
    public float Y
    {
        get;
    }

    /// <summary>
    ///     The width of this rectangle.
    /// </summary>
    public float Width
    {
        get;
    }

    /// <summary>
    ///     The height of this rectangle.
    /// </summary>
    public float Height
    {
        get;
    }

    /// <summary>
    ///     The left X position of this rectangle.
    /// </summary>
    public readonly float Left => X;

    /// <summary>
    ///     The top Y position of this rectangle.
    /// </summary>
    public readonly float Top => Y;

    /// <summary>
    ///     The right X position of this rectangle.
    /// </summary>
    public readonly float Right => X + Width;

    /// <summary>
    ///     The bottom Y position of this rectangle.
    /// </summary>
    public readonly float Bottom => Y + Height;

    /// <summary>
    ///     The top left position of this rectangle.
    /// </summary>
    public Vector2 TopLeft => new Vector2(Left, Top);

    /// <summary>
    ///     The top right position of this rectangle.
    /// </summary>
    public Vector2 TopRight => new Vector2(Right, Top);

    /// <summary>
    ///     The bottom left position of this rectangle.
    /// </summary>
    public Vector2 BottomLeft => new Vector2(Left, Bottom);

    /// <summary>
    ///     The bottom right position of this rectangle.
    /// </summary>
    public Vector2 BottomRight => new Vector2(Right, Bottom);

    /// <summary>
    ///     The centered position of this rectangle.
    /// </summary>
    public Vector2 Center => TopLeft + Size * 0.5f;

    /// <summary>
    ///     The size of this rectangle.
    /// </summary>
    public readonly Vector2 Size => new Vector2(Width, Height);

    public RectangleF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public RectangleF(Vector2 center, Vector2 size)
    {
        X = center.X - size.X * 0.5f;
        Y = center.Y - size.Y * 0.5f;
        Width = size.X;
        Height = size.Y;
    }

    /// <summary>
    ///     Whether this rectangle contains a given pofloat.
    /// </summary>
    public bool Contains(Vector2 position)
    {
        float x = position.X;
        float y = position.Y;
        return X <= x && x < X + Width && Y <= y && y < Y + Height;
    }

    /// <summary>
    ///     Whether this rectangle floatersects with
    ///     another one.
    /// </summary>
    public bool Intersects(RectangleF other)
    {
        return
            other.Left < Right &&
            Left < other.Right &&
            other.Top < Bottom &&
            Top < other.Bottom;
    }
}
