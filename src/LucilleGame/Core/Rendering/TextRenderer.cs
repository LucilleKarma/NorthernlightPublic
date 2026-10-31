using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of a global
///     text renderer, which can accept any
///     kind of font/text pair for rendering purposes.
/// </summary>
public sealed class TextRenderer : IRenderSubsystem
{
    private bool disposed;

    private readonly Lock initializationLock = new Lock();

    private readonly Dictionary<Font, TextEngineQueue> queues = [];

    /// <summary>
    ///     This renderer's graphics device.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    /// <summary>
    ///     The initialization parameters for this
    ///     renderer.
    /// </summary>
    public SpriteQueueParameters InitializationParameters
    {
        get;
        set;
    }

    public TextRenderer(GraphicsDevice device, SpriteQueueParameters parameters)
    {
        Device = device;
        InitializationParameters = parameters;
    }

    /// <summary>
    ///     Returns the size of a given string for
    ///     a given font, in pixels.
    /// </summary>
    public Vector2 MeasureString(string text, Font font)
    {
        if (!queues.ContainsKey(font))
        {
            lock (initializationLock)
            {
                queues[font] = new TextEngineQueue(Device, font, InitializationParameters);
            }
        }

        return queues[font].MeasureString(text);
    }

    /// <summary>
    ///     Enqueues a given text/font pair to this renderer.
    /// </summary>
    public void Enqueue(string text, Font font, Vector3 position, Color color, Vector2 scale, Vector2 normalizedOrigin = default, Angle rotation = default)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (!queues.ContainsKey(font))
        {
            lock (initializationLock)
            {
                queues[font] = new TextEngineQueue(Device, font, InitializationParameters);
            }
        }

        Vector2 originOffset = -MeasureString(text, font) * normalizedOrigin * scale;
        position.X += originOffset.X;
        position.Y += originOffset.Y;
        queues[font].Enqueue(text, position, color, scale, rotation);
    }

    public void Prepare(FrameContext frame)
    {
        if (disposed)
            return;

        foreach (TextEngineQueue queue in queues.Values)
            queue.Prepare(frame);
    }

    public void Render(FrameContext frame, RenderPass pass, Camera camera)
    {
        if (disposed)
            return;

        foreach (TextEngineQueue queue in queues.Values)
            queue.Render(frame, pass, camera);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        foreach (TextEngineQueue queue in queues.Values)
            queue.Dispose();
        disposed = true;
    }
}
