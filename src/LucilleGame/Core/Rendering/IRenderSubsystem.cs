using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

public interface IRenderSubsystem : IDisposable
{
    /// <summary>
    ///     Prepares this system for rendering.
    ///     This should be used for data batching and
    ///     such, outside of the central render pass scope.
    /// </summary>
    void Prepare(FrameContext frame);

    /// <summary>
    ///     Renders this system.
    /// </summary>
    void Render(FrameContext frame, RenderPass pass, Camera camera);
}
