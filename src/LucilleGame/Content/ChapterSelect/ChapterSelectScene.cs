using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.InputHandling;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.ChapterSelect;

public sealed class ChapterSelectScene(NorthernlightGame game) : Scene(game)
{
    private readonly Screen screen = new();

    public override bool PlayerCanUseQuitButton => false;

    public override void Initialize()
    {
        base.Initialize();

        SelectionMenu menu = new SelectionMenu(this);
        screen.Children.Add(new UiVersion(menu)
        {
            CopyrightEnabled = true
        });
        screen.Children.Add(menu);
    }

    public override void Show()
    {
        base.Show();
        Music.Add(new Sound(AudioBuffer.FromWav("AUDIO_DRONE.wav", musicDirectory: true)));

        UILayer.Preparations += RenderUi;
    }

    public override void Hide()
    {
        base.Hide();
        Music.Stop();

        UILayer.Preparations -= RenderUi;
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

    private void RenderUi(FrameContext frame, RenderLayer layer)
    {
        screen.PrepareRender(layer);
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
