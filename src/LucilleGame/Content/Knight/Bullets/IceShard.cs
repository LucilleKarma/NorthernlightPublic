using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class IceShard : Bullet
{
    private float existenceTimer;

    private float stretch;

    /// <summary>
    ///     The general scale factor of this shard.
    /// </summary>
    public float ScaleFactor
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The general stretch coefficient
    ///     of this shard.
    /// </summary>
    public float StretchCoefficient
    {
        get;
        set;
    } = 1f;

    public IceShard(KnightFightScene scene, Vector2 spawnPosition, Vector2 velocity, int damage, float grazePoints) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        Velocity = velocity;
    }

    protected override void UpdateInternal()
    {
        existenceTimer += Scene.Time.DeltaTime;
        if (existenceTimer >= 5f)
            Kill();

        float acceleration = float.InverseLerp(0.25f, 5f, existenceTimer) * 2500f;

        // If this shard has flown past the player, it can
        // go really fast for more impact, as a treat!
        bool gonePastTarget = Vector2.Dot(Velocity, Scene.BattleHandler.Soul.Position.AsVector2() - Position) <= 0f;
        if (gonePastTarget)
            acceleration += 7600f;

        Velocity += Velocity.SafeNormalize(Vector2.Zero) * Scene.Time.DeltaTime * acceleration;

        stretch = float.Clamp(Velocity.Length() * StretchCoefficient / 1231f, 0f, 1f);
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry cut = Atlases.KnightScene.IceShard;
        sprites.Enqueue(cut, new SpriteData(cut)
        {
            Position = DrawPosition,
            Scale = new Vector2(1f + stretch * 1.2f, 1f - stretch * 0.7f) * ScaleFactor,
            Tint = Color.White,
            Rotation = Velocity
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box)
    {
        RectangleF hurtBox = new RectangleF(Position, Atlases.KnightScene.IceShard.Area.Size * ScaleFactor * (0.56f - stretch * 0.38f));
        return box.Intersects(hurtBox);
    }
}
