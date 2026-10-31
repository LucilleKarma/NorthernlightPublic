using Northernlight.Core.Rendering;
using SDL3;

namespace Northernlight.Core;

/// <summary>
///     Represents a set of parameters that decide the
///     startup configuration of a game.
/// </summary>
public record GameParameters(int Width, int Height, string Title)
{
    /// <summary>
    ///     The flags that indicate how the
    ///     window should be created.
    /// </summary>
    public SDL.WindowFlags WindowFlags
    {
        get;
        init;
    }

    /// <summary>
    ///     The SDL window creation properties, created with
    ///     <see cref="SDL.CreateProperties"/>.
    /// </summary>
    public uint? WindowProperties
    {
        get;
        init;
    }

    /// <summary>
    ///     This game's starting camera instance.
    /// </summary>
    public Camera? MainCamera
    {
        get;
        init;
    }
}
