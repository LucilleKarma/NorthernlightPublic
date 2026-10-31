using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Bullets;

public sealed class RedFlurryImpactParticle : Bullet
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

    public RedFlurryImpactParticle(KnightFightScene scene, float lifetime, Vector2 spawnPosition, Vector2 velocity) : base(scene, 0, 0f, spawnPosition, velocity)
    {
        Lifetime = lifetime;
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
        Vector2 scale = new Vector2((1f - lifetimeRatio) * 1f, 0.3f) * 0.4f;
        AtlasEntry pixel = Atlases.KnightScene.SlashMarker;
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = DrawPosition,
            Scale = scale,
            Tint = new Color(255, 0, 0),
            Rotation = Velocity
        }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
