using SDL3;

namespace Northernlight.Core.Rendering;

/// <summary>
///     The designated processor of all SDL event polling.
/// </summary>
public static class SDLEventProcessor
{
    /// <summary>
    ///     The central processor of all SDL events.
    /// </summary>
    public static event Action<SDL.Event, SDL.EventType> OnEvent;

    /// <summary>
    ///     An event to take before processing anything.
    /// </summary>
    public static event Action PreProcess;

    internal static void Process()
    {
        PreProcess?.Invoke();
        while (SDL.PollEvent(out SDL.Event e))
            OnEvent?.Invoke(e, (SDL.EventType)e.Type);
    }
}
