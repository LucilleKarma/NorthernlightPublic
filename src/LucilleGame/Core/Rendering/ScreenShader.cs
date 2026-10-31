using Northernlight.Core;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents a screen shader.
/// </summary>
public sealed class ScreenShader
{
    /// <summary>
    ///     The preparations underlying this shader.
    /// </summary>
    public SpriteRenderer.ShaderWithPreparations ShaderPreparations
    {
        get;
    }

    /// <summary>
    ///     The intensity of this screen shader.
    /// </summary>
    public float Intensity
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this screen shader is currently active
    ///     or not.
    /// </summary>
    public bool Active
    {
        get;
        internal set;
    }

    public ScreenShader(SpriteRenderer.ShaderWithPreparations preparations) => ShaderPreparations = preparations;

    /// <summary>
    ///     Updates this screen shader.
    /// </summary>
    public void Update(Game game)
    {
        float intensityMoveDirection = Active ? 1f : -1f;
        Intensity = float.Clamp(Intensity + intensityMoveDirection * game.Time.DeltaTime, 0f, 1f);

        Active = false;
    }
}
