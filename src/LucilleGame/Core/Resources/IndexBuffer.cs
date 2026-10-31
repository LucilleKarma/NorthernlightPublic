using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of an index buffer.
/// </summary>
public sealed class IndexBuffer : GpuBuffer<uint>, IDisposable
{
    /// <summary>
    ///     The graphics device that owns this buffer.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    /// <summary>
    ///     The size of a single element, in bytes.
    /// </summary>
    public uint ElementSize
    {
        get;
    }

    /// <summary>
    ///     An SDL-friendly format of <see cref="ElementSize"/>.
    /// </summary>
    public SDL.GPUIndexElementSize SizeFormat
    {
        get;
    }

    public IndexBuffer(GraphicsDevice device, CommandBuffer commmandBuffer, ReadOnlySpan<uint> indices)
    {
        Device = device;
        ElementSize = sizeof(uint);
        SizeFormat = SDL.GPUIndexElementSize.IndexElementSize32Bit;

        uint size = (uint)(indices.Length * ElementSize);

        var bufferInfo = new SDL.GPUBufferCreateInfo
        {
            Size = size,
            Usage = SDL.GPUBufferUsageFlags.Index
        };
        Handle = SDL.CreateGPUBuffer(device.Handle, bufferInfo);

        UploadData(device, commmandBuffer, indices);
    }

    public override void Dispose()
    {
        if (Disposed)
            return;

        base.Dispose();
        if (Handle != nint.Zero)
            SDL.ReleaseGPUBuffer(Device.Handle, Handle);
    }
}
