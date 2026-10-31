using System.Runtime.CompilerServices;
using SDL3;

namespace Northernlight.Core.Resources;

public abstract class StorageBuffer
{
    /// <summary>
    ///     The handle of this buffer to its underlying
    ///     SDL resource.
    /// </summary>
    internal nint Handle
    {
        get;
        set;
    }
}

/// <summary>
///     Represents an instance of a storage buffer.
/// </summary>
public sealed class StorageBuffer<T> : StorageBuffer, IDisposable where T : unmanaged
{
    private bool disposed;

    /// <summary>
    ///     The amount of elements contained within
    ///     this buffer.
    /// </summary>
    public uint Count
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

    /// <summary>
    ///     Whether this buffer's contents can be read back or not.
    /// </summary>
    public bool CanBeRead
    {
        get;
        private set;
    }

    /// <summary>
    ///     Whether this buffer's contents can be written to or not.
    /// </summary>
    public bool CanBeWrittenTo
    {
        get;
        private set;
    }

    public T[] this[GraphicsDevice device, uint startIndex, uint length]
    {
        get
        {
            if (!CanBeRead)
                throw new InvalidOperationException("Storage buffer contents are write-only.");
            ObjectDisposedException.ThrowIf(disposed, this);

            T[] result = new T[length];

            using CommandBuffer commandBuffer = device.GenerateCommandBuffer();
            DownloadFromGPU(commandBuffer, result, startIndex);

            return result;
        }
        set
        {
            if (!CanBeWrittenTo)
                throw new InvalidOperationException("Storage buffer contents are read-only.");
            ObjectDisposedException.ThrowIf(disposed, this);

            using CommandBuffer commandBuffer = device.GenerateCommandBuffer();
            UploadDataToGPU(commandBuffer, value, startIndex);
        }
    }

    public T this[GraphicsDevice device, uint index]
    {
        get => this[device, index, 1][0];
        set => this[device, index, 1] = [value];
    }

    public T[] this[GraphicsDevice device]
    {
        get => this[device, 0, Count];
    }

    public StorageBuffer(GraphicsDevice device, uint capacity, bool read, bool write)
    {
        uint size = (uint)(capacity * Unsafe.SizeOf<T>());

        CanBeRead = read;
        CanBeWrittenTo = write;

        SDL.GPUBufferUsageFlags flags = 0;
        flags |= read ? SDL.GPUBufferUsageFlags.ComputeStorageRead : 0;
        flags |= write ? SDL.GPUBufferUsageFlags.ComputeStorageWrite : 0;
        if (!read && !write)
            throw new InvalidOperationException("A storage must at least be readable or writable.");

        var bufferInfo = new SDL.GPUBufferCreateInfo
        {
            Size = size,
            Usage = flags
        };
        Handle = SDL.CreateGPUBuffer(device.Handle, bufferInfo);
        Count = capacity;

        Device = device;
    }

    public StorageBuffer(GraphicsDevice device, ReadOnlySpan<T> data, bool read, bool write)
        : this(device, (uint)data.Length, read, write)
    {
        using CommandBuffer commandBuffer = device.GenerateCommandBuffer();
        UploadDataToGPU(commandBuffer, data);
    }

    /// <summary>
    ///     Uploads data to this buffer.
    /// </summary>
    internal void UploadDataToGPU(CommandBuffer commandBuffer, ReadOnlySpan<T> data, uint startIndex = 0)
    {
        if (data.Length + startIndex > Count)
            throw new IndexOutOfRangeException("Bounds of storage are exceeded by the data length and/or starting index.");

        uint elementSize = (uint)Unsafe.SizeOf<T>();

        using TransferBuffer<T> uploadBuffer = new TransferBuffer<T>(Device, SDL.GPUTransferBufferUsage.Upload, data, (uint)(data.Length * elementSize));
        uploadBuffer.Submit(data);

        using var copyScope = commandBuffer.Copy();

        SDL.GPUTransferBufferLocation source = new SDL.GPUTransferBufferLocation()
        {
            TransferBuffer = uploadBuffer.Handle,
            Offset = 0
        };
        SDL.GPUBufferRegion destination = new SDL.GPUBufferRegion()
        {
            Buffer = Handle,
            Offset = (uint)(startIndex * Unsafe.SizeOf<T>()),
            Size = uploadBuffer.Size
        };
        copyScope.UploadToGPUBuffer(source, destination, true);
    }

    /// <summary>
    ///     Reads back GPU data maintained by this buffer
    ///     back to the CPU.
    /// </summary>
    internal void DownloadFromGPU(CommandBuffer commandBuffer, Span<T> results, uint startIndex = 0)
    {
        if (results.Length + startIndex > Count)
            throw new IndexOutOfRangeException("Bounds of storage are exceeded by the data length and/or starting index.");

        using TransferBuffer<T> downloadBuffer = new TransferBuffer<T>(Device, SDL.GPUTransferBufferUsage.Download, (uint)(results.Length * Unsafe.SizeOf<T>()));

        using (var copyScope = commandBuffer.Copy())
        {
            SDL.GPUBufferRegion source = new SDL.GPUBufferRegion()
            {
                Buffer = Handle,
                Offset = (uint)(startIndex * Unsafe.SizeOf<T>()),
                Size = downloadBuffer.Size
            };
            SDL.GPUTransferBufferLocation destination = new SDL.GPUTransferBufferLocation()
            {
                TransferBuffer = downloadBuffer.Handle,
                Offset = 0,
            };
            copyScope.DownloadFromGPUBuffer(source, destination);
        }

        commandBuffer.Dispose();
        Device.WaitForGPUIdle();

        downloadBuffer.CopyTo(results);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);
        if (Handle != nint.Zero)
            SDL.ReleaseGPUBuffer(Device.Handle, Handle);
    }
}
