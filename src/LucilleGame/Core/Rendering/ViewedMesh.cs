namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a span-view-based, CPU-bound instance
///     of a renderable mesh.
/// </summary>
public readonly ref struct ViewedMesh<TVertex> : ICpuMesh<TVertex> where TVertex : unmanaged
{
    /// <summary>
    ///     The vertices associated with this mesh.
    /// </summary>
    public Span<TVertex> Vertices
    {
        get;
    }

    /// <summary>
    ///     The indices associated with this mesh.
    /// </summary>
    public Span<uint> Indices
    {
        get;
    }

    /// <summary>
    ///     The type of primitives contained within this mesh.
    /// </summary>
    public GPUPrimitiveType PrimitiveType
    {
        get;
    }

    /// <summary>
    ///     The amount of primitives this mesh possesses.
    /// </summary>
    public int PrimitiveCount
    {
        get;
    }

    public ViewedMesh(GPUPrimitiveType primitiveType, Span<TVertex> vertices, Span<uint> indices, int primitiveCount)
    {
        PrimitiveType = primitiveType;
        Vertices = vertices;
        Indices = indices;
        PrimitiveCount = primitiveCount;
    }
}
