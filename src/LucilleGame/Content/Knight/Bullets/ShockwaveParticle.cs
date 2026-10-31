using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Bullets;

public sealed class ShockwaveParticle : Bullet
{
    private readonly OwnedTimer timer;

    /// <summary>
    ///     How long this particle should exist for.
    /// </summary>
    public float Lifetime
    {
        get;
        set;
    }

    /// <summary>
    ///     The maximum scale of this particle.
    /// </summary>
    public float MaxScale
    {
        get;
        set;
    }

    /// <summary>
    ///     The base color of this shockwave.
    /// </summary>
    public Color BaseColor
    {
        get;
        set;
    } = Color.White;

    public ShockwaveParticle(KnightFightScene scene, float lifetime, float maxScale, Vector2 spawnPosition) : base(scene, 0, 0f, spawnPosition)
    {
        Lifetime = lifetime;
        MaxScale = maxScale;
        timer = scene.Time.Rent();
    }

    protected override void UpdateInternal()
    {
        if (timer.Elapsed(Lifetime))
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float lifetimeRatio = timer / Lifetime;
        Vector2 scale = Vector2.One * float.Pow(lifetimeRatio, 1.25f) * MaxScale;
        AtlasEntry wave = Atlases.KnightScene.Shockwave;
        Color color = BaseColor * float.Sqrt(1f - lifetimeRatio);
        sprites.Enqueue(wave, new SpriteData(wave)
        {
            Position = DrawPosition,
            Scale = scale / 400f,
            Tint = color
        }, BlendState.AlphaBlend);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
