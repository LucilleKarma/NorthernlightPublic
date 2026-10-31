using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Particles;

/// <summary>
///     A visual hit particle that displays in the world
///     when the Knight is attacked by Susie during her
///     clash state.
/// </summary>
public sealed class ClashParticle
{
    private OwnedTimer timer;

    /// <summary>
    ///     Whether this particle is dead or not.
    /// </summary>
    public bool Dead => timer >= 0.45f || Frame >= TotalFrames;

    /// <summary>
    ///     The current frame of this impact.
    /// </summary>
    public int Frame
    {
        get;
        set;
    }

    /// <summary>
    ///     The rotation of this particle.
    /// </summary>
    public Angle Rotation
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
    ///     The velocity of this impact particle.
    /// </summary>
    public Vector3 Velocity
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this particle animtes or not.
    /// </summary>
    public required bool Animates
    {
        get;
        init;
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
        Position += Velocity * game.Time.DeltaTime;
        Velocity = Vector3.LerpDt(Velocity, Vector3.Zero, 0.2f, game.Time);

        timer ??= game.Time.Rent();

        if (Animates && timer.Cycle(0.1333f))
            Frame++;
    }

    /// <summary>
    ///     Prepares this impact for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        if (TotalFrames == 0)
            return;

        float opacity = 1f;
        float scale = 1.85f;
        if (!Animates)
        {
            scale *= 1.5f;
            opacity = float.InverseLerp(0.45f, 0f, timer);
        }

        sprites.Enqueue(Texture, new SpriteData(Texture, Frame, 0, TotalFrames, 1)
        {
            Position = Position,
            Tint = Color.White * opacity,
            Scale = Vector2.One * scale,
            Rotation = Rotation
        });
    }
}
