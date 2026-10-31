using System.Numerics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class RedFlurryTelegraph : Bullet
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
    ///     Whether this telegraph should visually ignore box bounds
    ///     or not.
    /// </summary>
    public bool IgnoreBoxBounds
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

    /// <summary>
    ///     The idle drift angular velocity that applies
    ///     at the end of the telegraph.
    /// </summary>
    public Angle IdleDriftAngularVelocity
    {
        get;
        set;
    }

    public RedFlurryTelegraph(Attack attack, KnightFightScene scene, Vector2 spawnPosition, Vector2 aimDirection, int spinDirection, float telegraphDuration, Angle idleDriftAngularVelocity) : base(scene, 0, 0f, spawnPosition, default)
    {
        this.attack = attack;
        this.spinDirection = spinDirection;
        AimDirection = aimDirection;
        box = scene.BattleHandler.Box;
        TelegraphDuration = telegraphDuration;
        IdleDriftAngularVelocity = idleDriftAngularVelocity;
    }

    protected override void UpdateInternal()
    {
        float slowdownSharpnessExponent = 1.3f;
        float telegraphProgress = float.InverseLerp(0f, TelegraphDuration, timer);
        Angle spinArcFactor = Angle.Pi * 1.5f;
        float idleDrift = IdleDriftAngularVelocity * float.Pow(telegraphProgress, slowdownSharpnessExponent) * spinDirection;
        float angularVelocity = float.Pow(1f - telegraphProgress, slowdownSharpnessExponent) * spinArcFactor * spinDirection / TelegraphDuration + idleDrift;
        AimDirection = AimDirection.RotatedBy(angularVelocity * Scene.Time.DeltaTime);

        timer += Scene.Time.DeltaTime;
        if (timer >= TelegraphDuration)
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        Vector2 baseOrigin = DrawPosition.AsVector2();

        float telegraphProgress = float.InverseLerp(0f, TelegraphDuration, timer);
        float width = 16f + float.InverseLerp(0.2f, 0f, telegraphProgress) * 25f;
        width *= float.InverseLerp(-0.065f, 0.3f, telegraphProgress).Squared();

        float baseLength = float.Sqrt(telegraphProgress) * 600f + 50f;
        if (IgnoreBoxBounds)
        {
            width *= 2f;
            baseLength *= 10f;
        }

        Vector2 start = baseOrigin - AimDirection * baseLength;
        Vector2 end = baseOrigin + AimDirection * baseLength;

        float length = Vector2.Distance(start, end);

        SpriteRenderer.ShaderWithPreparations? shaderPreparations = new SpriteRenderer.ShaderWithPreparations(RadialWaveParticle.GetRestrictionShader(Scene.Game), RedFlurrySlash.PrepareRestrictionParameters_WithoutInversion);
        if (IgnoreBoxBounds)
            shaderPreparations = null;

        int priority = 0;
        if (attack is FinalAttackSideB)
            priority = 50;

        float flash = timer * 4f % 1f;
        Vector2 orthogonal = AimDirection.RotatedBy(Angle.PiOver2);
        for (int i = 0; i < 8; i++)
        {
            float progress = i / 8f;
            float widthFactor = 1f + progress * 1.75f;
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = new Vector3(baseOrigin, DrawPosition.Z),
                Tint = new Color(193, 0, 0) * (1f - progress) * 0.6f,
                Scale = new Vector2(length, width * widthFactor),
                Rotation = end - start
            }, BlendState.Additive, overridingShader: shaderPreparations, drawToDepthBuffer: false, queuePriorityInfluence: priority);
        }
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(baseOrigin, DrawPosition.Z + 1f),
            Tint = Color.Black,
            Scale = new Vector2(length, width * 0.5f),
            Rotation = end - start
        }, BlendState.Disabled, overridingShader: shaderPreparations, drawToDepthBuffer: false, queuePriorityInfluence: priority);

        for (int i = -1; i <= 1; i += 2)
        {
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = new Vector3(baseOrigin + orthogonal * flash * i * 40f, DrawPosition.Z),
                Tint = new Color(193, 0, 0) * (1f - flash),
                Scale = new Vector2(length, width * 0.5f),
                Rotation = end - start
            }, BlendState.Additive, overridingShader: shaderPreparations, drawToDepthBuffer: false, queuePriorityInfluence: priority);
        }
    }

    public override bool CollidingWith(RectangleF box) => false;
}
