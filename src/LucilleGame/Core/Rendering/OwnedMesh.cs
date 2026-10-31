namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a heap-allocated, CPU-bound instance
///     of a renderable mesh.
/// </summary>
public sealed class OwnedMesh<TVertex> : ICpuMesh<TVertex> where TVertex : unmanaged
{
    private readonly TVertex[] vertices;

    private readonly uint[] indices;

    /// <summary>
    ///     The amount of primitives this mesh possesses.
    /// </summary>
    public int PrimitiveCount
    {
        get;
    }

    /// <summary>
    ///     The amount of vertices this mesh possesses.
    /// </summary>
    public int VertexCount => vertices.Length;

    /// <summary>
    ///     The amount of indices this mesh possesses.
    /// </summary>
    public int IndexCount => indices.Length;

    /// <summary>
    ///     The vertices associated with this mesh.
    /// </summary>
    public Span<TVertex> Vertices => vertices;

    /// <summary>
    ///     The indices associated with this mesh.
    /// </summary>
    public Span<uint> Indices => indices;

    /// <summary>
    ///     The type of primitives contained within this mesh.
    /// </summary>
    public GPUPrimitiveType PrimitiveType
    {
        get;
    }

    public OwnedMesh(GPUPrimitiveType primitiveType, int vertexCapacity, int indexCapacity, int primitiveCount) :
        this(primitiveType, new TVertex[vertexCapacity], new uint[indexCapacity], primitiveCount)
    { }

    public OwnedMesh(GPUPrimitiveType primitiveType, TVertex[] vertices, uint[] indices, int primitiveCount)
    {
        PrimitiveType = primitiveType;
        this.vertices = vertices;
        this.indices = indices;
        PrimitiveCount = primitiveCount;
    }
}
