using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class StarPart : Bullet
{
    private float existenceTimer;

    private readonly float scale;

    private readonly float lifetime;

    /// <summary>
    ///     This star part's hurtbox.
    /// </summary>
    public RectangleF HurtBox => new RectangleF(Position, Atlases.KnightScene.StarPart.Area.Size * scale * 0.8f);

    public StarPart(KnightFightScene scene, Vector2 spawnPosition, Vector2 velocity, int damage, float grazePoints, float scale, float lifetime) : base(scene, damage, grazePoints, spawnPosition, velocity)
    {
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
        float pulse = 1f + float.Cos(float.Tau * existenceTimer / lifetime * 40f) * 0.2f;
        float opacity = float.InverseLerp(1f, 0.6f, existenceTimer / lifetime);
        AtlasEntry star = Atlases.KnightScene.StarPart;
        sprites.Enqueue(star, new SpriteData(star)
        {
            Position = DrawPosition,
            Scale = Vector2.One * scale,
            Tint = Color.White * opacity,
            Rotation = Velocity
        }, BlendState.NonPremultiplied);
        sprites.Enqueue(star, new SpriteData(star)
        {
            Position = DrawPosition,
            Scale = Vector2.One * scale * pulse,
            Tint = Color.White * opacity,
            Rotation = Velocity
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box) => box.Intersects(HurtBox) && existenceTimer < lifetime * 0.7f;
}
