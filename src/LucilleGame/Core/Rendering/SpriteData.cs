using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of sprite data that can be
///     pushed into a sprite queue.
/// </summary>
public struct SpriteData
{
    /// <summary>
    ///     The position of this sprite in
    ///     world space.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    ///     The pivot point origin of this
    ///     sprite.
    /// </summary>
    public Vector2 Origin;

    /// <summary>
    ///     The scale of this sprite.
    /// </summary>
    public Vector2 Scale;

    /// <summary>
    ///     The tint color of this sprite.
    /// </summary>
    public Color Tint = Color.White;

    /// <summary>
    ///     The frame of this sprite.
    /// </summary>
    public Rectangle Frame;

    /// <summary>
    ///     The rotation of this sprite.
    /// </summary>
    public Angle Rotation;

    public SpriteData(Texture texture)
    {
        Origin = texture.Size * 0.5f;
        Frame = new Rectangle(0, 0, (int)texture.Width, (int)texture.Height);
    }

    public SpriteData(AtlasEntry texture)
    {
        Origin = texture.Area.Size * 0.5f;
        Frame = texture.Area;
    }

    public SpriteData(AtlasEntry texture, int frameX, int frameY, int totalFramesX, int totalFramesY, int horizontalOffset = 0, int verticalOffset = 0)
    {
        Rectangle baseArea = texture.Area;
        int width = baseArea.Width / totalFramesX;
        int height = baseArea.Height / totalFramesY;

        Frame = new Rectangle(baseArea.Left + width * frameX + horizontalOffset, baseArea.Top + height * frameY + verticalOffset, width, height);
        Origin = Frame.Size * 0.5f;
    }
}
