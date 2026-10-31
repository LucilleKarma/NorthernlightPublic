using System.Numerics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     The rendering mechanism responsible for the
///     backdrop visuals during the Knight battle, including
///     the aurora, the fountain, etc.
/// </summary>
public sealed class KnightBattleBackdrop
{
    /// <summary>
    ///     The scene responsible for this
    ///     backdrop.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
        private set;
    }

    /// <summary>
    ///     The resolution of the game responsible
    ///     for this backdrop.
    /// </summary>
    public required Vector2 GameResolution
    {
        get;
        init;
    }

    /// <summary>
    ///     The visuals timer of this backdrop.
    /// </summary>
    public float VisualsTimer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The speed factor for the <see cref="VisualsTimer"/>.
    /// </summary>
    public float AnimationSpeed
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The vertical offset to apply to the foreground.
    /// </summary>
    public Vector2 ForegroundVerticalOffset => GameResolution * Vector2.UnitY * -0.023f + Vector2.UnitY * 3f;

    public KnightBattleBackdrop(KnightFightScene scene) => Scene = scene;

    /// <summary>
    ///     Updates this backdrop.
    /// </summary>
    public void Update() => VisualsTimer += Scene.Time.DeltaTime * AnimationSpeed;

    /// <summary>
    ///     Prepares this backdrop for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        PrepareSky(sprites);
        RenderAurora(sprites);
        RenderDarkFountain(sprites);
        PrepareForeground(sprites);
    }

    private void PrepareSky(SpriteRenderer sprites)
    {
        AtlasEntry sky = Atlases.LargeTextures.Sky;
        SpriteData skyData = new SpriteData(sky)
        {
            Position = new Vector3(GameResolution * 0.5f, -60f),
            Scale = Vector2.One * 2f,
            Tint = Color.White
        };
        sprites.Enqueue(sky, skyData);
    }

    private void RenderDarkFountain(SpriteRenderer sprites)
    {
        int totalFountainFrames = 6;
        float animationSpeedConstant = 0.5f; // Keep this as either a whole number or a reciprocal of one of them.
        float blackKnifeBpm = 147.5f;
        float blackKnifeBps = blackKnifeBpm / 60f;
        float fountainAnimationSpeed = blackKnifeBps * totalFountainFrames * animationSpeedConstant;

        int fountainFrameY = (int)(VisualsTimer * fountainAnimationSpeed % totalFountainFrames);
        AtlasEntry fountain = Atlases.LargeTextures.DarkFountain;
        SpriteData baseFountainData = new SpriteData(fountain, fountainFrameY / 3, fountainFrameY % 3, 2, 3)
        {
            Position = new Vector3(GameResolution * new Vector2(0.5f, 0.5f), 0f),
            Scale = Vector2.One,
            Tint = Color.White
        };

        int fountainPriority = VisualsTimer >= 3f ? 50 : 0;
        float fountainOpacity = 1f - Scene.BattleBackBackdropOpacity;
        Vector2 componentWiseScale = GameResolution / baseFountainData.Frame.Size;
        baseFountainData.Scale = Vector2.One * float.Min(componentWiseScale.X, componentWiseScale.Y) * 1.1f;
        sprites.Enqueue(fountain, baseFountainData with
        {
            Tint = new Color(1f, 1f, 1f) * fountainOpacity * 0.99f
        }, BlendState.NonPremultiplied, drawToDepthBuffer: false, queuePriorityInfluence: fountainPriority);
    }

    private void RenderAurora(SpriteRenderer sprites)
    {
        AtlasEntry texture = Atlases.LargeTextures.Aurora;

        int totalFrames = 5;

        for (int i = 0; i < totalFrames; i++)
        {
            int frame = i;
            float opacity = float.Lerp(0.75f, 1f, float.Cos01(VisualsTimer * 3f - i));
            Vector2 scale = GameResolution * new Vector2(1.1f, 2.1f) / texture.Area.Size;
            Vector2 drawPosition = GameResolution * new Vector2(0.5f, 0f) + Vector2.UnitY * scale * texture.Area.Height / totalFrames * 0.5f;
            drawPosition.X += float.Cos(VisualsTimer * 1.5f + i) * 20f;

            SpriteData auroraData = new SpriteData(texture, 0, frame, 1, totalFrames)
            {
                Position = new Vector3(drawPosition, -50f - i),
                Scale = scale,
                Tint = Color.White * opacity * 0.5f
            };

            sprites.Enqueue(texture, auroraData, BlendState.Additive, drawToDepthBuffer: false);
        }
    }

    private void PrepareForeground(SpriteRenderer sprites)
    {
        AtlasEntry foreground = Atlases.LargeTextures.Foreground;
        Vector2 scale = Vector2.One * 1.1f;
        Vector3 drawPosition = new Vector3(GameResolution * new Vector2(0f, 0.72f) + ForegroundVerticalOffset + Vector2.UnitX * scale * foreground.Area.Width * 0.5f, 8f);

        sprites.Enqueue(foreground, new SpriteData(foreground) with
        {
            Position = drawPosition,
            Scale = scale,
            Tint = Color.White,
        });
    }
}
