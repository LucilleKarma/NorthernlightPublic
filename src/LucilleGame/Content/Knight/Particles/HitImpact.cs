using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Particles;

/// <summary>
///     A visual hit particle that displays in the world
///     when the Knight is attacked by one of the heroes.
/// </summary>
public sealed class HitImpact
{
    private OwnedTimer timer;

    /// <summary>
    ///     The current frame of this impact.
    /// </summary>
    public int Frame
    {
        get;
        set;
    }

    /// <summary>
    ///     This impact's position in the world.
    /// </summary>
    public Vector3 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of frames this impact has.
    /// </summary>
    public required int TotalFrames
    {
        get;
        init;
    }

    /// <summary>
    ///     This particle's texture.
    /// </summary>
    public required AtlasEntry Texture
    {
        get;
        init;
    }

    /// <summary>
    ///     Updates this impact.
    /// </summary>
    public void Update(Game game)
    {
        timer ??= game.Time.Rent();

        if (timer.Cycle(0.1f))
            Frame++;
    }

    /// <summary>
    ///     Prepares this impact for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        if (TotalFrames == 0)
            return;

        sprites.Enqueue(Texture, new SpriteData(Texture, 0, Frame, 1, TotalFrames)
        {
            Position = Position,
            Tint = Color.White,
            Scale = Vector2.One * 1.9f
        });
    }
}
