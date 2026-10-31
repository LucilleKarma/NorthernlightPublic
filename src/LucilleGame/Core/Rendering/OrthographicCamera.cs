using System.Numerics;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an orthographic camera.
/// </summary>
public sealed class OrthographicCamera : Camera
{
    public OrthographicCamera(Vector2 size, Vector3 position, Vector3 forward, Vector3 up, float near, float far) : base(size, position, forward, up, near, far)
    { }

    protected override Matrix4x4 CalculateView()
    {
        Matrix4x4 zoom = Matrix4x4.CreateScale(Zoom, Zoom, 1f);
        return Matrix4x4.CreateLookAt(Position + Forward, Position, Up) * zoom;
    }

    protected override Matrix4x4 CalculateProjection()
    {
        return Matrix4x4.CreateOrthographicOffCenter(0f, Width, Height, 0f, Near, Far);
    }
}
