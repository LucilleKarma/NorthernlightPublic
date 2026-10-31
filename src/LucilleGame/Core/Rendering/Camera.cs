using System.Numerics;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a generic instance of an implementable camera.
/// </summary>
public abstract class Camera
{
    private int width;

    private int height;

    private Vector3 forward;

    private Vector3 up;

    private Vector3 position;

    private float near;

    private float far;

    private float zoom = 1f;

    /// <summary>
    ///     The zoom for this camera.
    /// </summary>
    public float Zoom
    {
        get => zoom;
        set
        {
            if (zoom != value)
            {
                zoom = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The width of the camera.
    /// </summary>
    public int Width
    {
        get => width;
        set
        {
            if (width != value)
            {
                width = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The height of the camera.
    /// </summary>
    public int Height
    {
        get => height;
        set
        {
            if (height != value)
            {
                height = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The forward direction for this camera.
    /// </summary>
    public Vector3 Forward
    {
        get => forward;
        set
        {
            if (forward != value)
            {
                forward = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The up direction for this camera.
    /// </summary>
    public Vector3 Up
    {
        get => up;
        set
        {
            if (up != value)
            {
                up = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The position of this camera in 3D space.
    /// </summary>
    public Vector3 Position
    {
        get => position;
        set
        {
            if (position != value)
            {
                position = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The distance to the near viewing plane.
    /// </summary>
    public float Near
    {
        get => near;
        set
        {
            if (near != value)
            {
                near = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The distance to the far viewing plane.
    /// </summary>
    public float Far
    {
        get => far;
        set
        {
            if (far != value)
            {
                far = value;
                RecalculateViewProjection();
            }
        }
    }

    /// <summary>
    ///     The active veiw matrix for this camera.
    /// </summary>
    public Matrix4x4 View
    {
        get;
        protected set;
    }

    /// <summary>
    ///     The active projection matrix for this camera.
    /// </summary>
    public Matrix4x4 Projection
    {
        get;
        protected set;
    }

    /// <summary>
    ///     Recalculates the view and projection matrices for this camera.
    /// </summary>
    protected void RecalculateViewProjection()
    {
        View = CalculateView();
        Projection = CalculateProjection();
    }

    public Camera(Vector2 size, Vector3 position, Vector3 forward, Vector3 up, float near, float far)
    {
        width = (int)size.X;
        height = (int)size.Y;

        this.forward = forward;
        this.up = up;
        this.position = position;
        this.near = near;
        this.far = far;

        RecalculateViewProjection();
    }

    /// <summary>
    ///     Calculates the view matrix for this camera.
    /// </summary>
    protected abstract Matrix4x4 CalculateView();

    /// <summary>
    ///     Calculates the projection matrix for this camera.
    /// </summary>
    protected abstract Matrix4x4 CalculateProjection();
}
