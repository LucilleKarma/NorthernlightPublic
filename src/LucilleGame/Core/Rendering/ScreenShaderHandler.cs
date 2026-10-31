using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a screen shader handler.
/// </summary>
public sealed class ScreenShaderHandler
{
    private readonly Game game;

    private readonly Dictionary<Shader, ScreenShader> activeShaders = [];

    private readonly List<Shader> keysToRemove = [];

    /// <summary>
    ///     Whether this system is active or not.
    /// </summary>
    public bool Active => activeShaders.Count >= 1;

    /// <summary>
    ///     All active shaders.
    /// </summary>
    public List<ScreenShader> ActiveShaders => activeShaders.Values.ToList();

    /// <summary>
    ///     The screen render target.
    /// </summary>
    public TextureRenderer ScreenTarget
    {
        get;
        private set;
    }

    /// <summary>
    ///     The alt screen render target.
    /// </summary>
    public TextureRenderer AltScreenTarget
    {
        get;
        private set;
    }

    public ScreenShaderHandler(Game game)
    {
        this.game = game;

        GenerateTargets((uint)game.Window.Width, (uint)game.Window.Height);

        game.Window.OnResize += AutoResizeTargets;
    }

    private void AutoResizeTargets(int w, int h) => GenerateTargets((uint)w, (uint)h);

    private void GenerateTargets(uint w, uint h)
    {
        SDL.GPUTextureCreateInfo textureCreationInfo = new SDL.GPUTextureCreateInfo()
        {
            Width = w,
            Height = h,
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
        };

        ScreenTarget?.Dispose();
        ScreenTarget = new TextureRenderer(textureCreationInfo, game.Device, game.Window);

        AltScreenTarget?.Dispose();
        AltScreenTarget = new TextureRenderer(textureCreationInfo, game.Device, game.Window);
    }

    /// <summary>
    ///     Activates a new screen shader.
    /// </summary>
    public void Activate(SpriteRenderer.ShaderWithPreparations shaderPreparations, bool startAtFullIntensity = false)
    {
        if (activeShaders.TryGetValue(shaderPreparations.Shader, out ScreenShader? existingShader))
            existingShader.Active = true;
        else
        {
            activeShaders[shaderPreparations.Shader] = new ScreenShader(shaderPreparations)
            {
                Active = true,
                Intensity = startAtFullIntensity ? 1f : 0f,
            };
        }
    }

    /// <summary>
    ///     Clears all active afterimages.
    /// </summary>
    public void Clear() => activeShaders.Clear();

    /// <summary>
    ///     Updates this handler.
    /// </summary>
    public void Update()
    {
        keysToRemove.Clear();
        foreach (ScreenShader shader in activeShaders.Values)
        {
            shader.Update(game);
            if (!shader.Active && shader.Intensity <= 0f)
                keysToRemove.Add(shader.ShaderPreparations.Shader);
        }

        foreach (Shader shader in keysToRemove)
            activeShaders.Remove(shader);
    }
}
