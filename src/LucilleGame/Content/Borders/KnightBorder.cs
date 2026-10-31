using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.Knight;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Borders;

/// <summary>
///     A dynamic border that can be used during the Knight
///     battle if the player has dynamic borders enabled.
/// </summary>
public sealed class KnightBorder : Border
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Uniforms
    {
        public Vector4 LetterboxDimensions;

        public Vector4 Displacement;

        public Vector4 ActivityInterpolant;

        public Vector4 ViewportSize;

        public Vector4 PixelationLevels;

        public Vector4 BackgroundColor;

        public Vector4 StarsColorA;

        public Vector4 StarsColorB;

        public Vector4 Time;

        public Vector4 EdgeColor;
    }

    private Rectangle activeLetterboxArea;

    private Vector2 activeViewportSize;

    /// <summary>
    ///     Thie border's background color.
    /// </summary>
    public Color BackgroundColor
    {
        get;
        set;
    }

    /// <summary>
    ///     This border's first stars color.
    /// </summary>
    public Color StarsColorA
    {
        get;
        set;
    }

    /// <summary>
    ///     This border's second stars color.
    /// </summary>
    public Color StarsColorB
    {
        get;
        set;
    }

    /// <summary>
    ///     An ever-incrementing time value for this
    ///     border.
    /// </summary>
    public float Time
    {
        get;
        set;
    }

    /// <summary>
    ///     The shine intensity factor of the stars.
    /// </summary>
    public float ShineIntensityFactor
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The twinkle animation speedup factor of the stars.
    /// </summary>
    public float TwinkleSpeedupFactor
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The amount by which the star colors should be
    ///     biased to red, as a 0-1 interpolant.
    /// </summary>
    public float BiasToRedInterpolant
    {
        get;
        set;
    }

    /// <summary>
    ///     The central border instance.
    /// </summary>
    public static KnightBorder Instance
    {
        get;
        private set;
    }

    public override void Load() => Instance ??= this;

    public override bool DynamicallyDetermineActivity(NorthernlightGame game)
    {
        if (game.CurrentScene is not KnightFightScene knightFight)
            return false;
        if (knightFight.DeathScreen.OverrideGameScreen)
            return false;

        return true;
    }

    public override void Update(NorthernlightGame game)
    {
        base.Update(game);

        Time += game.Time.DeltaTime * TwinkleSpeedupFactor * 0.2f;

        bool sideB = false;
        float restingShineIntensity = 1f;
        float restingTwinkleSpeed = 1f;
        if (game.CurrentScene is KnightFightScene knightFight)
        {
            float knightHpRatio = float.Clamp(knightFight.RoaringKnight.HP / (float)knightFight.RoaringKnight.MaxHP, 0f, 1f);
            restingShineIntensity += (1f - knightHpRatio).Squared() * 4f;
            restingTwinkleSpeed += (1f - knightHpRatio) * 2.5f;
            sideB = knightFight.SideB;
        }

        BackgroundColor = new Color(7, 6, 10);

        StarsColorA = new Color(156, 32, 255);
        StarsColorB = new Color(110, 150, 255);

        if (sideB)
        {
            StarsColorA = new Color(0, 73, 255);
            StarsColorB = new Color(255, 255, 132);
        }
        if (BiasToRedInterpolant > 0f)
        {
            StarsColorA = Color.Lerp(StarsColorA, new Color(255, 64, 0), BiasToRedInterpolant);
            StarsColorB = Color.Lerp(StarsColorB, new Color(255, 0, 64), BiasToRedInterpolant);
        }

        StarsColorA *= ShineIntensityFactor * 0.4f;
        StarsColorB *= ShineIntensityFactor * 0.45f;

        TwinkleSpeedupFactor = float.LerpDt(TwinkleSpeedupFactor, restingTwinkleSpeed, 1.5f, game.Time);
        ShineIntensityFactor = float.LerpDt(ShineIntensityFactor, restingShineIntensity, 1.6f, game.Time);
        BiasToRedInterpolant = float.LerpDt(BiasToRedInterpolant, 0f, 1.3f, game.Time);
    }

    public override void PrepareRender(Scene scene, FrameContext frame, Renderer renderer, Rectangle letterboxArea, Vector2 viewportSize)
    {
        activeLetterboxArea = letterboxArea;
        activeViewportSize = viewportSize;

        renderer.Get<SpriteRenderer>().Enqueue(Atlases.KnightScene.Pixel.BackingTexture, new SpriteData(Atlases.KnightScene.Pixel.BackingTexture)
        {
            Position = new Vector3(viewportSize * 0.5f, 0f),
            Tint = Color.Black,
            Scale = viewportSize / Atlases.KnightScene.Pixel.BackingTexture.Size
        }, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.KnightFightBorderShader, PrepareShaderParameters));
    }

    internal void PrepareShaderParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            LetterboxDimensions = new Vector4(0f, 0f, activeLetterboxArea.Width * 0.5f, activeLetterboxArea.Height * 0.5f),
            Displacement = new Vector4(activeLetterboxArea.Width * 0.5f + activeLetterboxArea.X, activeLetterboxArea.Height * 0.5f + activeLetterboxArea.Y, 0f, 0f),
            ActivityInterpolant = Vector4.One * ActivityInterpolant,
            ViewportSize = new Vector4(activeViewportSize, 0f, 0f),
            PixelationLevels = new Vector4(4f, 2f, 0f, 0f),
            StarsColorA = StarsColorA.AsVector4(),
            StarsColorB = StarsColorB.AsVector4(),
            BackgroundColor = BackgroundColor.AsVector4(),
            Time = Vector4.One * Time,
            EdgeColor = new Color(30, 59, 109).AsVector4()
        });
    }
}
