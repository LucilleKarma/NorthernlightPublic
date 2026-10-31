using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Particles;

/// <summary>
///     A visual hit particle that displays in the world
///     when a hero hits a critical strike when attacking.
/// </summary>
public sealed class CriticalParticle
{
    private float timer;

    /// <summary>
    ///     The opacity of this particle.
    /// </summary>
    public float Opacity
    {
        get;
        set;
    } = 1f;

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
        timer += game.Time.DeltaTime * 0.75f;
        Opacity = 1f - timer;
        Position += Velocity * game.Time.DeltaTime;
    }

    /// <summary>
    ///     Prepares this particle for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        int frame = (int)float.Round(timer * 4f);

        AtlasEntry texture = Atlases.KnightScene.CriticalStrikeStar;
        sprites.Enqueue(texture, new SpriteData(texture, 0, frame, 1, 5)
        {
            Position = Position,
            Tint = Color.White * float.Clamp(Opacity, 0f, 1f),
            Scale = Vector2.One * 2f
        });
    }
}
