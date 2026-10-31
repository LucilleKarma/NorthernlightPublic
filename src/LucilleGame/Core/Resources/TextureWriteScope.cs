using System.Numerics;
using Northernlight.Core.Rendering;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a scope that can be
///     used to transiently write data into textures.
/// </summary>
public readonly ref struct TextureWriteScope : IDisposable
{
    private readonly bool preservePreviousContents;

    private readonly Color clearColor;

    private readonly Texture texture;

    private readonly FrameContext frame;

    /// <summary>
    ///     The renderer used for the submission of
    ///     data into the texture.
    /// </summary>
    public PooledRenderer Renderer
    {
        get;
    }

    /// <summary>
    ///     The sampler state used by this
    ///     scope.
    /// </summary>
    public Sampler Sampler
    {
        get;
    }

    public TextureWriteScope(RendererPool pool, Texture texture, FrameContext frame, Sampler sampler, bool preservePreviousContents = false, Color? clearColor = null)
    {
        this.clearColor = clearColor ?? Color.Transparent;
        this.preservePreviousContents = preservePreviousContents;
        this.texture = texture;
        this.frame = frame;
        Sampler = sampler;

        Renderer = pool.Rent();
    }

    public void Dispose()
    {
        try
        {
            Renderer.Prepare(frame);

            OrthographicCamera camera = new OrthographicCamera(texture.Size, Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, -1000f, 1000f);
            RenderPass.PassTargetInfo passTargetInfo = new RenderPass.PassTargetInfo
            {
                Texture = texture,
                ClearColor = clearColor.AsSdl(),
                LoadOp = preservePreviousContents ? SDL.GPULoadOp.Load : SDL.GPULoadOp.Clear,
                StoreOp = SDL.GPUStoreOp.Store
            };
            using RenderPass pass = new RenderPass(frame.CommandBuffer, texture, passTargetInfo, depthStencilInfo: default);
            Renderer.Render(frame, pass, camera);
        }
        finally
        {
            Renderer.Dispose();
        }
    }
}
