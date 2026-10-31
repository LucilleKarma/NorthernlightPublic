using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a compute pass.
/// </summary>
public sealed class ComputePass : IDisposable
{
    private bool disposed;

    /// <summary>
    ///     The handle associated with this
    ///     compute pass.
    /// </summary>
    internal nint Handle
    {
        get;
        private set;
    }

    public ComputePass(CommandBuffer commandBuffer, params Span<StorageBuffer> storageBuffers)
    {
        SDL.GPUStorageBufferReadWriteBinding[] bufferBindings = new SDL.GPUStorageBufferReadWriteBinding[storageBuffers.Length];
        for (int i = 0; i < bufferBindings.Length; i++)
        {
            bufferBindings[i] = new SDL.GPUStorageBufferReadWriteBinding()
            {
                Buffer = storageBuffers[i].Handle,
                Cycle = 0
            };
        }

        Handle = SDL.BeginGPUComputePass(commandBuffer.Handle, Array.Empty<SDL.GPUStorageTextureReadWriteBinding>(), 0, bufferBindings, (uint)bufferBindings.Length);
    }

    /// <summary>
    ///     Binds read-only storage buffers to this 
    ///     render pass.
    /// </summary>
    public void BindReadOnlyStorageBuffers(uint firstSlot, params Span<StorageBuffer> buffers)
    {
        nint[] bufferHandles = new nint[buffers.Length];

        for (int i = 0; i < bufferHandles.Length; i++)
            bufferHandles[i] = buffers[i].Handle;

        SDL.BindGPUComputeStorageBuffers(Handle, firstSlot, bufferHandles, (uint)buffers.Length);
    }

    /// <summary>
    ///     Dispatches a compute shader.
    /// </summary>
    public void Dispatch(ComputeGroupCounts groupCounts)
    {
        SDL.DispatchGPUCompute(Handle, groupCounts.X, groupCounts.Y, groupCounts.Z);
    }

    public void Dispose()
    {
        if (disposed || Handle == 0)
            return;

        disposed = true;
        SDL.EndGPUComputePass(Handle);
    }
}
