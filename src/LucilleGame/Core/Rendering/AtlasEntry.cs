using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of a sprite atlas
///     entry, which acts as a subdivision of a larger
///     composite texture.
/// </summary>
public sealed class AtlasEntry
{
    /// <summary>
    ///     The owner of this atlas entry.
    /// </summary>
    public required Atlas Owner
    {
        get;
        init;
    }

    /// <summary>
    ///     The backing texture of this atlas.
    /// </summary>
    public required Texture BackingTexture
    {
        get;
        init;
    }

    /// <summary>
    ///     The area of this atlas entry on the
    ///     backing texture.
    /// </summary>
    public required Rectangle Area
    {
        get;
        init;
    }

    /// <summary>
    ///     The name of this atlas entry.
    /// </summary>
    public required string Name
    {
        get;
        init;
    }

    /// <summary>
    ///     The bounds of this atlas entry, represented as
    ///     a 4D vector.
    /// </summary>
    public Vector4 VectorBounds
    {
        get
        {
            Vector2 topLeft = new Vector2(Area.Left, Area.Top) / BackingTexture.Size;
            Vector2 bottomRight = new Vector2(Area.Right, Area.Bottom) / BackingTexture.Size;

            return new Vector4(topLeft, bottomRight.X, bottomRight.Y);
        }
    }
}
