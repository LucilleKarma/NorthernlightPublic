using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core;

/// <summary>
///     A simple, transient provider for the handling of
///     per-frame rendering data.
/// </summary>
public sealed class FrameContext : IDisposable
{
    /// <summary>
    ///     Whether this frame should be skipped or not, due
    ///     to the swapchain texture handle being undefined.
    ///     This most commonly occurs if the game is minimized.
    /// </summary>
    public bool SkipFrame => SwapchainTexture.Handle == 0 || Window is { IsFocused: false, PauseUpdatesOnLossOfFocus: true };

    /// <summary>
    ///     The graphics device.
    /// </summary>
    public required GraphicsDevice Device
    {
        get;
        init;
    }

    /// <summary>
    ///     The window.
    /// </summary>
    public required Window Window
    {
        get;
        init;
    }

    /// <summary>
    ///     The acquired swapchain texture.
    /// </summary>
    public required Texture SwapchainTexture
    {
        get;
        init;
    }

    /// <summary>
    ///     A base command buffer.
    /// </summary>
    public required CommandBuffer CommandBuffer
    {
        get;
        init;
    }

    /// <summary>
    ///     Begins a render pass for this frame.
    /// </summary>
    public RenderPass BeginRenderPass(Color clearColor, SDL.GPUDepthStencilTargetInfo depthStencilInfo, Texture? overridingOutputTexture = null, bool preservePreviousContents = false)
    {
        var passTargetInfo = new RenderPass.PassTargetInfo
        {
            Texture = overridingOutputTexture ?? SwapchainTexture,
            ClearColor = clearColor.AsSdl(),
            LoadOp = preservePreviousContents ? SDL.GPULoadOp.Load : SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store,
            Cycle = !preservePreviousContents,
        };

        return new RenderPass(CommandBuffer, overridingOutputTexture ?? SwapchainTexture, passTargetInfo, depthStencilInfo);
    }

    public void Dispose()
    {

    }
}
