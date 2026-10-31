using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.InputHandling;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.ChapterSelect;

public sealed class ChapterNotFoundScene(NorthernlightGame game, int chapter) : Scene(game)
{
    internal bool clearSelf;

    private readonly int chapter = chapter;

    private readonly Screen screen = new();

    public override void Initialize()
    {
        base.Initialize();

        UILayer.Preparations += PrepareRender;

        screen.Children.Add(new CrtText(this, chapter));
        Music.Add(new Sound(AudioBuffer.FromWav("Crt.wav", musicDirectory: true)));
    }

    private void PrepareRender(FrameContext frame, RenderLayer layer)
    {
        screen.PrepareRender(layer);
    }

    public override void Hide()
    {
        base.Hide();

        clearSelf = true;
        UILayer.Preparations -= PrepareRender;
        Music.Stop();

        CrtText.Target?.Dispose();
        CrtText.Target = null;
    }

    public override void Update()
    {
        base.Update();

        Time.GlobalSpeed = 1f;

        if (Matrix4x4.Invert(UICamera.LetterboxTranslation, out Matrix4x4 inverseTranslation))
        {
            Vector2 scale = UICamera.Window.Size / new Vector2(UICamera.EffectiveWidth, UICamera.EffectiveHeight);
            MouseScreen = Vector2.Transform(Input.RawMousePosition / scale, inverseTranslation);
            MouseWorld = MouseScreen + MainCamera.Position.AsVector2();
        }

        screen.Update();
    }

    public override void Render(FrameContext frame)
    {
        base.Render(frame);

        RenderLayer.Render(
            [new RenderLayer.RenderSet(UILayer, UICamera)],
            frame,
            Time,
            Color.Black
        );
    }
}
