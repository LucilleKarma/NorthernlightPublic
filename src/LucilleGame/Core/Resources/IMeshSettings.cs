using Northernlight.Core.Rendering;

namespace Northernlight.Core.Resources;

/// <summary>
///     Settings that dictate how a given mesh should be rendered.
/// </summary>
public interface IMeshSettings
{
    public delegate void PreparationActionDelegate(FrameContext frame, RenderPass pass, Camera camera);

    /// <summary>
    ///     The preparation to take for the pipeline that
    ///     will render this mesh.
    /// </summary>
    public PreparationActionDelegate PreparationAction
    {
        get;
        set;
    }

    /// <summary>
    ///     The vertex shader to use when
    ///     rendering this mesh.
    /// </summary>
    public Shader VertexShader
    {
        get;
        set;
    }

    /// <summary>
    ///     The fragment shader to use when
    ///     rendering this mesh.
    /// </summary>
    public Shader FragmentShader
    {
        get;
        set;
    }

    /// <summary>
    ///     The blend state to render this mesh
    ///     with.
    /// </summary>
    public BlendState BlendState
    {
        get;
        set;
    }
}
