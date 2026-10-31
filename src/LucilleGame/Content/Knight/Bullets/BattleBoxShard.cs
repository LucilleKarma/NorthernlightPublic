using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class BattleBoxShard : Bullet
{
    private float existenceTimer;

    private float glowIntensity;

    /// <summary>
    ///     The countdown timer for how much
    ///     longer this shard should glow.
    /// </summary>
    public float GlowCountdown
    {
        get;
        set;
    }

    /// <summary>
    ///     This shard's scale.
    /// </summary>
    public float Scale
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     Whether this shard wiggles idly or not.
    /// </summary>
    public bool Wiggles
    {
        get;
        set;
    }

    /// <summary>
    ///     The acceleration delay of this shard.
    /// </summary>
    public float AccelerationDelay
    {
        get;
        set;
    } = 0.6f;

    /// <summary>
    ///     The acceleration coefficient of this shard.
    /// </summary>
    public float Acceleration
    {
        get;
        set;
    } = 3000f;

    public BattleBoxShard(KnightFightScene scene, Vector2 spawnPosition, Vector2 velocity, int damage, float grazePoints) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        Velocity = velocity;
    }

    protected override void UpdateInternal()
    {
        existenceTimer += Scene.Time.DeltaTime;
        if (existenceTimer >= 3f)
            Kill();

        float acceleration = float.InverseLerp(0f, 2.3f, existenceTimer - AccelerationDelay).Squared() * Acceleration;
        Velocity += Velocity.SafeNormalize(Vector2.Zero) * Scene.Time.DeltaTime * acceleration;

        if (GlowCountdown > 0f)
        {
            GlowCountdown = float.Max(0f, GlowCountdown - Scene.Time.DeltaTime);
            glowIntensity = 1f;
        }
        else if (glowIntensity > 0f)
            glowIntensity = float.Max(0f, glowIntensity - Scene.Time.DeltaTime * 4f);
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry shard = Atlases.KnightScene.BattleBoxShard;
        AtlasEntry shardGlow = Atlases.KnightScene.BattleBoxShardGlow;
        Vector2 baseScale = Vector2.One;
        if (Wiggles)
        {
            float wiggleTimer = float.Tau * existenceTimer * 7f;
            baseScale.X *= 1f + float.Sin(wiggleTimer) * 0.08f;
            baseScale.Y *= 1f + float.Cos(wiggleTimer) * 0.05f;
        }

        Vector3 position = DrawPosition + Vector3.UnitZ * 12f;
        for (int i = 0; i < 6; i++)
        {
            float opacity = (1f - i / 6f).Squared();
            if (i == 0)
                opacity = 1f;

            Vector3 drawOffset = Velocity.AsVector3() * i * -0.02f;
            drawOffset *= float.InverseLerp(450f, 840f, Velocity.Length());
            drawOffset.Z += i * 0.2f;

            BlendState blend = i == 0 ? BlendState.NonPremultiplied : BlendState.AlphaBlend;

            sprites.Enqueue(shard, new SpriteData(shard)
            {
                Position = position + drawOffset,
                Scale = baseScale * Scale,
                Tint = Color.White * opacity,
                Rotation = Velocity
            }, blend);

            if (glowIntensity > 0f)
            {
                sprites.Enqueue(shardGlow, new SpriteData(shardGlow)
                {
                    Position = position + drawOffset + Vector3.UnitZ * 2.5f,
                    Scale = baseScale * Scale,
                    Tint = Color.White * glowIntensity * opacity,
                    Rotation = Velocity
                }, BlendState.Additive);
            }
        }
    }

    public override bool CollidingWith(RectangleF box)
    {
        RectangleF hurtBox = new RectangleF(Position, Atlases.KnightScene.BattleBoxShard.Area.Size * Scale * 0.25f);
        return box.Intersects(hurtBox);
    }
}
