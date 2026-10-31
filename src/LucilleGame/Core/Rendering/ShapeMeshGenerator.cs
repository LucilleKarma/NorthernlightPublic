namespace Northernlight.Core.Rendering;

/// <summary>
///     A provider for a given mesh shape, such as cubes, cylinders, spheres, etc.
/// </summary>
public abstract class ShapeMeshGenerator<TVertex> where TVertex : unmanaged
{
    /// <summary>
    ///     The type of primitives that are to be composed from this shape generator.
    /// </summary>
    public abstract GPUPrimitiveType PrimitiveType
    {
        get;
    }

    /// <summary>
    ///     The amount of vertices to generate for this shape, based on whatever settings are used for this generator.
    /// </summary>
    protected abstract int VertexCount
    {
        get;
    }

    /// <summary>
    ///     The amount of indices to generate for this shape, based on whatever settings are used for this generator.
    /// </summary>
    protected abstract int IndexCount
    {
        get;
    }

    /// <summary>
    ///     The amount of primitive shapes to generate for this shape, based on whatever settings are used for this generator.
    /// </summary>
    protected abstract int PrimitiveCount
    {
        get;
    }

    /// <summary>
    ///     Assigns data to vertices.
    /// </summary>
    protected abstract void AssignVertices(ref Span<TVertex> vertices);

    /// <summary>
    ///     Assigns data to indices.
    /// </summary>
    protected abstract void AssignIndices(ref Span<uint> indices);

    /// <summary>
    ///     Makes a new shape mesh.
    /// </summary>
    public OwnedMesh<TVertex> Make()
    {
        int vertexCount = VertexCount;
        int indexCount = IndexCount;
        TVertex[] vertices = new TVertex[vertexCount];
        uint[] indices = new uint[indexCount];

        Span<TVertex> verticesSpan = vertices;
        Span<uint> indicesSpan = indices;

        AssignVertices(ref verticesSpan);
        AssignIndices(ref indicesSpan);
        return new OwnedMesh<TVertex>(PrimitiveType, vertices, indices, PrimitiveCount);
    }
}
