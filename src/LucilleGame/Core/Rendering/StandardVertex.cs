using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Northernlight.Core.Rendering;

[StructLayout(LayoutKind.Sequential)]
[DebuggerDisplay("{Position}")]
public struct StandardVertex
{
    /// <summary>
    ///     The position of this vertex in
    ///     3D space.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    ///     The color of this vertex.
    /// </summary>
    public Vector4 Color;

    /// <summary>
    ///     The normal of this vertex.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    ///     The UVW texture coordinates of this vertex.
    /// </summary>
    public Vector3 Uvw;

    public StandardVertex(Vector3 position, Vector4 color, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Color = color;
        Normal = normal;
        Uvw = new Vector3(uv, 0f);
    }
}
