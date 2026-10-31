namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a generic instance of a CPU-bound
///     mesh instance.
/// </summary>
public interface ICpuMesh<TVertex> where TVertex : unmanaged
{
    /// <summary>
    ///     The vertices associated with this mesh.
    /// </summary>
    Span<TVertex> Vertices
    {
        get;
    }

    /// <summary>
    ///     The indices associated with this mesh.
    /// </summary>
    Span<uint> Indices
    {
        get;
    }

    /// <summary>
    ///     The type of primitives contained within this mesh.
    /// </summary>
    GPUPrimitiveType PrimitiveType
    {
        get;
    }

    /// <summary>
    ///     The amount of primitives this mesh possesses.
    /// </summary>
    int PrimitiveCount
    {
        get;
    }
}
