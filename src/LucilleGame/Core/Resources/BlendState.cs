using static SDL3.SDL;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents a blend state that can be
///     used for handling of texture blending.
/// </summary>
public readonly record struct BlendState
{
    public required bool Enabled
    {
        get;
        init;
    }

    public GPUBlendFactor SourceColorBlendFactor
    {
        get;
        init;
    }

    public GPUBlendFactor DestinationColorBlendFactor
    {
        get;
        init;
    }

    public GPUBlendOp ColorBlendOperation
    {
        get;
        init;
    }

    public GPUBlendOp AlphaBlendOperation
    {
        get;
        init;
    }

    public GPUBlendFactor SourceAlphaBlendFactor
    {
        get;
        init;
    }

    public GPUBlendFactor DestinationAlphaBlendFactor
    {
        get;
        init;
    }

    public GPUColorComponentFlags ColorWriteMask
    {
        get;
        init;
    }

    /// <summary>
    ///     A disabled blend state.
    /// </summary>
    public static readonly BlendState Disabled = new BlendState()
    {
        Enabled = false
    };

    /// <summary>
    ///     The alpha blend state.
    /// </summary>
    public static readonly BlendState AlphaBlend = new BlendState()
    {
        Enabled = true,
        ColorBlendOperation = GPUBlendOp.Add,
        AlphaBlendOperation = GPUBlendOp.Add,
        SourceColorBlendFactor = GPUBlendFactor.One,
        DestinationColorBlendFactor = GPUBlendFactor.OneMinusSrcAlpha,
        SourceAlphaBlendFactor = GPUBlendFactor.One,
        DestinationAlphaBlendFactor = GPUBlendFactor.OneMinusSrcAlpha
    };

    /// <summary>
    ///     The additive blend state.
    /// </summary>
    public static readonly BlendState Additive = new BlendState()
    {
        Enabled = true,
        ColorBlendOperation = GPUBlendOp.Add,
        AlphaBlendOperation = GPUBlendOp.Add,
        SourceColorBlendFactor = GPUBlendFactor.SrcAlpha,
        DestinationColorBlendFactor = GPUBlendFactor.One,
        SourceAlphaBlendFactor = GPUBlendFactor.SrcAlpha,
        DestinationAlphaBlendFactor = GPUBlendFactor.One
    };

    /// <summary>
    ///     The subtractive blend state.
    /// </summary>
    public static readonly BlendState Subtractive = new BlendState()
    {
        Enabled = true,
        ColorBlendOperation = GPUBlendOp.ReverseSubtract,
        AlphaBlendOperation = GPUBlendOp.ReverseSubtract,
        SourceColorBlendFactor = GPUBlendFactor.SrcAlpha,
        DestinationColorBlendFactor = GPUBlendFactor.One,
        SourceAlphaBlendFactor = GPUBlendFactor.SrcAlpha,
        DestinationAlphaBlendFactor = GPUBlendFactor.One
    };

    /// <summary>
    ///     The non-premultiplied blend state.
    /// </summary>
    public static readonly BlendState NonPremultiplied = new BlendState()
    {
        Enabled = true,
        ColorBlendOperation = GPUBlendOp.Add,
        AlphaBlendOperation = GPUBlendOp.Add,
        SourceColorBlendFactor = GPUBlendFactor.SrcAlpha,
        DestinationColorBlendFactor = GPUBlendFactor.OneMinusSrcAlpha,
        SourceAlphaBlendFactor = GPUBlendFactor.SrcAlpha,
        DestinationAlphaBlendFactor = GPUBlendFactor.OneMinusSrcAlpha
    };

    /// <summary>
    ///     The merge blend state.
    /// </summary>
    public static readonly BlendState Merge = new BlendState()
    {
        Enabled = true,
        ColorBlendOperation = GPUBlendOp.Max,
        AlphaBlendOperation = GPUBlendOp.Max,
        SourceColorBlendFactor = GPUBlendFactor.One,
        DestinationColorBlendFactor = GPUBlendFactor.OneMinusSrcAlpha,
        SourceAlphaBlendFactor = GPUBlendFactor.One,
        DestinationAlphaBlendFactor = GPUBlendFactor.OneMinusSrcAlpha
    };
}
