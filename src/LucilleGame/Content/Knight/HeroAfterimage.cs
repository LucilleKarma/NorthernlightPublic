using System.Numerics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents an instance of a fading
///     afterimage that a hero can emit.
/// </summary>
public sealed class HeroAfterimage
{
    private float time;

    private Vector3 position;

    private Vector3 velocity;

    private readonly float maxOpacity;

    private readonly float lifetime;

    private readonly HeroAnimation heroAnimation;

    /// <summary>
    ///     Whether this afterimage should be removed due
    ///     to exceeding its natural lifetime.
    /// </summary>
    public bool Dead => time >= lifetime;

    public HeroAfterimage(HeroAnimation animation, Vector3 position, Vector3 velocity, float lifetime, float maxOpacity)
    {
        heroAnimation = animation;
        this.position = position;
        this.velocity = velocity;
        this.maxOpacity = maxOpacity;
        this.lifetime = lifetime;
    }

    /// <summary>
    ///     Updates this afterimage.
    /// </summary>
    public void Update(KnightFightScene scene)
    {
        time += scene.Time.DeltaTime;
        position += velocity * scene.Time.DeltaTime;
    }

    /// <summary>
    ///     Prepares this afterimage for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        Color color = Color.White * (1f - time / lifetime) * maxOpacity;
        heroAnimation.Render(sprites, position, color);
    }
}
