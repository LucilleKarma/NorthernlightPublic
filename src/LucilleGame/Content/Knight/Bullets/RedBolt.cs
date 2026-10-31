using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class RedBolt : Bullet
{
    private readonly float upwardFlightAcceleration;

    private Angle rotation;

    private float existenceTimer;

    private float opacity;

    private float scale;

    private readonly List<Vector2> previousPositions = [];

    public RedBolt(KnightFightScene scene, Vector2 spawnPosition, Vector2 velocity, Angle rotation, int damage, float grazePoints, float upwardFlightAcceleration) : base(scene, damage, grazePoints, spawnPosition, velocity)
    {
        this.rotation = rotation;
        this.upwardFlightAcceleration = upwardFlightAcceleration;
    }

    protected override void UpdateInternal()
    {
        scale = float.Smoothstep(float.InverseLerp(0f, 0.1f, existenceTimer));
        opacity = float.InverseLerp(0f, 0.3f, scale);
        if (upwardFlightAcceleration != 0f)
        {
            scale *= 1.6f;

            Velocity -= Vector2.UnitY * Scene.Time.DeltaTime * upwardFlightAcceleration;
            rotation = Velocity;
        }
        else
        {

            rotation += float.Tau * float.InverseLerp(0.75f, 0f, existenceTimer) * Scene.Time.DeltaTime * 3f;
            scale *= 1.5f;

            if (existenceTimer >= 0.2f && existenceTimer <= 0.7f)
            {
                Angle idealAngle = Scene.BattleHandler.Soul.Position.AsVector2() - Position;
                rotation = rotation.StepTowards(idealAngle, Scene.Time.DeltaTime * 24f);
                Velocity = Vector2.LerpDt(Velocity, Vector2.Zero, 16f, Scene.Time);
            }
            if (existenceTimer >= 0.6f)
                Velocity += rotation.Unit * Scene.Time.DeltaTime * 840f;
        }

        existenceTimer += Scene.Time.DeltaTime;
        if (existenceTimer >= 6f)
            Kill();

        previousPositions.Add(Position);
        if (previousPositions.Count >= 5)
            previousPositions.RemoveAt(0);
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry bullet = Atlases.KnightScene.RedBolt;
        AtlasEntry afterimage = Atlases.KnightScene.StarPart;
        AtlasEntry glow = Atlases.LargeTextures.Glow;

        for (int i = 0; i < previousPositions.Count; i++)
        {
            float opacity = i / (float)previousPositions.Count;
            sprites.Enqueue(afterimage, new SpriteData(afterimage)
            {
                Position = new Vector3(previousPositions[i], DrawPosition.Z - i * 0.2f - 1f),
                Scale = Vector2.One * scale,
                Tint = new Color(255, 0, 0) * this.opacity * opacity,
                Rotation = rotation
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }

        float glowIntensity = 1f;
        sprites.Enqueue(glow, new SpriteData(glow)
        {
            Position = DrawPosition,
            Scale = Vector2.One * scale * glowIntensity * 0.2f,
            Tint = new Color(255, 0, 0) * float.Clamp(opacity * glowIntensity, 0f, 1f) * 0.5f
        }, BlendState.Additive, drawToDepthBuffer: false);
        sprites.Enqueue(bullet, new SpriteData(bullet)
        {
            Position = DrawPosition,
            Scale = Vector2.One * scale,
            Tint = Color.White * opacity * 0.99f,
            Rotation = rotation
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box)
    {
        if (upwardFlightAcceleration != 0f && Velocity.Y > 0f)
            return false;

        RectangleF hurtBox = new RectangleF(Position, Vector2.One * 16f);
        return opacity >= 0.65f && box.Intersects(hurtBox);
    }
}
