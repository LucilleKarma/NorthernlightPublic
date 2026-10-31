using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class LargeStarExplosion : Bullet
{
    private float existenceTimer;

    private readonly float scale;

    private readonly Attack attack;

    private readonly float lifetime;

    public LargeStarExplosion(KnightFightScene scene, Attack attack, Vector2 spawnPosition, float scale, float lifetime) : base(scene, 0, 0f, spawnPosition)
    {
        this.attack = attack;
        this.scale = scale;
        this.lifetime = lifetime;
    }

    protected override void UpdateInternal()
    {
        existenceTimer += Scene.Time.DeltaTime;
        if (existenceTimer >= lifetime)
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float lifetimeRatio = existenceTimer / lifetime;
        float pulse = scale * (1f + float.InverseLerp(0f, 0.1f, lifetimeRatio) * 0.25f);
        AtlasEntry star = Atlases.KnightScene.StarExplosion;
        sprites.Enqueue(star, new SpriteData(star, 0, lifetimeRatio >= 0.2f ? 1 : 0, 1, 2)
        {
            Position = DrawPosition,
            Scale = Vector2.One * scale * pulse,
            Tint = Color.White * (1f - lifetimeRatio)
        }, BlendState.AlphaBlend);
    }

    public override bool CollidingWith(RectangleF box)
    {
        RectangleF hurtBox = new RectangleF(Position, Atlases.KnightScene.Star.Area.Size * scale * 0.6f);
        return box.Intersects(hurtBox);
    }
}
