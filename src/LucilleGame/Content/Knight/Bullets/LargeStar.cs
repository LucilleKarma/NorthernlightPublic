using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class LargeStar : Bullet
{
    private float existenceTimer;

    private readonly float scale;

    private readonly Attack attack;

    private readonly float lifetime;

    /// <summary>
    ///     This star's hurtbox.
    /// </summary>
    public RectangleF HurtBox => new RectangleF(Position, Atlases.KnightScene.Star.Area.Size * scale * 0.65f);

    public LargeStar(KnightFightScene scene, Attack attack, Vector2 spawnPosition, Vector2 velocity, int damage, float grazePoints, float scale, float lifetime) : base(scene, damage, grazePoints, spawnPosition, velocity)
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

        if (existenceTimer >= 3f)
            Velocity = Vector2.LerpDt(Velocity, Vector2.Zero, 2f, Scene.Time);
    }

    /// <summary>
    ///     Makes this star explode.
    /// </summary>
    public void Explode()
    {
        attack.Bullets.Add(new LargeStarExplosion(Scene, attack, Position, scale, 0.5f));

        float explosionAngle = Scene.Rng.NextFloat(float.Tau);
        for (int i = 0; i < 3; i++)
        {
            Vector2 direction = (float.Tau * i / 3f + explosionAngle).ToUnit();
            attack.Bullets.Add(new StarPart(Scene, Position, direction * 96f, (int)(Damage * 0.75f), GrazePoints / 8f, scale, 4f));

            direction = (float.Tau * (i + 0.5f) / 3f + explosionAngle).ToUnit();
            attack.Bullets.Add(new StarPart(Scene, Position, direction * 32f, (int)(Damage * 0.75f), GrazePoints / 8f, scale, 6f));
        }
        attack.Bullets.Add(new ShockwaveParticle(Scene, 0.4f, scale * 0.3f, Position));

        Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float pulse = 1f + float.Cos(float.Tau * existenceTimer / lifetime * 50f) * 0.05f;
        AtlasEntry star = Atlases.KnightScene.Star;
        sprites.Enqueue(star, new SpriteData(star)
        {
            Position = DrawPosition,
            Scale = Vector2.One * scale * pulse,
            Tint = Color.White
        }, BlendState.NonPremultiplied);

        AtlasEntry core = Atlases.KnightScene.StarGlow;
        sprites.Enqueue(core, new SpriteData(core)
        {
            Position = DrawPosition,
            Scale = Vector2.One * scale * pulse,
            Tint = Color.White
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box) => box.Intersects(HurtBox);
}
