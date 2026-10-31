using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a camera that orthographically restricts a dynamic
///     viewport size down to a designated size.
/// </summary>
public sealed class RestrictedOrthographicCamera : Camera
{
    /// <summary>
    ///     The window to
    /// </summary>
    public Window Window
    {
        get;
    }

    /// <summary>
    ///     The effective view width of this camera, taking
    ///     into account letterboxing.
    /// </summary>
    public int EffectiveWidth
    {
        get;
        private set;
    }

    /// <summary>
    ///     The effective view width of this camera, taking
    ///     into account letterboxing.
    /// </summary>
    public int EffectiveHeight
    {
        get;
        private set;
    }

    /// <summary>
    ///     The cull rectangle of this camera.
    /// </summary>
    public Rectangle CullRectangle
    {
        get;
        set;
    }

    /// <summary>
    ///     The letterbox translation matrix.
    /// </summary>
    public Matrix4x4 LetterboxTranslation
    {
        get;
        private set;
    }

    public RestrictedOrthographicCamera(Window window, Vector2 size, Vector3 position, Vector3 forward, Vector3 up, float near, float far) : base(size, position, forward, up, near, far)
    {
        Window = window;
        Window.OnResize += (w, h) => RecalculateViewProjection();
        RecalculateViewProjection();
    }

    protected override Matrix4x4 CalculateView()
    {
        Matrix4x4 zoom = Matrix4x4.CreateScale(Zoom, Zoom, 1f);
        Vector3 position = Position;

        return Matrix4x4.CreateLookAt(position + Forward, position, Up) * zoom;
    }

    protected override Matrix4x4 CalculateProjection()
    {
        float designWidth = Width;
        float designHeight = Height;
        float windowWidth = Window?.Width ?? designWidth;
        float windowHeight = Window?.Height ?? designHeight;

        float scale = float.Min(windowWidth / designWidth, windowHeight / designHeight);
        EffectiveWidth = (int)float.Round(designWidth * scale);
        EffectiveHeight = (int)float.Round(designHeight * scale);

        CullRectangle = new Rectangle(0, 0, EffectiveWidth, EffectiveHeight);

        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale, scale, 1f);
        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(0f, windowWidth, windowHeight, 0f, Near, Far);

        return scaleMatrix * projection;
    }
}
