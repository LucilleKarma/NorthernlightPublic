using System.Numerics;
using System.Runtime.Intrinsics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class BlackSword : Bullet
{
    private bool hasDashed;

    private float telegraphLineOpacity;

    private Vector2 aimDirection;

    private readonly int dashDamage;

    private readonly float dashAnimationDuration;

    private readonly Attack attack;

    private readonly OwnedTimer afterimageTimer;

    internal readonly bool top;

    /// <summary>
    ///     The base rotation of this sword.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }
    /// <summary>
    ///     The previous position of this sword.
    /// </summary>
    public Vector2 PreviousPosition
    {
        get;
        set;
    }

    /// <summary>
    ///     The effective rotation of this sword, factoring
    ///     in whether it's on the top of the circle or not.
    /// </summary>
    public Angle EffectiveRotation
    {
        get
        {
            if (DashTimer <= 0f && !top)
                return Rotation + Angle.Pi;

            return Rotation;
        }
    }

    /// <summary>
    ///     How long this sword has existed, in seconds.
    /// </summary>
    public float ExistenceTimer
    {
        get;
        set;
    }

    /// <summary>
    ///     How far along this sword is with its
    ///     dash animation as a timer.
    /// </summary>
    public float DashTimer
    {
        get;
        private set;
    }

    /// <summary>
    ///     This sword's current color.
    /// </summary>
    public Color Color
    {
        get
        {
            Soul soul = Scene.BattleHandler.Soul;
            Color color = Color.White;
            if (DashTimer <= 0f && CollidingWith(new RectangleF(soul.Position.AsVector2(), Vector2.One * Scale * 150f)))
                color = new Color(208, 0, 50);

            return color;
        }
    }

    /// <summary>
    ///     The general scale of swords.
    /// </summary>
    public static float Scale => 0.8f;

    public BlackSword(KnightFightScene scene, Attack attack, int damage, int dashDamage, float grazePoints, float dashAnimationDuration, bool top, Vector2 spawnPosition, Vector2 velocity, Angle rotation) : base(scene, damage, grazePoints, spawnPosition, velocity)
    {
        this.attack = attack;
        this.dashAnimationDuration = dashAnimationDuration;
        this.top = top;
        this.dashDamage = dashDamage;
        this.Rotation = rotation;
        afterimageTimer = Scene.Time.Rent();
    }

    protected override void UpdateInternal()
    {
        if (ExistenceTimer >= 5f)
            ClearIfOffscreen();

        if (afterimageTimer.Cycle(hasDashed ? 0.01f : 0.02f))
            attack.Bullets.Add(new BlackSwordAfterimage(Scene, Color, Position, EffectiveRotation));

        ExistenceTimer += Scene.Time.DeltaTime;

        if (DashTimer > 0f)
        {
            PerformDashAnimationStuff();
            DashTimer += Scene.Time.DeltaTime;
        }
    }

    private void PerformDashAnimationStuff()
    {
        float dashAnimationDuration = 1.15f;
        float dashAnimationProgress = DashTimer / dashAnimationDuration;

        telegraphLineOpacity = float.InverseLerp(0f, 0.225f, dashAnimationProgress);

        if (dashAnimationProgress >= 1f)
        {
            if (!hasDashed)
            {
                Scene.AddScreenShake(2.3f);
                hasDashed = true;
            }

            telegraphLineOpacity = 0f;
            Velocity += aimDirection * Scene.Time.DeltaTime * 33000f;
            if (dashAnimationProgress >= 1.5f)
                Kill();

            return;
        }

        Vector2 currentDirection = Rotation.Unit;
        Vector2 idealDirection = aimDirection;

        float aimCoefficient = telegraphLineOpacity * 17.5f;
        Vector2 newDirection = Vector2.LerpDt(currentDirection, idealDirection, aimCoefficient, Scene.Time).SafeNormalize(currentDirection);
        Rotation = newDirection;

        float reelBackProgress = float.InverseLerp(0.5f, 1f, dashAnimationProgress);
        float reelBackSpeed = float.Smoothstep(float.Smoothstep(reelBackProgress.Squared())) * (1f - reelBackProgress) * 800f;
        Velocity = aimDirection * -reelBackSpeed;
        PreviousPosition = Position;
    }

    /// <summary>
    ///     Starts this sword's dash animation.
    /// </summary>
    public void StartDashAnimation()
    {
        ClearIfOffscreen();
        aimDirection = (Scene.BattleHandler.Soul.Position.AsVector2() - Position + Scene.Rng.NextCircular(150f)).SafeNormalize(Vector2.UnitY);
        Damage = dashDamage;

        Velocity = Vector2.Zero;
        PreviousPosition = Position;
        DashTimer = 0.00001f;
    }

    /// <summary>
    ///     Clears this sword if it's offscreen currently.
    /// </summary>
    public void ClearIfOffscreen()
    {
        RectangleF zoneOfNotInstantlyDyingAHorribleDeath = new RectangleF(NorthernlightGame.KnightFightResolution * 0.5f, NorthernlightGame.KnightFightResolution * 1.35f);
        bool byebye = !zoneOfNotInstantlyDyingAHorribleDeath.Contains(Position);
        if (byebye)
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        Color color = Color;
        AtlasEntry sword = Atlases.KnightScene.BlackSword;
        Angle rotation = EffectiveRotation;

        int afterimageCount = 6;
        float afterimageOffsetIncrement = 4.5f;
        if (attack is FinalAttackSideB)
            afterimageOffsetIncrement *= 0.2f;

        Vector2 direction = (Position - PreviousPosition).SafeNormalize(Vector2.Zero);
        if (DashTimer > 0f)
            direction = Vector2.Zero;
        if (hasDashed)
        {
            direction = Velocity.SafeNormalize(Vector2.Zero);
            afterimageCount = 18;
        }

        for (int i = 0; i < afterimageCount; i++)
        {
            float opacity = float.Sqrt(1f - i / (float)afterimageCount);
            if (i == 0)
                opacity = 1f;

            Vector3 drawPosition = DrawPosition - new Vector3(direction * i * afterimageOffsetIncrement, i);

            sprites.Enqueue(sword, new SpriteData(sword)
            {
                Position = drawPosition,
                Tint = color * opacity,
                Rotation = rotation,
                Scale = Vector2.One * Scale
            });
        }

        if (telegraphLineOpacity > 0f)
        {
            AtlasEntry pixel = Atlases.KnightScene.Pixel;
            float length = 3000f;
            float width = telegraphLineOpacity * 28f;
            Vector2 scale = new Vector2(length, width);
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = DrawPosition + EffectiveRotation.Unit.AsVector3() * length * 0.5f - Vector3.UnitZ * 19f,
                Scale = scale / new Vector2(1f, pixel.Area.Height) * new Vector2(1f, 0.75f),
                Tint = new Color(193, 0, 0) * telegraphLineOpacity * 0.5f,
                Rotation = EffectiveRotation
            }, BlendState.AlphaBlend, drawToDepthBuffer: false);
        }
    }

    public override void OnHit(Hero target)
    {
        Scene.AddScreenShake(4.2f);
    }

    public override bool CollidingWith(RectangleF box)
    {
        if (DashTimer > 0f && !hasDashed)
            return false;

        float leniencyThing = DashTimer > 0f ? 0.075f : 0.125f;
        Angle rotation = EffectiveRotation;
        Vector2 direction = rotation.Unit;
        Vector2 center = Position + direction * 9f;
        Vector2 fullHitboxSize = Atlases.KnightScene.BlackSword.Area.Size;
        for (int i = 0; i < 8; i++)
        {
            float checkProgress = i / 7f;
            Vector2 swordCheckOrigin = center + direction * (checkProgress - 0.25f) * fullHitboxSize.Y;
            RectangleF swordCheckArea = new RectangleF(swordCheckOrigin, fullHitboxSize * leniencyThing);
            if (box.Intersects(swordCheckArea))
                return true;
        }

        return false;
    }
}
