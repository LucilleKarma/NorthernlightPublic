using System.Numerics;

namespace Northernlight.Core.Mathematics;

/// <summary>
///     Represents an instance of a rectangle in 2D context.
/// </summary>
public readonly struct Rectangle
{
    /// <summary>
    ///     The left X position of this rectangle.
    /// </summary>
    public int X
    {
        get;
    }

    /// <summary>
    ///     The top Y position of this rectangle.
    /// </summary>
    public int Y
    {
        get;
    }

    /// <summary>
    ///     The width of this rectangle.
    /// </summary>
    public int Width
    {
        get;
    }

    /// <summary>
    ///     The height of this rectangle.
    /// </summary>
    public int Height
    {
        get;
    }

    /// <summary>
    ///     The left X position of this rectangle.
    /// </summary>
    public readonly int Left => X;

    /// <summary>
    ///     The top Y position of this rectangle.
    /// </summary>
    public readonly int Top => Y;

    /// <summary>
    ///     The right X position of this rectangle.
    /// </summary>
    public readonly int Right => X + Width;

    /// <summary>
    ///     The bottom Y position of this rectangle.
    /// </summary>
    public readonly int Bottom => Y + Height;

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

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rectangle(float x, float y, float width, float height) :
        this((int)x, (int)y, (int)width, (int)height)
    {
    }

    public Rectangle(Vector2 center, Vector2 size)
    {
        X = (int)(center.X - size.X * 0.5f);
        Y = (int)(center.Y - size.Y * 0.5f);
        Width = (int)size.X;
        Height = (int)size.Y;
    }

    /// <summary>
    ///     Whether this rectangle contains a given point.
    /// </summary>
    public bool Contains(Vector2 position)
    {
        float x = position.X;
        float y = position.Y;
        return X <= x && x < X + Width && Y <= y && y < Y + Height;
    }

    /// <summary>
    ///     Vertically subdivides this rectangle and
    ///     returns the result.
    /// </summary>
    public Rectangle VerticalSubdivide(float topRatio, float bottomRatio)
    {
        int top = (int)float.Round(float.Lerp(Top, Bottom, topRatio)) - 1;
        int height = (int)float.Round(Height * (bottomRatio - topRatio)) + 1;
        return new Rectangle(Left, top, Width, height);
    }

    /// <summary>
    ///     Whether this rectangle intersects with
    ///     another one.
    /// </summary>
    public bool Intersects(Rectangle other)
    {
        return
            other.Left < Right &&
            Left < other.Right &&
            other.Top < Bottom &&
            Top < other.Bottom;
    }
}
