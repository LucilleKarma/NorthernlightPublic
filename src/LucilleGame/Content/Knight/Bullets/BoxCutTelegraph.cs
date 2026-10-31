using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class BoxCutTelegraph : Bullet
{
    private readonly OwnedTimer timer;

    private readonly OwnedTimer particleSpawnTimer;

    private readonly Attack attack;

    /// <summary>
    ///     Whether this telegraph's edge should be gray
    ///     instead of red.
    /// </summary>
    public bool GrayInsteadOfRed
    {
        get;
        set;
    }

    /// <summary>
    ///     How long this telegraph should exist for.
    /// </summary>
    public float TelegraphDuration
    {
        get;
        set;
    }

    /// <summary>
    ///     How long this telegraph should exist for
    ///     after box telegraphs stop appearing.
    /// </summary>
    public float GraceDuration
    {
        get;
        set;
    }

    /// <summary>
    ///     The direction of this telegraph.
    /// </summary>
    public Vector2 AimDirection
    {
        get;
        set;
    }

    public BoxCutTelegraph(Attack attack, KnightFightScene scene, Vector2 spawnPosition, Vector2 aimDirection, float telegraphDuration, float graceDuration) : base(scene, 0, 0f, spawnPosition, default)
    {
        this.attack = attack;
        AimDirection = aimDirection;
        timer = scene.Time.Rent();
        particleSpawnTimer = scene.Time.Rent();
        TelegraphDuration = telegraphDuration;
        GraceDuration = graceDuration;
    }

    protected override void UpdateInternal()
    {
        if (timer / TelegraphDuration <= 0.3f)
        {
            if (particleSpawnTimer.Cycle(0.02f))
            {
                Vector2 forward = AimDirection.SafeNormalize(Vector2.Zero);
                Vector2 side = forward.RotatedBy(Angle.PiOver2);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 particleSpawnPosition = Position + forward * Scene.Rng.NextFloat(-1000f, 1000f) + side * Scene.Rng.NextFloat(-80f, 80f);
                    Vector2 particleVelocity = forward * 16000f;
                    attack.Bullets.Add(new LineParticle(Scene, TelegraphDuration * 0.15f, particleSpawnPosition, particleVelocity));
                }
            }
        }

        if (timer.Elapsed(TelegraphDuration))
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float telegraphProgress = float.InverseLerp(0f, 1f, timer / TelegraphDuration);
        float maxLength = 3000f;
        float length = float.InverseLerp(0f, 0.5f, telegraphProgress) * maxLength;
        float graceProgress = float.InverseLerp(TelegraphDuration - GraceDuration, TelegraphDuration, timer);
        float maxWidth = (1f + float.Cos(timer * 75f) * 0.4f) * 32f;
        float width =
            float.InverseLerp(0f, 0.07f, telegraphProgress) *
            (1f - graceProgress) * maxWidth;

        float graceBump = float.Bump(graceProgress.Cubed());
        width += float.Pow(graceBump, 4f) * maxWidth * 4f;

        AtlasEntry cut = Atlases.KnightScene.Cut;
        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        Color startingColor = GrayInsteadOfRed ? new Color(140, 140, 140) : new Color(255, 0, 0);
        Color color = Color.Lerp(startingColor, Color.White, graceBump * 1.3f);
        Vector2 scale = new Vector2(length, width);
        Vector3 steppedBack = DrawPosition + new Vector3(AimDirection * maxLength * -0.25f, 0f);

        sprites.Enqueue(cut, new SpriteData(cut)
        {
            Position = steppedBack - Vector3.UnitZ,
            Scale = scale / new Vector2(1f, cut.Area.Height),
            Tint = color,
            Rotation = AimDirection
        }, BlendState.Merge, drawToDepthBuffer: false);
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = steppedBack,
            Scale = scale / new Vector2(1f, pixel.Area.Height) * new Vector2(1f, 0.75f),
            Tint = Color.Lerp(Color.Black, color, graceBump),
            Rotation = AimDirection
        }, BlendState.AlphaBlend, drawToDepthBuffer: false);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
