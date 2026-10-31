using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Bullets;

public sealed class RadialWaveParticle : Bullet
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Uniforms
    {
        public Vector4 BoxDimensions;

        public Vector4 BoxRotation;

        public Vector4 Invert;
    }

    private readonly OwnedTimer timer;

    private readonly BattleBox box;

    private readonly Scene scene;

    /// <summary>
    ///     How long this particle should exist for.
    /// </summary>
    public float Lifetime
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this wave should be confined
    ///     to the battle box or not.
    /// </summary>
    public bool ConfinedToBattleBox
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this particle should render additively
    ///     or not.
    /// </summary>
    public bool Additive
    {
        get;
        set;
    }

    /// <summary>
    ///     The maximum scale of this particle.
    /// </summary>
    public float MaxScale
    {
        get;
        set;
    }

    /// <summary>
    ///     The color of this particle.
    /// </summary>
    public Color Color
    {
        get;
        set;
    }

    public RadialWaveParticle(KnightFightScene scene, BattleBox box, float lifetime, float maxScale, Vector2 spawnPosition, Color color) : base(scene, 0, 0f, spawnPosition)
    {
        Lifetime = lifetime;
        MaxScale = maxScale;
        timer = scene.Time.Rent();
        Color = color;

        this.scene = scene;
        this.box = box;
    }

    protected override void UpdateInternal()
    {
        if (timer.Elapsed(Lifetime))
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float lifetimeRatio = timer / Lifetime;
        Vector2 scale = Vector2.One * float.Pow(lifetimeRatio, 1.25f) * MaxScale;
        AtlasEntry wave = Atlases.KnightScene.RadialBurst;
        Color color = Color * float.Pow(1f - lifetimeRatio, 0.45f);
        SpriteRenderer.ShaderWithPreparations? shaderPerparations = null;
        if (ConfinedToBattleBox)
        {
            RedFlurrySlash.InitializeBoxAndScene(box, scene);
            shaderPerparations = new SpriteRenderer.ShaderWithPreparations(GetRestrictionShader(Scene.Game), RedFlurrySlash.PrepareRestrictionParameters_WithoutInversion);
        }

        sprites.Enqueue(wave, new SpriteData(wave)
        {
            Position = DrawPosition,
            Scale = scale / 100f,
            Tint = color
        }, Additive ? BlendState.Additive : BlendState.AlphaBlend, drawToDepthBuffer: true, translucent: false, overridingShader: shaderPerparations);
    }

    internal static Shader GetRestrictionShader(Game game) => Shaders.RotatedBoxRestrictionShader;

    public override bool CollidingWith(RectangleF box) => false;
}
