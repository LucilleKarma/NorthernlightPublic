using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;

namespace Northernlight.Content.Borders;

/// <summary>
///     Represents a kind of border that can
///     be displayed beyond the bounds of the
///     letterbox for aesthetic flair.
/// </summary>
public abstract class Border
{
    /// <summary>
    ///     The amount by which this border
    ///     should render, as a 0-1 interpolant.
    /// </summary>
    public float ActivityInterpolant
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this border is currently active or not.
    /// </summary>
    public bool Active => ActivityInterpolant > 0f;

    /// <summary>
    ///     Whether this border should be active or not.
    /// </summary>
    public bool ShouldBeActive
    {
        get;
        set;
    }

    /// <summary>
    ///     Updates this border.
    /// </summary>
    public virtual void Update(NorthernlightGame game)
    {
        float activityUpdateSpeed = 1f;
        float updateDirection = ShouldBeActive ? 1f : -1f;
        ActivityInterpolant = float.Clamp(ActivityInterpolant + game.Time.DeltaTime * updateDirection * activityUpdateSpeed, 0f, 1f);
    }

    /// <summary>
    ///     Applies optional load actions for when this border
    ///     is loaded by the system.
    /// </summary>
    public virtual void Load() { }

    /// <summary>
    ///     Dynamically determines whether this border
    ///     should be active based on the current
    ///     gameplay context.
    /// </summary>
    public abstract bool DynamicallyDetermineActivity(NorthernlightGame game);

    /// <summary>
    ///     Prepares this border for rendering.
    /// </summary>
    public abstract void PrepareRender(Scene scene, FrameContext frame, Renderer renderer, Rectangle letterboxArea, Vector2 viewportSize);
}
