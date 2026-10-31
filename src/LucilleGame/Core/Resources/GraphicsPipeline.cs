using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a graphics pipeline.
/// </summary>
public sealed class GraphicsPipeline : IDisposable, IPipeline
{
    private bool disposed;

    /// <summary>
    ///     The handle associated with this
    ///     pipeline.
    /// </summary>
    internal nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     This pipeline's vertex shader.
    /// </summary>
    public Shader VertexShader
    {
        get;
    }

    /// <summary>
    ///     This pipeline's fragment shader.
    /// </summary>
    public Shader FragmentShader
    {
        get;
    }

    /// <summary>
    ///     The graphics device that owns this pipeline.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    /// <summary>
    ///     The parameter set assigned to
    ///     by this pipeline.
    /// </summary>
    public GraphicsPipelineParameters Parameters
    {
        get;
    }

    public GraphicsPipeline(GraphicsDevice device, Shader vertex, Shader fragment, GraphicsPipelineParameters parameters)
    {
        Device = device;

        VertexShader = vertex;
        FragmentShader = fragment;
        Parameters = parameters;

        WeakReference<IPipeline> selfRef = new WeakReference<IPipeline>(this);
        vertex.AttachedPipelines.Add(selfRef);
        fragment.AttachedPipelines.Add(selfRef);
        Recreate();
    }

    /// <summary>
    ///     (Re)creates this pipeline.
    /// </summary>
    public void Recreate()
    {
        SDL.GPUGraphicsPipelineCreateInfo pipelineInfo = new SDL.GPUGraphicsPipelineCreateInfo()
        {
            VertexShader = VertexShader.Handle,
            FragmentShader = FragmentShader.Handle,
            PrimitiveType = (SDL.GPUPrimitiveType)Parameters.Topology,
            VertexInputState =
            {
                NumVertexBuffers = (uint)Parameters.VertexAttributes.VertexBufferDescriptions.Length,
                VertexBufferDescriptions = Parameters.VertexAttributes.VertexBufferDescriptionsPointer,
                NumVertexAttributes = Parameters.VertexAttributes.Count,
                VertexAttributes = Parameters.VertexAttributes.Pointer
            },
            TargetInfo =
            {
                NumColorTargets = (uint)Parameters.VertexAttributes.ColorTargetDescriptions.Length,
                ColorTargetDescriptions = Parameters.VertexAttributes.ColorTargetDescriptionsPointer,
                HasDepthStencilTarget = Parameters.DepthStencil.EnableDepthTest,
                DepthStencilFormat = Parameters.DepthBufferFormat
            },
            DepthStencilState = new SDL.GPUDepthStencilState()
            {
                CompareOp = Parameters.DepthStencil.CompareOperator,
                BackStencilState = Parameters.DepthStencil.BackStencilState,
                FrontStencilState = Parameters.DepthStencil.FrontStencilState,
                CompareMask = Parameters.DepthStencil.CompareMask,
                WriteMask = Parameters.DepthStencil.WriteMask,
                EnableDepthTest = Parameters.DepthStencil.EnableDepthTest,
                EnableDepthWrite = Parameters.DepthStencil.EnableDepthWrite,
                EnableStencilTest = Parameters.DepthStencil.EnableStencilTest
            }
        };

        if (Handle != 0)
            SDL.ReleaseGPUGraphicsPipeline(Device.Handle, Handle);

        Handle = SDL.CreateGPUGraphicsPipeline(Device.Handle, pipelineInfo);
    }

    /// <summary>
    ///     Binds this pipeline to a given render pass.
    /// </summary>
    public void BindToPass(RenderPass pass) => SDL.BindGPUGraphicsPipeline(pass.Handle, Handle);

    public void Dispose()
    {
        if (disposed || Handle == nint.Zero)
            return;

        disposed = true;
        SDL.ReleaseGPUGraphicsPipeline(Device.Handle, Handle);
    }
}
