using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class Wave : Bullet
{
    private float existenceTimer;

    private readonly Attack attack;

    /// <summary>
    ///     The general scale factor of this wave.
    /// </summary>
    public float ScaleFactor
    {
        get;
        set;
    } = 1f;

    public Wave(KnightFightScene scene, Attack attack, Vector2 spawnPosition, Vector2 velocity, int damage, float grazePoints) : base(scene, damage, grazePoints, spawnPosition, velocity)
    {
        this.attack = attack;
    }

    protected override void UpdateInternal()
    {
        existenceTimer += Scene.Time.DeltaTime;
        if (existenceTimer >= 5f)
            Kill();

        if (existenceTimer >= 0.72f)
        {
            for (int i = 0; i < attack.Bullets.Count; i++)
            {
                if (attack.Bullets[i] is LargeStar star)
                {
                    if (CollidingWith(star.HurtBox))
                    {
                        star.Explode();
                        star.Kill();
                    }
                }
            }
        }

        float acceleration = float.InverseLerp(0.25f, 2f, existenceTimer) * 7600f;
        Velocity += Velocity.SafeNormalize(Vector2.Zero) * Scene.Time.DeltaTime * acceleration;
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry wave = Atlases.KnightScene.Wave;
        float stretch = Velocity.Length() / 3301f;

        sprites.Enqueue(wave, new SpriteData(wave)
        {
            Position = DrawPosition,
            Scale = new Vector2(1f - stretch, 0.65f + stretch * 1.3f) * ScaleFactor,
            Tint = Color.White,
            Rotation = Velocity + Angle.PiOver2
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box)
    {
        RectangleF hurtBox = new RectangleF(Position, Atlases.KnightScene.Wave.Area.Size * ScaleFactor * 0.6f);
        return box.Intersects(hurtBox);
    }
}
