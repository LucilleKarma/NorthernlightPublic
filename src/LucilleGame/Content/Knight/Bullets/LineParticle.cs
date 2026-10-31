using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class LineParticle : Bullet
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

    public LineParticle(KnightFightScene scene, float lifetime, Vector2 spawnPosition, Vector2 velocity) : base(scene, 0, 0f, spawnPosition, velocity)
    {
        Lifetime = lifetime;
        timer = scene.Time.Rent();
    }

    protected override void UpdateInternal()
    {
        Velocity = Vector2.LerpDt(Velocity, Vector2.Zero, 50f, Scene.Time);

        if (timer.Elapsed(Lifetime))
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float lifetimeRatio = timer / Lifetime;
        Vector2 scale = new Vector2(1f, (1f - lifetimeRatio) * 500f);
        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        Color color = Color.Lerp(new Color(255, 0, 0), Color.White * 0.9f, lifetimeRatio.Squared());
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = DrawPosition,
            Scale = scale / new Vector2(1f, pixel.Area.Height) * new Vector2(1f, 0.75f),
            Tint = color,
            Rotation = Velocity + Angle.PiOver2
        }, BlendState.AlphaBlend, drawToDepthBuffer: false);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
