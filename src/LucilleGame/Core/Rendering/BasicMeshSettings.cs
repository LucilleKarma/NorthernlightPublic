using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Settings that dictate how a given mesh should be rendered.
/// </summary>
public sealed class BasicMeshSettings : IMeshSettings
{
    public required IMeshSettings.PreparationActionDelegate PreparationAction
    {
        get;
        set;
    }

    public required Shader VertexShader
    {
        get;
        set;
    }

    public required Shader FragmentShader
    {
        get;
        set;
    }

    public BlendState BlendState
    {
        get;
        set;
    } = BlendState.AlphaBlend;
}
