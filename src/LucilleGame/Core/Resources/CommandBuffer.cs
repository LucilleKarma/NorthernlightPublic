using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a command buffer.
/// </summary>
public sealed class CommandBuffer : IDisposable
{
    private bool disposed;

    /// <summary>
    ///     The handle associated with this
    ///     command buffer.
    /// </summary>
    internal nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The graphics device that owns this
    ///     command buffer.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    public record CopyPassScope : IDisposable
    {
        private readonly CommandBuffer owner;

        /// <summary>
        ///     The copy pass handle used by this scope.
        /// </summary>
        public readonly nint PassHandle;

        public CopyPassScope(CommandBuffer owner)
        {
            this.owner = owner;
            PassHandle = SDL.BeginGPUCopyPass(owner.Handle);
        }

        // This does not appear to be
        // included in the standard SDL function suite.
        // Perhaps an obscure RTFM issue, but I'll assume
        // for now that this is an accidental API deficiency.
        private static unsafe void DownloadFromGPUBuffer(nint copyPass, in global::SDL3.SDL.GPUBufferRegion source, in global::SDL3.SDL.GPUTransferBufferLocation destination)
        {
            fixed (global::SDL3.SDL.GPUTransferBufferLocation* __destination_native = &destination)
            fixed (global::SDL3.SDL.GPUBufferRegion* __source_native = &source)
            {
                __PInvoke(copyPass, __source_native, __destination_native);
            }

            [global::System.Runtime.InteropServices.DllImportAttribute("SDL3", EntryPoint = "SDL_DownloadFromGPUBuffer", ExactSpelling = true)]
            [global::System.Runtime.InteropServices.UnmanagedCallConvAttribute(CallConvs = new global::System.Type[] { typeof(global::System.Runtime.CompilerServices.CallConvCdecl) })]
            static extern unsafe void __PInvoke(nint __copyPass_native, global::SDL3.SDL.GPUBufferRegion* __source_native, global::SDL3.SDL.GPUTransferBufferLocation* __destination_native);
        }

        /// <summary>
        ///     Uploads data from a transfer buffer to a GPU buffer.
        /// </summary>
        public void UploadToGPUBuffer(SDL.GPUTransferBufferLocation source, SDL.GPUBufferRegion destination, bool cycle) =>
            SDL.UploadToGPUBuffer(PassHandle, source, destination, cycle);

        /// <summary>
        ///     Uploads data from a transfer buffer to a GPU buffer.
        /// </summary>
        public void UploadToGPUBuffer<T>(TransferBuffer<T> source, GpuBuffer<T> destination, uint destinationSize, bool cycle, uint sourceOffset = 0, uint destinationOffset = 0) where T : unmanaged
        {
            SDL.GPUTransferBufferLocation sourceLocation = new SDL.GPUTransferBufferLocation()
            {
                Offset = sourceOffset,
                TransferBuffer = source.Handle
            };
            SDL.GPUBufferRegion destinationLocation = new SDL.GPUBufferRegion()
            {
                Offset = destinationOffset,
                Buffer = destination.Handle,
                Size = destinationSize
            };
            SDL.UploadToGPUBuffer(PassHandle, sourceLocation, destinationLocation, cycle);
        }

        /// <summary>
        ///     Downloads data from a a GPU buffer to a transfer buffer.
        /// </summary>
        public void DownloadFromGPUBuffer(SDL.GPUBufferRegion source, SDL.GPUTransferBufferLocation destination) =>
            DownloadFromGPUBuffer(PassHandle, source, destination);

        public void Dispose()
        {
            SDL.EndGPUCopyPass(PassHandle);
            GC.SuppressFinalize(this);
        }
    }

    public CommandBuffer(GraphicsDevice device)
    {
        Handle = SDL.AcquireGPUCommandBuffer(device.Handle);
        if (Handle == 0)
            SDL.Log($"AcquireGPUCommandBuffer failed: {SDL.GetError()}");

        Device = device;
    }

    /// <summary>
    ///     Waits for a GPU-bound swapchain texture to be generated, and returns
    ///     the result.
    /// </summary>
    public Texture WaitAndAcquireGPUSwapchainTexture(Window window)
    {
        if (!SDL.WaitAndAcquireGPUSwapchainTexture(Handle, window.Handle, out nint swapchainTextureHandle, out _, out _))
            throw new Exception($"WaitAndAcquireGPUSwapchainTexture failed: {SDL.GetError()}");

        return new Texture(swapchainTextureHandle, (uint)window.Width, (uint)window.Height, Device, Device.GetSwapchainColorTargetDescription(window).Format);
    }

    /// <summary>
    ///     Binds vertex shader uniform data to this
    ///     buffer.
    /// </summary>
    public unsafe void BindVertexUniformData<TData>(uint slotIndex, TData data) where TData : unmanaged
    {
        SDL.PushGPUVertexUniformData(Handle, slotIndex, (nint)(&data), (uint)sizeof(TData));
    }

    /// <summary>
    ///     Binds fragment shader uniform data to this
    ///     buffer.
    /// </summary>
    public unsafe void BindFragmentUniformData<TData>(uint slotIndex, TData data) where TData : unmanaged
    {
        SDL.PushGPUFragmentUniformData(Handle, slotIndex, (nint)(&data), (uint)sizeof(TData));
    }

    /// <summary>
    ///     Binds compute shader uniform data to this
    ///     buffer.
    /// </summary>
    public unsafe void BindComputeUniformData<TData>(uint slotIndex, TData data) where TData : unmanaged
    {
        SDL.PushGPUComputeUniformData(Handle, slotIndex, (nint)(&data), (uint)sizeof(TData));
    }

    /// <summary>
    ///     Binds vertex storage buffers to this 
    ///     render pass.
    /// </summary>
    public void BindVertexStorageBuffers(uint firstSlot, params StorageBuffer[] buffers)
    {
        nint[] bufferHandles = new nint[buffers.Length];

        for (int i = 0; i < bufferHandles.Length; i++)
            bufferHandles[i] = buffers[i].Handle;

        SDL.BindGPUVertexStorageBuffers(Handle, firstSlot, bufferHandles, (uint)buffers.Length);
    }

    /// <summary>
    ///     Creates a copy pass scope for this buffer.
    /// </summary>
    public CopyPassScope Copy() => new CopyPassScope(this);

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        SDL.SubmitGPUCommandBuffer(Handle);
    }
}
