using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class BlackDagger : Bullet
{
    private bool playedDashSound;

    private Vector3 startingDrawPosition;

    private readonly OwnedTimer timer;

    private readonly BattleBox box;

    private readonly Attack attack;

    private float TelegraphProgress => timer / TelegraphDuration;

    /// <summary>
    ///     Whether to show the white line
    ///     visual or not.
    /// </summary>
    public bool IncludeWhiteLine
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     Whether the dash sound should be played or not.
    /// </summary>
    public bool CanPlayDashSound
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     The amount by which this dagger has faded out.
    /// </summary>
    public float FadeOut => float.InverseLerp(1f, 1.16f, TelegraphProgress);

    /// <summary>
    ///     The maximum reel-back speed of this dagger.
    /// </summary>
    public float MaxReelBackSpeed
    {
        get;
        set;
    }

    /// <summary>
    ///     How long this dagger should telegraph for
    ///     before flying forward.
    /// </summary>
    public float TelegraphDuration
    {
        get;
        set;
    }

    /// <summary>
    ///     The aim direction of this dagger.
    /// </summary>
    public Vector2 AimDirection
    {
        get;
        set;
    }

    /// <summary>
    ///     The scale of this dagger.
    /// </summary>
    public float Scale
    {
        get;
        set;
    } = 0.7f;

    public BlackDagger(Attack attack, KnightFightScene scene, int damage, float grazePoints, Vector2 spawnPosition, Vector2 aimDirection, float maxReelBackSpeed, float telegraphDuration) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        this.attack = attack;
        AimDirection = aimDirection;
        box = scene.BattleHandler.Box;
        timer = scene.Time.Rent();
        MaxReelBackSpeed = maxReelBackSpeed;
        TelegraphDuration = telegraphDuration;
    }

    protected override void UpdateInternal()
    {
        if (TelegraphProgress < 1f)
        {
            float reelBackSpeed = float.InverseLerpBump(0f, 0.4f, 0.4f, 0.65f, TelegraphProgress) * MaxReelBackSpeed;
            Velocity = -AimDirection * reelBackSpeed;
        }
        else
        {
            if (!playedDashSound)
            {
                if (CanPlayDashSound)
                {
                    string dashSound = "KnightDaggerDash.wav";
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav(dashSound)));
                }

                startingDrawPosition = DrawPosition;
                playedDashSound = true;
            }

            // Slow, but hopefully not terrible given
            // how quickly these things disappear.
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

            Velocity = AimDirection * 20000f;

            if (FadeOut >= 1f)
                Kill();
        }
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry dagger = Atlases.KnightScene.BlackDagger;
        AtlasEntry daggerWhite = Atlases.KnightScene.BlackDaggerWhite;
        Angle rotation = AimDirection;

        float pulseProgress = float.InverseLerp(0.82f, 0.95f, TelegraphProgress);
        if (pulseProgress > 0f && TelegraphProgress < 1f)
        {
            sprites.Enqueue(dagger, new SpriteData(dagger)
            {
                Position = DrawPosition + Vector3.UnitZ * 50f,
                Scale = Vector2.One * Scale * new Vector2(1f + pulseProgress, 1f),
                Tint = new Color(1f, 0f, 0f, 1f - pulseProgress),
                Rotation = rotation
            }, BlendState.NonPremultiplied);
        }

        sprites.Enqueue(daggerWhite, new SpriteData(daggerWhite)
        {
            Position = DrawPosition + Vector3.UnitZ * 20f,
            Scale = Vector2.One * Scale,
            Tint = Color.White * (pulseProgress > 0f ? 1f : 0f),
            Rotation = rotation
        }, BlendState.NonPremultiplied);

        float fadeToRed = TelegraphProgress;
        Color color = Color.Lerp(Color.White, new Color(137, 0, 0), fadeToRed);
        sprites.Enqueue(dagger, new SpriteData(dagger)
        {
            Position = DrawPosition,
            Scale = Vector2.One * Scale,
            Tint = color,
            Rotation = rotation
        }, BlendState.NonPremultiplied);

        AtlasEntry pixel = Atlases.KnightScene.Pixel;

        float afterimageZOffset = 130f;
        Vector3 afterimagePoint = DrawPosition;
        Vector3 afterimageStep = Velocity.AsVector3().SafeNormalize(Vector3.UnitY) * -18f;
        while (Vector3.Dot(afterimagePoint - startingDrawPosition, Velocity.AsVector3()) > 0f)
        {
            sprites.Enqueue(dagger, new SpriteData(dagger)
            {
                Position = afterimagePoint + Vector3.UnitZ * afterimageZOffset,
                Scale = Vector2.One * Scale * 0.9f,
                Tint = new Color(255, 0, 0) * (TelegraphProgress >= 1f ? 1f : 0f) * 0.125f,
                Rotation = rotation
            }, BlendState.AlphaBlend);

            afterimageZOffset -= 2f;
            afterimagePoint += afterimageStep;
        }

        if (IncludeWhiteLine)
        {
            Vector3 lineStart = box.KeepInside(startingDrawPosition, Vector2.One);
            Vector3 lineEnd = box.KeepInside(DrawPosition, Vector2.One);
            Vector3 lineCenter = (lineStart + lineEnd) * 0.5f;
            float lineLength = Vector3.Distance(lineStart, lineEnd);

            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = lineCenter + Vector3.UnitZ * 500f,
                Rotation = (lineEnd - lineStart).AsVector2(),
                Scale = new Vector2(lineLength, 3f),
                Tint = Color.White * float.InverseLerp(0f, 0.65f, float.Bump(FadeOut))

            }, BlendState.AlphaBlend);
        }
    }

    public override bool CollidingWith(RectangleF box)
    {
        if (TelegraphProgress >= 1f)
        {
            Vector2 a = startingDrawPosition.AsVector2();
            Vector2 b = DrawPosition.AsVector2();

            float d = float.PositiveInfinity;

            Vector2 topLeft = box.TopLeft;
            Vector2 topRight = box.TopRight;
            Vector2 bottomLeft = box.BottomLeft;
            Vector2 bottomRight = box.BottomRight;
            d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, topLeft, topRight));
            d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, bottomLeft, bottomRight));
            d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, topLeft, bottomLeft));
            d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, topRight, bottomRight));

            return d <= 3f;
        }

        return false;
    }
}
