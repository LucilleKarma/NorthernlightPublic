using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a graphics device.
/// </summary>
public sealed class GraphicsDevice : IDisposable
{
    private bool disposed;

    /// <summary>
    ///     The handle associated with this
    ///     graphics device.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The shader code format accepted by this
    ///     graphics device.
    /// </summary>
    public SDL.GPUShaderFormat ShaderFormat
    {
        get;
        private set;
    }

    /// <summary>
    ///     The depth buffer's backing
    ///     format. Returns 0 if the depth buffer
    ///     is unused.
    /// </summary>
    public SDL.GPUTextureFormat DepthBufferFormat
    {
        get;
        private set;
    }

    /// <summary>
    ///     A point clamp sampler state.
    /// </summary>
    public readonly Sampler PointClamp;

    /// <summary>
    ///     A linear clamp sampler state.
    /// </summary>
    public readonly Sampler LinearClamp;

    /// <summary>
    ///     A point wrap sampler state.
    /// </summary>
    public readonly Sampler PointWrap;

    /// <summary>
    ///     A linear wrap sampler state.
    /// </summary>
    public readonly Sampler LinearWrap;

    public GraphicsDevice(SDL.GPUShaderFormat shaderFormat, SDL.GPUTextureFormat depthBufferFormat = 0)
    {
        ShaderFormat = shaderFormat;
        Handle = SDL.CreateGPUDevice(ShaderFormat, true, null);
        if (Handle == 0)
            SDL.Log($"SDL_CreateGPUDevice() failed: {SDL.GetError()}");

        PointClamp = new Sampler(this, new SDL.GPUSamplerCreateInfo()
        {
            MinFilter = SDL.GPUFilter.Nearest,
            MagFilter = SDL.GPUFilter.Nearest,
            MipmapMode = SDL.GPUSamplerMipmapMode.Nearest,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge
        });
        LinearClamp = new Sampler(this, new SDL.GPUSamplerCreateInfo()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge
        });
        PointWrap = new Sampler(this, new SDL.GPUSamplerCreateInfo()
        {
            MinFilter = SDL.GPUFilter.Nearest,
            MagFilter = SDL.GPUFilter.Nearest,
            MipmapMode = SDL.GPUSamplerMipmapMode.Nearest,
            AddressModeU = SDL.GPUSamplerAddressMode.Repeat,
            AddressModeV = SDL.GPUSamplerAddressMode.Repeat
        });
        LinearWrap = new Sampler(this, new SDL.GPUSamplerCreateInfo()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.Repeat,
            AddressModeV = SDL.GPUSamplerAddressMode.Repeat
        });

        DepthBufferFormat = depthBufferFormat;
    }

    /// <summary>
    ///     Generates a new command buffer.
    /// </summary>
    public CommandBuffer GenerateCommandBuffer() => new CommandBuffer(this);

    /// <summary>
    ///     Generates a transient context for a single
    ///     frame.
    /// </summary>
    /// <remarks>
    ///     It is expected that this will be disposed by
    ///     the end of the frame.
    /// </remarks>
    public FrameContext BeginFrame(Window window, CommandBuffer commandBuffer)
    {
        return new FrameContext()
        {
            Device = this,
            Window = window,
            CommandBuffer = commandBuffer,
            SwapchainTexture = commandBuffer.WaitAndAcquireGPUSwapchainTexture(window)
        };
    }

    /// <summary>
    ///     Claims a window as used by this graphics device.
    /// </summary>
    public void ClaimWindow(Window window)
    {
        if (!SDL.ClaimWindowForGPUDevice(Handle, window.Handle))
            SDL.Log($"SDL_ClaimWindowForGPUDevice() failed: {SDL.GetError()}");
    }

    /// <summary>
    ///     Returns the color target description of the swapchain.
    /// </summary>
    public SDL.GPUColorTargetDescription GetSwapchainColorTargetDescription(Window window) => new SDL.GPUColorTargetDescription()
    {
        Format = SDL.GetGPUSwapchainTextureFormat(Handle, window.Handle)
    };

    /// <summary>
    ///     Sets parameters for the swapchain texture,
    /// </summary>
    public void SetGPUSwapchainParameters(Window window, SDL.GPUSwapchainComposition composition, SDL.GPUPresentMode presentMode)
    {
        SDL.SetGPUSwapchainParameters(Handle, window.Handle, composition, presentMode);
    }

    /// <summary>
    ///     Blocks the thread until the GPU is fully idle.
    /// </summary>
    public void WaitForGPUIdle() => SDL.WaitForGPUIdle(Handle);

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        SDL.DestroyGPUDevice(Handle);
    }
}
