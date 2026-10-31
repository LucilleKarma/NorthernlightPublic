using static SDL3.SDL;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents a depth stencil state.
/// </summary>
public readonly record struct DepthStencilState
{
    /// <summary>
    ///     The comparison operator used for depth testing.
    /// </summary>
    public GPUCompareOp CompareOperator
    {
        get;
        init;
    }

    /// <summary>
    ///     The stencil operator state for back-facing triangles.
    /// </summary>
    public GPUStencilOpState BackStencilState
    {
        get;
        init;
    }

    /// <summary>
    ///     The stencil operator state for front-facing triangles.
    /// </summary>
    public GPUStencilOpState FrontStencilState
    {
        get;
        init;
    }

    /// <summary>
    ///     Selects the bits of the stencil values participating in the stencil test.
    /// </summary>
    public byte CompareMask
    {
        get;
        init;
    }

    /// <summary>
    ///     The byte mask for the stencil values when updated
    ///     by the stencil test.
    /// </summary>
    public byte WriteMask
    {
        get;
        init;
    }

    /// <summary>
    ///     Whether the depth test is enabled or not.
    /// </summary>
    public bool EnableDepthTest
    {
        get;
        init;
    }

    /// <summary>
    ///     Whether depth writes are enabled or not.
    /// </summary>
    /// <remarks>
    ///     This value is ignored if <see cref="EnableDepthTest"/> is <see langword="false"/>.
    /// </remarks>
    public bool EnableDepthWrite
    {
        get;
        init;
    }

    /// <summary>
    ///     Whether the stencil test is enabled or not.
    /// </summary>
    public bool EnableStencilTest
    {
        get;
        init;
    }

    /// <summary>
    ///     A depth stencil state which performs no tests.
    /// </summary>
    public static readonly DepthStencilState None = new DepthStencilState()
    {
        EnableDepthTest = false,
        EnableDepthWrite = false
    };

    /// <summary>
    ///     A depth stencil state which reads the
    ///     depth buffer, but does not write to it.
    /// </summary>
    public static readonly DepthStencilState DepthRead = new DepthStencilState()
    {
        EnableDepthTest = true,
        EnableDepthWrite = false,
        CompareOperator = GPUCompareOp.Less
    };

    /// <summary>
    ///     A depth stencil state which reads
    ///     and writes to the depth buffer.
    /// </summary>
    public static readonly DepthStencilState Default = new DepthStencilState()
    {
        EnableDepthTest = true,
        EnableDepthWrite = true,
        CompareOperator = GPUCompareOp.Less
    };
}
