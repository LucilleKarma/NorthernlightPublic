using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class Star : Bullet
{
    private Angle rotation;

    private float existenceTimer;

    private float opacity;

    public Star(KnightFightScene scene, Vector2 spawnPosition, Vector2 velocity, int damage, float grazePoints) : base(scene, damage, grazePoints, spawnPosition, velocity)
    {
        rotation = velocity;
    }

    protected override void UpdateInternal()
    {
        Vector2 lockedPosition = Scene.BattleHandler.Box.KeepInside(Position, Atlases.KnightScene.StarBullet.Area.Size * 0.2f);
        if (Position != lockedPosition)
        {
            Position = lockedPosition;
            Velocity = Vector2.Zero;
        }

        if (Velocity.Length() >= 1f)
            rotation = Velocity;

        existenceTimer += Scene.Time.DeltaTime;
        opacity = float.InverseLerp(2.5f, 1.8f, existenceTimer);
        if (opacity <= 0f)
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry cut = Atlases.KnightScene.StarBullet;
        sprites.Enqueue(cut, new SpriteData(cut)
        {
            Position = DrawPosition,
            Scale = Vector2.One * 0.3f,
            Tint = Color.White * opacity,
            Rotation = rotation
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box)
    {
        RectangleF hurtBox = new RectangleF(Position, Vector2.One * 16f);
        return opacity >= 0.65f && box.Intersects(hurtBox);
    }
}
