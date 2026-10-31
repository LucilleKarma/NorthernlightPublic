using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents sets of parameters that
///     govern the behavior of a <see cref="SpriteQueue"/>.
/// </summary>
public readonly record struct SpriteQueueParameters
{
    /// <summary>
    ///     The fragment shader to use with the queue's pipeline.
    /// </summary>
    /// <remarks>
    ///     A default fragment shader is used if this is <see langword="null"/>.
    /// </remarks>
    public Shader? OverridingFragmentShader
    {
        get;
        init;
    } = null;

    /// <summary>
    ///     The initial capacity of the queue.
    /// </summary>
    public uint InitialCapacity
    {
        get;
        init;
    } = 256;

    /// <summary>
    ///     The samplier that should be used when
    ///     rendering the texture.
    /// </summary>
    public required Sampler Sampler
    {
        get;
        init;
    }

    /// <summary>
    ///     The blend state that should be used
    ///     when rendering textures.
    /// </summary>
    public required BlendState BlendState
    {
        get;
        init;
    }

    /// <summary>
    ///     The depth stencil state that should be
    ///     used when rendering textures.
    /// </summary>
    public required DepthStencilState DepthStencilState
    {
        get;
        init;
    }

    /// <summary>
    ///     The color target description.
    /// </summary>
    public required SDL.GPUColorTargetDescription ColorTargetDescription
    {
        get;
        init;
    }

    public SpriteQueueParameters()
    {

    }
}
