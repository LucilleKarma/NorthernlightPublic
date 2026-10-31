using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class AimedBlackDagger : Bullet
{
    private float opacity;

    private float existenceTimer;

    private Vector3 startingDrawPosition;

    private float telegraphDuration;

    private bool playedDashSound;

    private readonly Angle rotation;

    /// <summary>
    ///     How far along this dagger is in
    ///     its telegraph animation.
    /// </summary>
    public float TelegraphProgress
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount by which this dagger has faded out.
    /// </summary>
    public float FadeOut => float.InverseLerp(1f, 1.24f, TelegraphProgress);

    /// <summary>
    ///     The scale of this dagger.
    /// </summary>
    public Vector2 Scale
    {
        get
        {
            float stretch = float.InverseLerp(0.8f, 0.95f, TelegraphProgress);
            return Vector2.One * ScaleFactor * new Vector2(1f + stretch.Squared() * 0.5f, 1f - stretch * 0.4f);
        }
    }

    /// <summary>
    ///     The general scale factor of this dagger.
    /// </summary>
    public float ScaleFactor
    {
        get;
        set;
    } = 1f;

    public AimedBlackDagger(KnightFightScene scene, int damage, float grazePoints, Vector2 spawnPosition, Vector2 aimDirection) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        rotation = aimDirection;
    }

    protected override void UpdateInternal()
    {
        if (existenceTimer == 0f)
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("DaggerSummon.wav")));

        existenceTimer += Scene.Time.DeltaTime;
        opacity = float.InverseLerp(0f, 0.25f, existenceTimer);

        Vector2 forward = rotation.Unit;

        if (TelegraphProgress > 0f && telegraphDuration > 0f)
            TelegraphProgress += Scene.Time.DeltaTime / telegraphDuration;

        if (TelegraphProgress <= 1f)
        {
            float wave = float.Sin(float.Tau * existenceTimer);
            float accelerationCoefficient = 12.5f;
            Velocity = Vector2.LerpDt(Velocity, forward * wave * (1f - TelegraphProgress).Squared() * 65f, accelerationCoefficient, Scene.Time);

            startingDrawPosition = DrawPosition;
        }
        else
        {
            if (!playedDashSound)
            {
                playedDashSound = true;
                Scene.AddScreenShake(2.4f);
                Scene.BattleHandler.Box.Position += forward * 3f;

                string dashSound = "KnightDaggerDash.wav";
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav(dashSound)));
            }

            Velocity = forward * 12000f;
            if (FadeOut >= 1f)
                Kill();
        }
    }

    /// <summary>
    ///     Prepares this dagger for firing.
    /// </summary>
    public void PrepareToFire(float telegraphDuration)
    {
        if (TelegraphProgress > 0f)
            return;

        this.telegraphDuration = telegraphDuration;
        TelegraphProgress = 0.001f;
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry dagger = Atlases.KnightScene.BlackDagger;
        AtlasEntry daggerWhite = Atlases.KnightScene.BlackDaggerWhite;

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

        Vector3 lineStart = startingDrawPosition;
        Vector3 lineEnd = DrawPosition;
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
