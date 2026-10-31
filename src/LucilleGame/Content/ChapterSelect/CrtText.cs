using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Core;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;
using SDL3;

namespace Northernlight.Content.ChapterSelect;

public sealed class CrtText : DrawableObject
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Uniforms
    {
        public Vector4 Time;

        public Vector4 SphericalWarpAmount;

        public Vector4 AberrationAmount;

        public Vector4 Resolution;

        public Vector4 ScanLineAmount;

        public Vector4 NoiseAmount;

        public Vector4 InterferenceAmount;

        public Vector4 GrilleAmount;

        public Vector4 GrilleSize;

        public Vector4 VignetteAmount;

        public Vector4 VignetteIntensity;

        public Vector4 RollLineAmount;

        public Vector4 RollSpeed;

        public Vector4 ScanLineStrength;

        public Vector4 PixelStrength;
    }

    private bool resetTarget;

    private string spelledOutText = string.Empty;

    private readonly OwnedTimer spellOutTimer;

    private readonly OwnedTimer existenceTimer;

    /// <summary>
    ///     This object's font.
    /// </summary>
    public Font Font
    {
        get;
        private set;
    } = Fonts.GetFont(Fonts.Type.fnt_main);

    /// <summary>
    ///     The chapter number that the player tried
    ///     and failed to load.
    /// </summary>
    public int Chapter
    {
        get;
    }

    /// <summary>
    ///     The localization text to spell out.
    /// </summary>
    public LanguageAgnosticText Text
    {
        get;
    }

    /// <summary>
    ///     The render target responsible for this
    ///     system's render contents.
    /// </summary>
    public static Texture? Target
    {
        get;
        internal set;
    }

    /// <summary>
    ///     The scene responsible for this text.
    /// </summary>
    public Scene Scene
    {
        get;
    }

    public CrtText(ChapterNotFoundScene scene, int chapter)
    {
        spellOutTimer = scene.Game.Time.Rent();
        existenceTimer = scene.Game.Time.Rent();

        Chapter = chapter;
        Scene = scene;
        Text = LocalizationEntries.ChapterUnloaded.WithArgs(Chapter.ToString());

        if (Target is null)
        {
            SDL.GPUTextureCreateInfo textureCreationInfo = new SDL.GPUTextureCreateInfo()
            {
                Width = (uint)NorthernlightGame.KnightFightResolution.X,
                Height = (uint)NorthernlightGame.KnightFightResolution.X,
                Type = SDL.GPUTextureType.TextureType2D,
                Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
                LayerCountOrDepth = 1,
                NumLevels = 1,
                Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
            };

            Target = new Texture(textureCreationInfo, scene.Device);
        }
    }

    private void PrepareTexture(FrameContext frame)
    {
        if (Target is null)
            return;

        using TextureWriteScope scope = Target.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp);

        TextRenderer text = scope.Renderer.Get<TextRenderer>();
        Vector3 drawOrigin = new Vector3(Target.Size, 0f) * new Vector3(0.25f, 0.15f, 1f);
        text.Enqueue(spelledOutText, Font, drawOrigin, Color.White, Vector2.One * 4f, new Vector2(0f, 0.5f));
    }

    protected override void UpdateSelf()
    {
        base.UpdateSelf();

        string toSpellOut = Text.Value;
        float spellDelay = spelledOutText == string.Empty ? 4f : 0.45f;
        if (spellOutTimer.Cycle(spellDelay) && spelledOutText != toSpellOut)
        {
            resetTarget = true;
            spelledOutText += toSpellOut[spelledOutText.Length];
        }

        if (existenceTimer.Cycle(18f))
            Scene.Game.PresentScene(new ChapterSelectScene(Scene.Game));
    }

    protected override void PrepareRenderSelf(RenderLayer layer)
    {
        base.PrepareRenderSelf(layer);

        Scene.Game.EnqueueForBeforeNextRender(f =>
        {
            if (!resetTarget)
                return;

            resetTarget = false;

            PrepareTexture(f);
        });

        if (Target is null)
            return;

        SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();
        Vector2 sceneSize = new Vector2(320f, 240f);
        sprites.Enqueue(Target, new SpriteData(Target)
        {
            Position = new Vector3(sceneSize * 0.5f, 0f),
            Scale = sceneSize / Target.Size,
            Tint = Color.White
        }, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.CrtShader, PrepareCrtParameters));
    }

    private void PrepareCrtParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            Time = Vector4.UnitX * (existenceTimer * 6f + Scene.Game.Rng.NextFloat(0.65f)),
            Resolution = new Vector4(NorthernlightGame.KnightFightResolution, 0f, 0f),
            ScanLineAmount = Vector4.UnitX * 0.51f,
            ScanLineStrength = Vector4.UnitX * -8f,
            SphericalWarpAmount = Vector4.UnitX * 0.02f,
            NoiseAmount = Vector4.UnitX * 0.05f,
            InterferenceAmount = Vector4.UnitX * 0.25f,
            GrilleAmount = Vector4.UnitX * 0.3f,
            GrilleSize = Vector4.UnitX * 0.2f,
            VignetteAmount = Vector4.UnitX * 1.2f,
            VignetteIntensity = Vector4.UnitX * 0.6f,
            AberrationAmount = Vector4.UnitX * 0.95f,
            RollLineAmount = Vector4.UnitX * 0.4f,
            RollSpeed = Vector4.UnitX * 0.5f,
            PixelStrength = Vector4.UnitX * -1f,
        });
    }
}
