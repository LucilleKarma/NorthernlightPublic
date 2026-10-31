using System.Numerics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class RedFlurrySlash : Bullet
{
    private float timer;

    private readonly Attack attack;

    private static BattleBox box;

    private static Scene scene;

    /// <summary>
    ///     How long this slash should exist for.
    /// </summary>
    public float SlashDuration
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this slash should visually ignore box bounds
    ///     or not.
    /// </summary>
    public bool IgnoreBoxBounds
    {
        get;
        set;
    }

    /// <summary>
    ///     The direction of this slash.
    /// </summary>
    public Vector2 AimDirection
    {
        get;
        set;
    }

    public RedFlurrySlash(Attack attack, KnightFightScene scene, Vector2 spawnPosition, Vector2 aimDirection, int damage, float grazePoints, float slashDuration) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        this.attack = attack;
        AimDirection = aimDirection;
        box = scene.BattleHandler.Box;
        SlashDuration = slashDuration;

        RedFlurrySlash.scene = scene;
    }

    protected override void UpdateInternal()
    {
        timer += Scene.Time.DeltaTime;
        if (timer >= SlashDuration)
        {
            if (!IgnoreBoxBounds)
                attack.Bullets.Add(new RedFlurrySlashResidue(Scene, SlashDuration * 0.85f, Position, AimDirection));
            Kill();
        }
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float lifetimeProgress = float.InverseLerp(0f, SlashDuration, timer);

        AtlasEntry slash = Atlases.KnightScene.SlashMarker;
        Color innerSlashColor = Color.Lerp(new Color(255, 0, 0), Color.White, lifetimeProgress * 0.6f);
        Vector2 scale = new Vector2(10f, (1f - lifetimeProgress) * 4f) * float.Sqrt(1f - lifetimeProgress) * float.InverseLerp(0f, 0.2f, lifetimeProgress);
        if (IgnoreBoxBounds)
            scale.X *= 4f;

        SpriteRenderer.ShaderWithPreparations? outsideOfBoxShader = new SpriteRenderer.ShaderWithPreparations(RadialWaveParticle.GetRestrictionShader(Scene.Game), PrepareRestrictionParameters_WithInversion);
        SpriteRenderer.ShaderWithPreparations? insideOfBoxShader = new SpriteRenderer.ShaderWithPreparations(RadialWaveParticle.GetRestrictionShader(Scene.Game), PrepareRestrictionParameters_WithoutInversion);

        int priority = 0;
        if (attack is FinalAttackSideB)
            priority = -50;

        sprites.Enqueue(slash, new SpriteData(slash)
        {
            Position = DrawPosition,
            Tint = innerSlashColor,
            Scale = scale * new Vector2(1f, 2f - float.Pow(lifetimeProgress, 0.75f)),
            Rotation = AimDirection
        }, BlendState.AlphaBlend, overridingShader: insideOfBoxShader, queuePriorityInfluence: priority);
        sprites.Enqueue(slash, new SpriteData(slash)
        {
            Position = DrawPosition,
            Tint = new Color(255, 0, 0),
            Scale = scale * new Vector2(1f, 1.5f),
            Rotation = AimDirection
        }, BlendState.AlphaBlend, overridingShader: IgnoreBoxBounds ? null : outsideOfBoxShader, queuePriorityInfluence: priority);
        sprites.Enqueue(slash, new SpriteData(slash)
        {
            Position = DrawPosition + Vector3.UnitZ,
            Tint = Color.Black,
            Scale = scale * new Vector2(1.1f, 5f) * 0.3f,
            Rotation = AimDirection
        }, BlendState.AlphaBlend, translucent: true, overridingShader: outsideOfBoxShader, queuePriorityInfluence: priority);
    }

    internal static void InitializeBoxAndScene(BattleBox box, Scene scene)
    {
        RedFlurrySlash.box = box;
        RedFlurrySlash.scene = scene;
    }

    internal static void PrepareRestrictionParameters_WithoutInversion(FrameContext frame, RenderPass pass)
    {
        Vector2 size = box.Size - Vector2.One * 8f;
        Vector2 position = box.Position;
        float scaleFactor = 1f;
        if (scene.MainCamera is RestrictedOrthographicCamera c)
            scaleFactor = c.EffectiveWidth / (float)c.Width;

        size *= scaleFactor;
        position *= scaleFactor;

        frame.CommandBuffer.BindFragmentUniformData(0, new RadialWaveParticle.Uniforms()
        {
            BoxDimensions = new Vector4(position + size * 0.5f, size.X, size.Y),
            BoxRotation = Vector4.One * box.Rotation,
            Invert = Vector4.Zero
        });
    }

    internal static void PrepareRestrictionParameters_WithInversion(FrameContext frame, RenderPass pass)
    {
        Vector2 size = box.Size;
        Vector2 position = box.Position;
        float scaleFactor = 1f;
        if (scene.MainCamera is RestrictedOrthographicCamera c)
            scaleFactor = c.EffectiveWidth / (float)c.Width;

        size *= scaleFactor;
        position *= scaleFactor;

        frame.CommandBuffer.BindFragmentUniformData(0, new RadialWaveParticle.Uniforms()
        {
            BoxDimensions = new Vector4(position + size * 0.5f, size.X, size.Y),
            BoxRotation = Vector4.One * box.Rotation,
            Invert = Vector4.One
        });
    }

    public override bool CollidingWith(RectangleF box)
    {
        float coverage = 2400f;
        float width = 4.25f;
        if (IgnoreBoxBounds)
        {
            width *= 3f;
            coverage *= 4f;
        }

        Vector2 a = Position - AimDirection * coverage * 0.5f;
        Vector2 b = Position + AimDirection * coverage * 0.5f;

        float d = float.PositiveInfinity;

        Vector2 topLeft = box.TopLeft;
        Vector2 topRight = box.TopRight;
        Vector2 bottomLeft = box.BottomLeft;
        Vector2 bottomRight = box.BottomRight;
        d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, topLeft, topRight));
        d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, bottomLeft, bottomRight));
        d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, topLeft, bottomLeft));
        d = float.Min(d, CollisionUtils.CalculateSegmentSegmentDistance(a, b, topRight, bottomRight));

        return d <= width;
    }
}
