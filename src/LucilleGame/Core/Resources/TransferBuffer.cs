using System.Runtime.CompilerServices;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a transfer buffer.
/// </summary>
public sealed unsafe class TransferBuffer<T> : IDisposable where T : unmanaged
{
    private bool disposed;

    /// <summary>
    ///     The handle of this buffer to its underlying
    ///     SDL resource.
    /// </summary>
    internal nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The size of this buffer in bytes.
    /// </summary>
    public uint Size
    {
        get;
        private set;
    }

    /// <summary>
    ///     The graphics device that owns this buffer.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    public readonly record struct MapScope : IDisposable
    {
        private readonly TransferBuffer<T> owner;

        /// <summary>
        ///     The transfer handle used by this scope.
        /// </summary>
        public readonly nint TransferHandle;

        public MapScope(TransferBuffer<T> owner)
        {
            this.owner = owner;
            TransferHandle = SDL.MapGPUTransferBuffer(owner.Device.Handle, owner.Handle, false);
        }

        public void Dispose()
        {
            if (TransferHandle == 0)
                return;

            SDL.UnmapGPUTransferBuffer(owner.Device.Handle, owner.Handle);
            GC.SuppressFinalize(this);
        }
    }

    public TransferBuffer(GraphicsDevice device, SDL.GPUTransferBufferUsage usage, uint bufferSize)
    {
        Device = device;
        Handle = SDL.CreateGPUTransferBuffer(Device.Handle, new SDL.GPUTransferBufferCreateInfo()
        {
            Usage = usage,
            Size = bufferSize
        });
        Size = bufferSize;
    }

    public TransferBuffer(GraphicsDevice device, SDL.GPUTransferBufferUsage usage, ReadOnlySpan<T> data, uint bufferSize) :
        this(device, usage, bufferSize)
    {
        Submit(data);
    }

    /// <summary>
    ///     Submits CPU data to this buffer.
    /// </summary>
    public void Submit(ReadOnlySpan<T> data)
    {
        uint bufferSize = (uint)(data.Length * Unsafe.SizeOf<T>());
        using MapScope scope = Map();
        fixed (T* dataPointer = data)
        {
            Buffer.MemoryCopy(
                source: dataPointer,
                destination: (void*)scope.TransferHandle,
                destinationSizeInBytes: bufferSize,
                sourceBytesToCopy: (uint)(sizeof(T) * data.Length)
            );
        }
    }

    /// <summary>
    ///     Copies the contents of this transfer buffer to
    ///     a managed span.
    /// </summary>
    public void CopyTo(Span<T> data)
    {
        using MapScope scope = Map();
        fixed (T* dataPointer = data)
        {
            Buffer.MemoryCopy(
                source: (void*)scope.TransferHandle,
                destination: dataPointer,
                destinationSizeInBytes: Size,
                sourceBytesToCopy: (uint)(sizeof(T) * data.Length)
            );
        }
    }

    /// <summary>
    ///     Creates a temporary map scope for this buffer.
    /// </summary>
    public MapScope Map() => new MapScope(this);

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);
        if (Handle != nint.Zero)
            SDL.ReleaseGPUTransferBuffer(Device.Handle, Handle);
    }
}
