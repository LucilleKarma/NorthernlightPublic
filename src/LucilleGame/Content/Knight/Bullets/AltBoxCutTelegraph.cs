using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class AltBoxCutTelegraph : Bullet
{
    private float timer;

    private readonly int spinDirection;

    private readonly BattleBox box;

    private readonly Attack attack;

    /// <summary>
    ///     How long this telegraph should exist for.
    /// </summary>
    public float TelegraphDuration
    {
        get;
        set;
    }

    /// <summary>
    ///     How long this telegraph should
    ///     create slice visuals for when it's
    ///     done telegraphing.
    /// </summary>
    public float SliceDuration
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

    public AltBoxCutTelegraph(Attack attack, KnightFightScene scene, Vector2 spawnPosition, Vector2 aimDirection, float telegraphDuration, float sliceDuration) : base(scene, 0, 0f, spawnPosition, default)
    {
        this.attack = attack;
        spinDirection = scene.Rng.NextBool() ? -1 : 1;
        AimDirection = aimDirection;
        box = scene.BattleHandler.Box;
        TelegraphDuration = telegraphDuration;
        SliceDuration = sliceDuration;
    }

    protected override void UpdateInternal()
    {
        timer += Scene.Time.DeltaTime;
        if (timer >= TelegraphDuration + SliceDuration)
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        Vector3 drawPosition = DrawPosition + Vector3.UnitZ * 100f;
        if (timer <= TelegraphDuration)
        {
            float telegraphProgress = float.InverseLerp(0f, TelegraphDuration, timer);
            float length = telegraphProgress * 4000f;
            float width = float.Pow(1f - telegraphProgress, 1.5f) * 40f;
            Color color = new Color(193, 0, 0);
            Angle theta = new Angle(AimDirection) + spinDirection * (1f - telegraphProgress) * 0.2f;

            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = drawPosition,
                Tint = color,
                Scale = new Vector2(length, width),
                Rotation = theta
            }, BlendState.NonPremultiplied);
        }
        else if (timer <= TelegraphDuration + SliceDuration)
        {
            float sliceProgress = float.InverseLerp(0f, SliceDuration, timer - TelegraphDuration);
            float length = 4000f;
            float width = float.Pow(1f - sliceProgress, 1.5f) * 16f;
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = drawPosition,
                Tint = Color.White,
                Scale = new Vector2(length, width),
                Rotation = AimDirection
            }, BlendState.NonPremultiplied);

            int frame = (int)(sliceProgress * 4f);
            if (frame <= 3)
            {
                AtlasEntry slash = Atlases.KnightScene.SlashAnimation;
                sprites.Enqueue(slash, new SpriteData(slash, 0, frame, 1, 4)
                {
                    Position = drawPosition + Vector3.UnitZ * 8f,
                    Tint = Color.White,
                    Scale = new Vector2(0.6f, 1f) * 1.5f,
                    Rotation = AimDirection
                }, BlendState.NonPremultiplied);
            }
        }
    }

    public override bool CollidingWith(RectangleF box) => false;
}
