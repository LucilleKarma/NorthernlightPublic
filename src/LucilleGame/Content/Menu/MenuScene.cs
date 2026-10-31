using System.Numerics;
using Northernlight.Content.ChapterSelect;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.InputHandling;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Menu;

public sealed class MenuScene(NorthernlightGame game) : Scene(game)
{
    private readonly Screen screen = new();

    internal Menu menu;

    public override void Initialize()
    {
        base.Initialize();

        menu = new Menu(this);
        screen.Children.Add(menu);
    }

    public override void Show()
    {
        base.Show();

        Sound beautifulDogComposedMenuMusic = new Sound(AudioBuffer.FromWav("Menu.wav", musicDirectory: true));
        beautifulDogComposedMenuMusic.Stream.FrequencyRatio = 0.95f;

        Music.Add(beautifulDogComposedMenuMusic);

        UILayer.Preparations += RenderUi;
    }

    public override void Hide()
    {
        base.Hide();

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
