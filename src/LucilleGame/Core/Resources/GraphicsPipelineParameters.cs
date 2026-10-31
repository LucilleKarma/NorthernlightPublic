using Northernlight.Core.Rendering;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents a set of graphics pipeline parameters.
/// </summary>
public record GraphicsPipelineParameters
{
    /// <summary>
    ///     The optional format to use for a depth buffer.
    /// </summary>
    public SDL.GPUTextureFormat DepthBufferFormat
    {
        get;
        init;
    }

    /// <summary>
    ///     The vertex attributes of the pipeline.
    /// </summary>
    public required VertexAttributes VertexAttributes
    {
        get;
        init;
    }

    /// <summary>
    ///     The type of primitive topology used
    ///     by the pipeline.
    /// </summary>
    public required GPUPrimitiveType Topology
    {
        get;
        init;
    }

    /// <summary>
    ///     The pipeline's depth stencil state.
    /// </summary>
    public required DepthStencilState DepthStencil
    {
        get;
        init;
    }
}
