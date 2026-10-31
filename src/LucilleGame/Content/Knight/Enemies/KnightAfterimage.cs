using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using static Northernlight.Content.Knight.Enemies.RoaringKnight;

namespace Northernlight.Content.Knight.Enemies;

/// <summary>
///     Represents an instance of an afterimage
///     the Knight can produce.
/// </summary>
public sealed class KnightAfterimage
{
    internal float timer;

    /// <summary>
    ///     Whether this afterimage should render
    ///     additively or not.
    /// </summary>
    public bool Additive
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     Whether this afterimage should be visually flipped.
    /// </summary>
    public bool HorizontallyFlip
    {
        get;
        set;
    }

    /// <summary>
    ///     The draw position of this afterimage.
    /// </summary>
    public Vector3 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     The velocity of this afterimage.
    /// </summary>
    public Vector3 Velocity
    {
        get;
        set;
    }

    /// <summary>
    ///     The scale of this afterimage.
    /// </summary>
    public Vector2 Scale
    {
        get;
        set;
    }

    /// <summary>
    ///     How long this afterimage should exist
    ///     for, in frames.
    /// </summary>
    public float Lifetime
    {
        get;
        set;
    }

    /// <summary>
    ///     This afterimage's currently used render state.
    /// </summary>
    public KnightRenderState RenderState
    {
        get;
        set;
    }

    /// <summary>
    ///     Updates this afterimage.
    /// </summary>
    public void Update(Time time)
    {
        Position += Velocity * time.DeltaTime;
        timer += time.DeltaTime;
    }

    /// <summary>
    ///     Prepares this afterimage for rendering.
    /// </summary>
    public void Prepare(SpriteRenderer sprites, Color tint)
    {
        float opacity = float.Pow(1f - timer / Lifetime, 1.3f) * 0.67f;
        if (!Additive)
            opacity *= 0.7f;

        sprites.Enqueue(RenderState.Texture, new SpriteData(RenderState.Texture, 0, RenderState.CurrentFrame, 1, RenderState.TotalFrames) with
        {
            Position = Position - Vector3.UnitZ * 20f,
            Scale = Scale * new Vector2(HorizontallyFlip ? -1f : 1f, 1f) * 2f,
            Tint = tint * opacity,
        }, Additive ? BlendState.Additive : BlendState.AlphaBlend, drawToDepthBuffer: false);
    }
}
