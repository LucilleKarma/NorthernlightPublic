using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.ChapterSelect;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Content.Menu;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;
using static Northernlight.Content.Knight.AttackPatterns.HowlingWinds;

namespace Northernlight.Content.Logo;

public sealed class FreezingLogo : DrawableObject
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UniformsLogo
    {
        public Vector4 WavinessData;

        public Vector4 FreezeData;

        public Vector4 LogoTextureBounds;

        public Vector4 NoiseTextureBounds;
    }

    private bool initialized;

    private float windTimer;

    private float windIntensity;

    private float logoFreezeIntensity;

    private Sound windSound;

    private Sound introSound;

    private readonly Scene scene;

    private readonly OwnedTimer timer;

    /// <summary>
    ///     The manual fade-out progress value, which
    ///     can be started if the player presses
    ///     confirm to speed the animation up.
    /// </summary>
    public float ManualFadeoutProgress
    {
        get;
        set;
    }

    public FreezingLogo(Scene scene)
    {
        timer = scene.Time.Rent();
        this.scene = scene;
    }

    protected override void UpdateSelf()
    {
        base.UpdateSelf();

        if (!initialized)
        {
            initialized = true;
            introSound = new Sound(AudioBuffer.FromWav("AUDIO_INTRONOISE.wav"));
            scene.Audio.Play(introSound);

            windSound?.Stop();
            windSound = new Sound(AudioBuffer.FromWav("BlizzardWindLoop.wav"))
            {
                Loop = true
            };
            scene.Audio.Play(windSound);
            scene.Music.Stop();
        }

        if (ManualFadeoutProgress > 0f)
            ManualFadeoutProgress += scene.Time.DeltaTime * 0.75f;
        else if (ActiveKeymap.Confirm.WasJustPressed)
            ManualFadeoutProgress = 0.0001f;

        if (timer.Cycle(14.5f) || ManualFadeoutProgress >= 1f)
        {
            MenuScene menu = new MenuScene(scene.Game);
            menu.Initialize();
            scene.Game.PresentScene(menu);

            introSound.Stop();
        }

        windIntensity = float.InverseLerpBump(5f, 7f, 10.75f, 14f, timer) * (1f - ManualFadeoutProgress) * 2f;
        windTimer += scene.Time.DeltaTime * (float.Sqrt(windIntensity) * 1.9f + 0.4f);

        float idealLogoFreezeIntensity = float.Max(logoFreezeIntensity, float.Clamp(windIntensity, 0f, 1f));
        logoFreezeIntensity = float.LerpDt(logoFreezeIntensity, idealLogoFreezeIntensity, 1f, scene.Time);

        windSound.Stream.Volume = windIntensity * 0.5f;
        introSound.Stream.Volume = 1f - ManualFadeoutProgress;
    }

    protected override void PrepareRenderSelf(RenderLayer layer)
    {
        base.PrepareRenderSelf(layer);

        float logoFadeOut = float.InverseLerp(14.5f, 13.75f, timer) * (1f - ManualFadeoutProgress);
        Angle windAngle = Angle.Pi - 0.16f;
        Angle jitter = scene.Rng.NextFloat(-1f, -0.6f) * windIntensity * 0.03f;

        SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();
        Texture logo = Atlases.Logo.IMAGE_LOGO.BackingTexture;
        sprites.Enqueue(logo, new SpriteData(logo)
        {
            Position = new Vector3(160f, 110f, 0f),
            Scale = Atlases.Logo.IMAGE_LOGO.Area.Size / logo.Size,
            Tint = Color.White * (1f - float.Sqrt(logoFreezeIntensity) * 0.98f) * logoFadeOut,
            Rotation = jitter
        }, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.LogoShader, ApplyLogoShader));

        Texture blizzard = Atlases.LargeTextures.CloudyNoise.BackingTexture;
        SpriteData blizzardData = new SpriteData(blizzard)
        {
            Position = new Vector3(160f, 110f, 50f),
            Scale = Vector2.One * 420f / blizzard.Size,
            Tint = Color.White * windIntensity * 0.25f,
            Rotation = windAngle
        };
        sprites.Enqueue(blizzard, blizzardData, default, false, new SpriteRenderer.ShaderWithPreparations(Shaders.BlizzardShader, PrepareBlizzardParameters));

        SpriteData windData = new SpriteData(blizzard)
        {
            Position = new Vector3(160f, 110f, 100f),
            Scale = Vector2.One * 420f / blizzard.Size,
            Tint = Color.White * windIntensity.Squared() * 0.6f,
            Rotation = windAngle
        };
        sprites.Enqueue(blizzard, windData, default, false, new SpriteRenderer.ShaderWithPreparations(Shaders.WindLinesShader, PrepareWindParameters));
    }

    private void ApplyLogoShader(FrameContext frame, RenderPass pass)
    {
        float time = timer * 60f;
        float fadeIn = float.InverseLerp(0f, 3f, timer);
        float offsetFactor = (1f - fadeIn) * 0.9f;
        float opacity = fadeIn * 0.5f;
        float snapPrecision = 1.5f;
        Color freezeColor = new Color(0.1f, 0.7f, 0.94f);
        freezeColor = Color.Lerp(freezeColor, Color.White, 0.6f);
        freezeColor *= 1.5f;

        frame.CommandBuffer.BindFragmentUniformData(0, new UniformsLogo()
        {
            WavinessData = new Vector4(time, offsetFactor, opacity, snapPrecision),
            FreezeData = new Vector4(float.InverseLerp(0.25f, 1f, logoFreezeIntensity).Squared() * 2.65f, freezeColor.R, freezeColor.G, freezeColor.B),
            LogoTextureBounds = Atlases.Logo.IMAGE_LOGO.VectorBounds,
            NoiseTextureBounds = Atlases.Logo.Noise_OmitPadding.VectorBounds
        });
    }

    private void PrepareBlizzardParameters(FrameContext frame, RenderPass pass)
    {
        float zoom = 0.85f;
        Vector2 windZoom = new Vector2(1.5f, 6f) * zoom;
        Vector2 snowZoom = new Vector2(0.25f, 1f) * zoom;

        frame.CommandBuffer.BindFragmentUniformData(0, new BlizzardUniforms()
        {
            Time = new Vector4(-windTimer, 0f, 0f, 0f),
            NoiseTextureBounds = Atlases.LargeTextures.SemiRandom_OmitPadding.VectorBounds,
            PerlinTextureBounds = Atlases.LargeTextures.PerlinNoise_OmitPadding.VectorBounds,
            Zoom = new Vector4(windZoom.X, windZoom.Y, snowZoom.X, snowZoom.Y)
        });
        pass.BindFragmentSamplerTextures(1, new RenderPass.SamplerTexture(scene.Device.PointWrap, Atlases.LargeTextures.Atlas.Value.BackingTexture));
    }

    private void PrepareWindParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new FallingStars.Uniforms()
        {
            Time = new Vector4(windTimer * -0.9f, 0f, 0f, 0f),
            OverlayColor = Vector4.Zero,
            WindTextureBounds = Atlases.LargeTextures.WindLines.VectorBounds,
        });
    }
}
