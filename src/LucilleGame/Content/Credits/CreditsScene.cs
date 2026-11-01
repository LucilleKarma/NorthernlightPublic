using System.Numerics;
using Northernlight.Content.ChapterSelect;
using Northernlight.Content.Knight;
using Northernlight.Core;
using Northernlight.Core.InputHandling;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Credits;

public sealed class CreditsScene(NorthernlightGame game, int saveFileNumber) : Scene(game)
{
    private CreditsPlayer player;

    private readonly Screen screen = new();

    /// <summary>
    ///     The save file slot number of this credits sequence.
    /// </summary>
    public int ActiveSaveFileNumber
    {
        get;
        set;
    } = saveFileNumber;

    /// <summary>
    ///     Whether the game should be closed when the credits are finished.
    /// </summary>
    public bool CloseGameAtEnd
    {
        get;
        set;
    }

    public override void Initialize()
    {
        base.Initialize();

        player = new CreditsPlayer(
                this,
                [
                    new CreditsPlayer.Display(
                        [
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Start1),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Start2),
                            CreditsPlayer.Line.Newline,
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Start3)
                        ]
                    ),

                    new CreditsPlayer.Display(
                        [
                            new CreditsPlayer.Line(LocalizationEntries.Credits_MakingOriginalGame, Title: true),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_DeltaruneTeam)
                        ]
                    ),

                    new CreditsPlayer.Display(
                        [
                            new CreditsPlayer.Line(LocalizationEntries.Credits_BackgroundArtTitle, Title: true),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Punkdog),
                            CreditsPlayer.Line.Newline,

                            new CreditsPlayer.Line(LocalizationEntries.Credits_BattleArtTitle, Title: true),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_FunkIt),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Throudon),
                            CreditsPlayer.Line.Newline,

                            new CreditsPlayer.Line(LocalizationEntries.Credits_BaseGameIntegrationProgrammingTitle, Title: true),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Tomat)
                        ]
                    ),

                    new CreditsPlayer.Display(
                        [
                            new CreditsPlayer.Line(LocalizationEntries.Credits_QATitle, Title: true),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Blast),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Flye),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Lapisqui),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Endlessvoid),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Oli),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Sprun),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_YuH),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Vani),
                        ]
                    ),

                    new CreditsPlayer.Display(
                        [
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Zoey),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Mattik),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Pengolin),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Smh),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Snake),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Spooktacular),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Walpar),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_Ivory),
                            new CreditsPlayer.Line(LocalizationEntries.Credits_LGL),
                        ]
                    ),
                ]
            );
        screen.Children.Add(player);
    }

    public override void Show()
    {
        base.Show();

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
        if (player.Done)
        {
            if (CloseGameAtEnd)
            {
                if (Music.Tracks.Length <= 0 || Music.Tracks[0].PositionBytes >= Music.Tracks[0].LoopEndBytes)
                    Environment.Exit(0);
            }
            else
            {
                screen.Children.Clear();

                KnightFightScene fight = new KnightFightScene(Game, ActiveSaveFileNumber)
                {
                    BlackCountdown = 0.33f
                };
                fight.Initialize();
                fight.DeathScreen.PlayerWon = true;
                fight.DeathScreen.StartAnimation();

                Game.PresentScene(fight);
                Game.BlackCountdown = 0.33f;
            }
            return;
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
