using System.Runtime.CompilerServices;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents a basis for a GPU buffer.
/// </summary>
public abstract class GpuBuffer<T> : IDisposable where T : unmanaged
{
    /// <summary>
    ///     Whether this buffer has been disposed or not.
    /// </summary>
    protected bool Disposed
    {
        get;
        private set;
    }

    protected TransferBuffer<T> transferBuffer;

    /// <summary>
    ///     The handle of this buffer to its underlying
    ///     GPU resource.
    /// </summary>
    public nint Handle
    {
        get;
        protected set;
    }

    /// <summary>
    ///     Uploads CPU data to this index buffer.
    /// </summary>
    public void UploadData(GraphicsDevice device, CommandBuffer commandBuffer, ReadOnlySpan<T> data)
    {
        if (transferBuffer is null)
            transferBuffer = new TransferBuffer<T>(device, SDL.GPUTransferBufferUsage.Upload, data, (uint)(data.Length * Unsafe.SizeOf<T>()));
        else
            transferBuffer.Submit(data);

        using var copyScope = commandBuffer.Copy();

        copyScope.UploadToGPUBuffer(transferBuffer, this, transferBuffer.Size, true);
    }

    public virtual void Dispose()
    {
        if (Disposed)
            return;

        Disposed = true;

        GC.SuppressFinalize(this);
        transferBuffer.Dispose();
    }
}
