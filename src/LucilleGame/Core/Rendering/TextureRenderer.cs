using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

public record TextureRenderer : IDisposable
{
    public readonly Texture Texture;

    public readonly Renderer Renderer;

    public TextureRenderer(SDL.GPUTextureCreateInfo textureCreationInfo, GraphicsDevice device, Window window)
    {
        Texture = new Texture(textureCreationInfo, device);

        Renderer = new Renderer();
        Renderer.Add(new SpriteRenderer(device, window, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = device.PointClamp,
            InitialCapacity = 4,
            ColorTargetDescription = device.GetSwapchainColorTargetDescription(window),
            DepthStencilState = DepthStencilState.Default,
            OverridingFragmentShader = Shaders.BasicFrag
        }));
        Renderer.Add(new MeshRenderer(device, window));
        Renderer.Add(new TextRenderer(device, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = device.PointClamp,
            ColorTargetDescription = device.GetSwapchainColorTargetDescription(window),
            DepthStencilState = DepthStencilState.Default
        }));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Texture.Dispose();
        Renderer.Dispose();
    }
}
