using System.Runtime.CompilerServices;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a vertex buffer.
/// </summary>
public sealed class VertexBuffer<T> : GpuBuffer<T>, IDisposable where T : unmanaged
{
    /// <summary>
    ///     The graphics device that owns this buffer.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    public VertexBuffer(GraphicsDevice device, CommandBuffer commandBuffer, ReadOnlySpan<T> vertices)
    {
        uint size = (uint)(vertices.Length * Unsafe.SizeOf<T>());
        var bufferInfo = new SDL.GPUBufferCreateInfo
        {
            Size = size,
            Usage = SDL.GPUBufferUsageFlags.Vertex
        };
        Handle = SDL.CreateGPUBuffer(device.Handle, bufferInfo);

        UploadData(device, commandBuffer, vertices);
        Device = device;
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
