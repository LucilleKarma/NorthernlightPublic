using System.Numerics;
using System.Runtime.InteropServices;

namespace Northernlight.Core.Rendering;

// Make sure to update the vertex attributes of the
// sprite queue pipeline if you change the data layout
// of this type.

/// <summary>
///     Represents an instance of quadrilateral
///     data that is used by the sprite queue.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpriteQuadVertex
{
    public Vector2 Corner;

    public SpriteQuadVertex(float x, float y)
    {
        Corner = new Vector2(x, y);
    }
}
