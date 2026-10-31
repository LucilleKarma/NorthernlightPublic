using System.Numerics;
using Northernlight.Core.Rendering;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a process window.
/// </summary>
public sealed class Window : IDisposable
{
    private bool disposed;

    private string title;

    /// <summary>
    ///     The handle associated with this
    ///     window.
    /// </summary>
    internal nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The width of this window, in pixels.
    /// </summary>
    public int Width
    {
        get;
        private set;
    }

    /// <summary>
    ///     The height of this window, in pixels.
    /// </summary>
    public int Height
    {
        get;
        private set;
    }

    /// <summary>
    ///     The title of this window.
    /// </summary>
    public string Title
    {
        get => title;
        set
        {
            if (disposed)
                return;

            if (title != value)
            {
                title = value;
                SDL.SetWindowTitle(Handle, title);
            }
        }
    }

    /// <summary>
    ///     Whether this window is minimized or not.
    /// </summary>
    public bool IsMinimized => SDL.GetWindowFlags(Handle).HasFlag(SDL.WindowFlags.Minimized);

    /// <summary>
    ///     Whether this window is currently focused or not.
    /// </summary>
    public bool IsFocused
    {
        get;
        private set;
    }

    /// <summary>
    ///     Whether to pause updates (skip frames) when the window is unfocused.
    /// </summary>
    public bool PauseUpdatesOnLossOfFocus
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     Whether to pause playing music when the window is unfocused.
    /// </summary>
    public bool PauseMusicOnLossOfFocus
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     The size of this window, in pixels.
    /// </summary>
    public Vector2 Size => new Vector2(Width, Height);

    /// <summary>
    ///     An event that's fired whenever this window is resized.
    /// </summary>
    public event Action<int, int> OnResize;

    public Window(int width, int height, string title, SDL.WindowFlags flags, uint? props)
    {
        this.title = title;
        Width = width;
        Height = height;

        if (props.HasValue)
        {
            var properties = props.Value;
            {
                SDL.SetNumberProperty(properties, SDL.Props.WindowCreateWidthNumber, width);
                SDL.SetNumberProperty(properties, SDL.Props.WindowCreateHeightNumber, height);
                SDL.SetStringProperty(properties, SDL.Props.WindowCreateTitleString, title);
            }

            Handle = SDL.CreateWindowWithProperties(properties);
        }
        else
        {
            Handle = SDL.CreateWindow(title, Width, Height, flags);
        }

        SDLEventProcessor.OnEvent += (e, type) =>
        {
            if (type == SDL.EventType.WindowResized)
            {
                Width = e.Window.Data1;
                Height = e.Window.Data2;
                OnResize?.Invoke(Width, Height);
            }
            if (type == SDL.EventType.WindowFocusGained)
                IsFocused = true;
            if (type == SDL.EventType.WindowFocusLost)
                IsFocused = false;
        };
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        SDL.DestroyWindow(Handle);
    }
}
