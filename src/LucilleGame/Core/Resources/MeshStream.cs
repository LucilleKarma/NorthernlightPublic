using Northernlight.Core.Rendering;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a GPU-bound mesh with
///     a vertex and index buffer.
/// </summary>
public sealed class MeshStream<TVertex> : IDisposable where TVertex : unmanaged
{
    private bool disposed;

    /// <summary>
    ///     The graphics device that owns
    ///     this mesh.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
        init;
    }

    /// <summary>
    ///     This mesh's vertex buffer.
    /// </summary>
    public VertexBuffer<TVertex> VertexBuffer
    {
        get;
        init;
    }

    /// <summary>
    ///     This mesh's index buffer.
    /// </summary>
    public IndexBuffer IndexBuffer
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of indices maintained by this mesh.
    /// </summary>
    public uint IndexCount
    {
        get;
        init;
    }

    public MeshStream(GraphicsDevice device, CommandBuffer commandBuffer, ReadOnlySpan<TVertex> vertices, ReadOnlySpan<uint> indices)
    {
        Device = device;
        IndexCount = (uint)indices.Length;
        VertexBuffer = new VertexBuffer<TVertex>(Device, commandBuffer, vertices);
        IndexBuffer = new IndexBuffer(Device, commandBuffer, indices);
    }

    public MeshStream(GraphicsDevice device, CommandBuffer commandBuffer, ICpuMesh<TVertex> meshCpu)
    {
        Device = device;
        IndexCount = (uint)meshCpu.Indices.Length;
        VertexBuffer = new VertexBuffer<TVertex>(Device, commandBuffer, meshCpu.Vertices);
        IndexBuffer = new IndexBuffer(Device, commandBuffer, meshCpu.Indices);
    }

    /// <summary>
    ///     Renders this mesh.
    /// </summary>
    public void Render(RenderPass pass, uint indexCount, uint instanceCount)
    {
        pass.BindVertexBuffers(0, VertexBuffer);
        pass.BindIndexBuffer(IndexBuffer);
        pass.RenderIndexed(indexCount, instanceCount);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}
