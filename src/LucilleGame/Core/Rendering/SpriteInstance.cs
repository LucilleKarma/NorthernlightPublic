using System.Numerics;
using System.Runtime.InteropServices;

namespace Northernlight.Core.Rendering;

// Make sure to update the vertex attributes of the
// sprite queue pipeline if you change the data layout
// of this type.

/// <summary>
///     Represents an instance of packed sprite data
///     that is to be sent to the GPU for vertex
///     creation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpriteInstance
{
    public Vector4 PositionXYSizeXY;

    public Vector4 UVRectangle;

    public Vector4 Color;

    public Vector4 RotationOriginPositionZ;

    private Vector4 padding;
}
