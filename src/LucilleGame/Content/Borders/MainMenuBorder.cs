using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.Menu;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Borders;

/// <summary>
///     A simple, empty border that is exclusively
///     black. Used if the user chooses to have a simple
///     border.
/// </summary>
public sealed class MainMenuBorder : Border
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Uniforms
    {
        public Vector4 LetterboxDimensions;

        public Vector4 Displacement;

        public Vector4 ActivityInterpolant;
    }

    private Rectangle activeLetterboxArea;

    public override bool DynamicallyDetermineActivity(NorthernlightGame game) => game.CurrentScene is MenuScene;

    public override void PrepareRender(Scene scene, FrameContext frame, Renderer renderer, Rectangle letterboxArea, Vector2 viewportSize)
    {
        if (scene is not MenuScene m)
            return;

        activeLetterboxArea = letterboxArea;
        renderer.Get<SpriteRenderer>().Enqueue(Atlases.KnightScene.Pixel, new SpriteData(Atlases.KnightScene.Pixel)
        {
            Position = new Vector3(viewportSize * 0.5f, 0f),
            Tint = Color.Lerp(Color.Black, Color.White, m.menu.screenWhitening.Squared()),
            Scale = viewportSize
        }, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.SimpleBorderShader, PrepareShaderParameters));
    }

    internal void PrepareShaderParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            LetterboxDimensions = new Vector4(0f, 0f, activeLetterboxArea.Width * 0.5f, activeLetterboxArea.Height * 0.5f),
            Displacement = new Vector4(activeLetterboxArea.Width * 0.5f + activeLetterboxArea.X, activeLetterboxArea.Height * 0.5f + activeLetterboxArea.Y, 0f, 0f),
            ActivityInterpolant = Vector4.One * ActivityInterpolant
        });
    }
}
