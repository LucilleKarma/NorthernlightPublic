using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Core;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents a kind of animation that
///     can be performed by a hero.
/// </summary>
public sealed class HeroAnimation(AtlasEntry texture, int totalHorizontalFrames, NorthernlightGame game, float animationRateSeconds = 8f / 60f,
    Action? onCompletion = null, Action<int>? onFrameChange = null, bool loops = true, float horizontalOffset = 0f, float verticalOffset = 0f, bool animates = true)
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Uniforms
    {
        public Vector4 TextureSize;

        public Vector4 OutlineColor;

        public Vector4 OutlineDirection;

        public Vector4 BiasToFullColor;

        public Vector4 BiasingColor;
    }

    private int frame;

    private float currentFlashGlow;

    private readonly bool animates = animates;

    private readonly bool loops = loops;

    private readonly float horizontalOffset = horizontalOffset;

    private readonly float verticalOffset = verticalOffset;

    private readonly NorthernlightGame game = game;

    private readonly int totalHorizontalFrames = totalHorizontalFrames;

    private readonly float animationRateSeconds = animationRateSeconds;

    private readonly Action? onCompletion = onCompletion;

    private readonly Action<int>? onFrameChange = onFrameChange;

    private readonly OwnedTimer timer = game.Time.Rent();

    /// <summary>
    ///     The current frame.
    /// </summary>
    public int Frame
    {
        get => frame;
        set => frame = value;
    }

    /// <summary>
    ///     The amount of horizontal frames
    ///     present on this animation sheet.
    /// </summary>
    public int TotalHorizontalFrames => totalHorizontalFrames;

    /// <summary>
    ///     How long this animation lasts for before
    ///     either looping or ceasing at the final
    ///     frame.
    /// </summary>
    public float Duration => totalHorizontalFrames * animationRateSeconds;

    /// <summary>
    ///     This animation's sprite sheet texture.
    /// </summary>
    public readonly AtlasEntry Texture = texture;

    /// <summary>
    ///     Updates this animation.
    /// </summary>
    public void Update()
    {
        int previousFrame = frame;
        if (animates && timer.Cycle(animationRateSeconds) && frame < totalHorizontalFrames)
            frame++;

        if (frame >= totalHorizontalFrames)
        {
            onCompletion?.Invoke();
            if (loops)
                frame = 0;
            else
                frame = totalHorizontalFrames - 1;
        }

        if (frame != previousFrame)
            onFrameChange?.Invoke(frame);
    }

    /// <summary>
    ///     Resets this animation's current frame.
    /// </summary>
    public void ResetFrame()
    {
        timer.Reset();
        frame = 0;
    }

    /// <summary>
    ///     Makes a clone of this animation state.
    /// </summary>
    public HeroAnimation Clone()
    {
        return new HeroAnimation(Texture, TotalHorizontalFrames, game, animationRateSeconds, onCompletion, onFrameChange, loops, horizontalOffset, verticalOffset, false)
        {
            frame = Frame
        };
    }

    internal void PrepareRenderParameters(FrameContext frame, RenderPass pass)
    {
        float opacity = 1f;
        if (game.CurrentScene is KnightFightScene scene)
            opacity = 1f - scene.BattleBackBackdropOpacity;

        Vector2 outlineDirection = new Vector2(-1f, 1f);
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            OutlineColor = new Vector4(242f, 115f, 236f, 255f) * opacity / 255f,
            OutlineDirection = new Vector4(outlineDirection.SafeNormalize(), 0f, 0f),
            TextureSize = new Vector4(Atlases.KnightScene.Atlas.Value.BackingTexture.Size * 0.5f, 0f, 0f),
            BiasToFullColor = Vector4.UnitX * currentFlashGlow,
            BiasingColor = new Vector4(1f, 1f, 1f, 1f)
        });
    }

    /// <summary>
    ///     Renders this animation.
    /// </summary>
    public void Render(SpriteRenderer sprites, Vector3 drawPosition, Color tint, float currentFlashGlow = 0f, float dz = 0f, SpriteRenderer.ShaderWithPreparations? overridingShader = null)
    {
        this.currentFlashGlow = currentFlashGlow;
        Vector2 scale = Vector2.One * 1.8f;
        sprites.Enqueue(Texture, new SpriteData(Texture, frame, 0, totalHorizontalFrames, 1, 1) with
        {
            Position = drawPosition - Vector3.UnitY * Texture.Area.Size.Y * scale.X * 0.5f + new Vector3(horizontalOffset * scale.X, verticalOffset * scale.Y, dz),
            Scale = scale,
            Tint = tint,
        }, overridingShader: overridingShader ?? new SpriteRenderer.ShaderWithPreparations(Shaders.DirectionalOutlineShader, PrepareRenderParameters), queuePriorityInfluence: -10);
    }
}
