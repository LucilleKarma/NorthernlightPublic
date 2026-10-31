namespace Northernlight.Core.Rendering;

/// <summary>
///     Specifies the primitive topology of a graphics pipeline.
///     <list type="bullet">
///         <item>For HLSL compiling to SPIRV you must decorate a float output
///         with [[vk::builtin("PointSize")]].</item>
///         <item>For GLSL you must set the gl_PointSize
///         builtin. For MSL you must include a float output with the [[point_size]]
///         decorator.</item>
///     </list>
/// </summary>
public enum GPUPrimitiveType
{
    /// <summary>
    ///     A series of separate triangles.
    /// </summary>
    TriangleList,

    /// <summary>
    ///     A series of connected triangles.
    /// </summary>
    TriangleStrip,

    /// <summary>
    ///     A series of separate lines.
    /// </summary>
    LineList,

    /// <summary>
    ///     A series of connected lines.
    /// </summary>
    LineStrip,
}
