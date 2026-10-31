using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Particles;

/// <summary>
///     A visual hit particle that displays in the world
///     when a hero is healed.
/// </summary>
public sealed class HealParticle
{
    private float timer;

    /// <summary>
    ///     The opacity of this particle.
    /// </summary>
    public float Opacity
    {
        get;
        set;
    } = 2f;

    /// <summary>
    ///     This particle's rotation.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }

    /// <summary>
    ///     This particle's position in the world.
    /// </summary>
    public Vector3 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     This particle's velocity.
    /// </summary>
    public Vector3 Velocity
    {
        get;
        set;
    }

    /// <summary>
    ///     Updates this particle.
    /// </summary>
    public void Update(Game game)
    {
        timer += game.Time.DeltaTime;

        if (timer >= 0.0833f)
            Opacity -= game.Time.DeltaTime * 6f;
        Position += Velocity * game.Time.DeltaTime;
    }

    /// <summary>
    ///     Prepares this particle for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        float lifetimeProgress = Opacity * 0.5f;
        int frame = (int)float.Round(lifetimeProgress * 6f);

        AtlasEntry texture = Atlases.KnightScene.SweetSweetHealParticle;
        sprites.Enqueue(texture, new SpriteData(texture, frame, 0, 6, 1)
        {
            Position = Position,
            Tint = new Color(0, 255, 0) * float.Clamp(Opacity, 0f, 1f),
            Scale = Vector2.One,
            Rotation = Rotation
        });
    }
}
