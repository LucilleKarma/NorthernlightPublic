using System.Numerics;
using System.Reflection;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Content.Borders;

/// <summary>
///     The system responsible for the handling of
///     borders beyond the game's lettebox.
/// </summary>
public sealed class BorderSystem
{
    private readonly Dictionary<RuntimeTypeHandle, Border> cachedBorders = [];

    private Scene activeScene;

    /// <summary>
    ///     The game responsbile for this system.
    /// </summary>
    public NorthernlightGame Game
    {
        get;
        private set;
    }

    /// <summary>
    ///     The background render target texture.
    /// </summary>
    public Texture BackingTarget
    {
        get;
        private set;
    }

    private static bool HasEmptyConstructor(Type t)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        return t.GetConstructor(flags, Type.EmptyTypes) is not null;
    }

    private static List<Border> FindBorders()
    {
        Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

        List<Type> siftedTypes = allTypes.
            Where(t => !t.IsAbstract && !t.ContainsGenericParameters && t.IsAssignableTo(typeof(Border))).
            Where(HasEmptyConstructor).ToList();

        List<Border> borders = siftedTypes.
            Select(t => (Border)Activator.CreateInstance(t)!).ToList();

        return borders;
    }

    public BorderSystem(NorthernlightGame game)
    {
        Game = game;
        EnsureTargetSize();

        List<Border> borders = FindBorders();
        foreach (Border border in borders)
        {
            cachedBorders[border.GetType().TypeHandle] = border;
            border.Load();
        }
    }

    private void EnsureTargetSize()
    {
        if (BackingTarget is not null && BackingTarget.Width == Game.Window.Width && BackingTarget.Height == Game.Window.Height)
            return;

        BackingTarget?.Dispose();
        SDL.GPUTextureCreateInfo textureCreationInfo = new SDL.GPUTextureCreateInfo()
        {
            Width = (uint)Game.Window.Width,
            Height = (uint)Game.Window.Height,
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
        };
        BackingTarget = new Texture(textureCreationInfo, Game.Device);
    }

    /// <summary>
    ///     Updates this system.
    /// </summary>
    public void Update()
    {
        foreach (Border border in cachedBorders.Values)
        {
            switch (Game.Config.BorderMode)
            {
                case BorderMode.None:
                    border.ShouldBeActive = false;
                    break;
                case BorderMode.Simple:
                    border.ShouldBeActive = border is SimpleBorder;
                    break;
                case BorderMode.Dynamic:
                    border.ShouldBeActive = border.DynamicallyDetermineActivity(Game);
                    break;
            }

            border.Update(Game);
        }
    }

    /// <summary>
    ///     Prepares this system for rendering.
    /// </summary>
    public void PrepareRender(FrameContext frame, SpriteRenderer sprites, Scene scene)
    {
        if (Game.CurrentScene is null)
            return;

        EnsureTargetSize();

        activeScene = scene;
        if (activeScene.MainCamera is not RestrictedOrthographicCamera camera)
            return;

        Vector3 baseDisplacement = new Vector3(scene.Window.Width - camera.EffectiveWidth, scene.Window.Height - camera.EffectiveHeight, 0f) * 0.5f;
        Rectangle letterboxArea = new Rectangle((int)baseDisplacement.X, (int)baseDisplacement.Y, camera.EffectiveWidth, camera.EffectiveHeight);

        bool anyActive = false;
        using (TextureWriteScope scope = BackingTarget.Scope(scene.SharedRenderers, frame, scene.Device.PointClamp))
        {
            foreach (Border border in cachedBorders.Values)
            {
                if (!border.Active)
                    continue;

                border.PrepareRender(Game.CurrentScene, frame, scope.Renderer, letterboxArea, BackingTarget.Size);
                anyActive = true;
            }
        }

        if (!anyActive)
            return;

        SpriteRenderer.ShaderWithPreparations? shaderPerparations = new SpriteRenderer.ShaderWithPreparations(Shaders.RotatedBoxRestrictionShader, PrepareRestrictionParameters);
        sprites.Enqueue(BackingTarget, new SpriteData(BackingTarget)
        {
            Position = new Vector3(scene.Window.Width * 0.5f, scene.Window.Height * 0.5f, 400f),
            Scale = Vector2.One,
            Tint = Color.White
        }, overridingShader: shaderPerparations);
    }

    internal void PrepareRestrictionParameters(FrameContext frame, RenderPass pass)
    {
        if (activeScene.MainCamera is not RestrictedOrthographicCamera camera)
            return;

        Vector3 baseDisplacement = new Vector3(activeScene.Window.Width - camera.EffectiveWidth, activeScene.Window.Height - camera.EffectiveHeight, 0f) * 0.5f;
        Rectangle letterboxArea = new Rectangle((int)baseDisplacement.X, (int)baseDisplacement.Y, camera.EffectiveWidth, camera.EffectiveHeight);
        frame.CommandBuffer.BindFragmentUniformData(0, new RadialWaveParticle.Uniforms()
        {
            BoxDimensions = new Vector4(letterboxArea.X, letterboxArea.Y, letterboxArea.Width, letterboxArea.Height),
            BoxRotation = Vector4.Zero,
            Invert = Vector4.One
        });
    }
}
