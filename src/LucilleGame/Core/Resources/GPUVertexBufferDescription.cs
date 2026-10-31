using System.Runtime.InteropServices;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     A structure specifying the parameters of
///     vertex buffers used in a graphics pipeline
/// </summary>
public readonly record struct GPUVertexBufferDescription(Type t, SDL.GPUVertexInputRate inputRate)
{
    /// <summary>
    ///     The type of data contained within this
    ///     buffer.
    /// </summary>
    public Type Type
    {
        get;
        init;
    } = t;

    /// <summary>
    ///     The binding slot of the vertex buffer.
    /// </summary>
    public uint Slot
    {
        get;
        init;
    }

    /// <summary>
    ///     The size of a single element + the offset between elements.
    /// </summary>
    public uint Pitch
    {
        get;
        init;
    } = (uint)Marshal.SizeOf(t);

    /// <summary>
    ///     Whether attribute addressing is a function
    ///     of the vertex index or instance index.
    /// </summary>
    public SDL.GPUVertexInputRate InputRate
    {
        get;
        init;
    } = inputRate;

    public static GPUVertexBufferDescription From<T>(SDL.GPUVertexInputRate inputRate) => new GPUVertexBufferDescription(typeof(T), inputRate);
}
