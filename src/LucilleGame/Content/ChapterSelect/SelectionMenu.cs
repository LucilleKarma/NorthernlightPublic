using System.Diagnostics;
using System.Numerics;
using Northernlight.Content.Logo;
using Northernlight.Content.SaveData;
using Northernlight.Content.SaveData.SurveyReader;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.ChapterSelect;

public sealed class SelectionMenu : DrawableObject
{
    public enum Selection
    {
        ChapterOne,
        ChapterTwo,
        ChapterThree,
        ChapterFour,
        ChapterFive,
        ChapterSix,
        Quit,

        Count
    }

    public enum PlaySelection
    {
        Play,
        DoNot,

        Count
    }

    private bool displayingPlayOptions;

    private Selection selection;

    private PlaySelection playSelection;

    private Action? onFullDisappear;

    private float fadeInCountdown = 0.5f;

    internal float disappearanceTimer;

    private readonly Scene scene;

    internal static float DisappearanceDuration => 1.5f;

    public float Scale
    {
        get;
        set;
    } = 1f;

    public float FadeInProgress
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The amount of unlocked chapters to select.
    /// </summary>
    public const int TotalUnlockedChapters = 6;

    /// <summary>
    ///     The amount of chapters that will exist total.
    /// </summary>
    public const int TotalChapters = 7;

    public Font Font
    {
        get;
        private set;
    } = new Font("8bitoperator.ttf", 16f);

    public Font FontJapanese
    {
        get;
        private set;
    } = Fonts.GetFont(Fonts.Type.fnt_ja_main);

    public Vector3 DrawOffset => Vector3.UnitY * float.Lerp(-20f, 0f, float.Cbrt(FadeInProgress));

    public SelectionMenu(Scene scene) => this.scene = scene;

    protected override void UpdateSelf()
    {
        base.UpdateSelf();

        if (fadeInCountdown > 0f)
        {
            fadeInCountdown -= scene.Time.DeltaTime;
            FadeInProgress = 0f;
        }
        else
            FadeInProgress = float.Clamp(FadeInProgress + scene.Time.DeltaTime * 1.75f, 0f, 1f);

        if (disappearanceTimer > 0f)
        {
            disappearanceTimer += scene.Time.DeltaTime;
            if (disappearanceTimer >= DisappearanceDuration)
                onFullDisappear?.Invoke();

            return;
        }

        if (displayingPlayOptions)
        {
            ProcessPlayOptions();
            return;
        }

        playSelection = PlaySelection.Play;

        Selection previousSelection = selection;
        if (ActiveKeymap.Up.WasJustPressed)
            selection--;
        if (ActiveKeymap.Down.WasJustPressed)
            selection++;

        int selectionCount = (int)Selection.Count;
        if (selection < 0)
            selection = (Selection)(selectionCount - 1);
        if ((int)selection >= selectionCount)
            selection = 0;

        if (previousSelection != selection)
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));

        if (ActiveKeymap.Confirm.WasJustPressed)
            ProcessConfirmation();
    }

    private void ProcessPlayOptions()
    {
        int offset = 0;
        if (ActiveKeymap.Left.WasJustPressed)
            offset = -1;
        if (ActiveKeymap.Right.WasJustPressed)
            offset = 1;

        if (offset != 0)
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));

        playSelection += offset;
        if (playSelection < 0)
            playSelection = PlaySelection.Count - 1;
        if (playSelection == PlaySelection.Count)
            playSelection = PlaySelection.Play;

        if (ActiveKeymap.Cancel.WasJustPressed)
        {
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
            displayingPlayOptions = false;
        }
        else if (ActiveKeymap.Confirm.WasJustPressed)
        {
            if (playSelection == PlaySelection.DoNot)
            {
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                displayingPlayOptions = false;
            }
            else
            {
                switch (selection)
                {
                    case Selection.ChapterOne:
                        PrepareToEnterVanillaChapter(1);
                        break;

                    case Selection.ChapterTwo:
                        PrepareToEnterVanillaChapter(2);
                        break;

                    case Selection.ChapterThree:
                        PrepareToEnterVanillaChapter(3);
                        break;

                    case Selection.ChapterFour:
                        PrepareToEnterVanillaChapter(4);
                        break;

                    case Selection.ChapterFive:
                        PrepareToEnterVanillaChapter(5);
                        break;

                    case Selection.ChapterSix:
                        PrepareToEnterOurChapter();
                        break;
                }
            }
        }
    }

    private void ProcessConfirmation()
    {
        scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
        switch (selection)
        {
            case Selection.Quit:
                Environment.Exit(0);
                break;

            default:
                displayingPlayOptions = true;
                break;
        }
    }

    private void PrepareToEnterVanillaChapter(int chapter)
    {
        bool success = GameDataLocations.TryFind(out string? deltaruneDirectory);
        if (!success || deltaruneDirectory is null)
        {
            scene.Game.PresentScene(new ChapterNotFoundScene(scene.Game, chapter));
            return;
        }

        disappearanceTimer = 0.0001f;
        switch (chapter)
        {
            case 1:
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("ch1_select.wav"), volume: 0.74f));
                break;

            case 2:
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("ch2_select.wav")));
                break;

            case 3:
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("ch3_select.wav"), volume: 0.7f));
                break;

            case 4:
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("ch4_select.wav")));
                break;

            case 5:
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("ch5_select.wav")));
                break;
        }

        onFullDisappear = () =>
        {
            scene.Music.Stop();

            var chapterSuffix = "_windows";
            var gameWad = "data.win";
            if (OperatingSystem.IsMacOS())
            {
                chapterSuffix = "_mac";
                gameWad = "game.ios";
            }

            string chapterDirectory = Path.Combine(deltaruneDirectory, $"chapter{chapter}{chapterSuffix}");

            // TODO: idk where the runner is on mac lol
            string deltaruneExe = Path.Combine(deltaruneDirectory, "DELTARUNE.exe");

            var process = new ProcessStartInfo
            {
                FileName = deltaruneExe,
                WorkingDirectory = chapterDirectory,
                Arguments = $"-game {gameWad}", // launcher switch_-1 returning_0
                UseShellExecute = false,
            };

            Process.Start(process);
            Thread.Sleep(2000);
            Environment.Exit(0);
        };
    }

    private void PrepareToEnterOurChapter()
    {
        disappearanceTimer = 0.0001f;
        scene.Audio.Play(new Sound(AudioBuffer.FromWav("ch6_select.wav")));
        onFullDisappear = () =>
        {
            LogoScene logo = new LogoScene(scene.Game);
            logo.Initialize();
            scene.Game.PresentScene(logo);

            scene.Game.Window.Title = "DELTARUNE Chapter 6";
        };
    }

    internal static Vector3 SquishDrawPosition(Vector3 p, float disappearanceProgress)
    {
        p.X = float.Lerp(p.X, 155f, float.Pow(disappearanceProgress, 0.65f) * 0.75f);
        p.Y *= 1f - float.Pow(disappearanceProgress, 1.5f) * 0.4f;
        return p;
    }

    internal static Vector2 SquishDrawScale(Vector2 scale, float disappearanceProgress)
    {
        scale.X *= 1f - float.Pow(disappearanceProgress, 1.25f) * 0.8f;
        return scale;
    }

    protected override void PrepareRenderSelf(RenderLayer layer)
    {
        base.PrepareRenderSelf(layer);

        SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();
        TextRenderer text = layer.OutputRenderer.Get<TextRenderer>();

        float disappearanceProgress = float.InverseLerp(0f, DisappearanceDuration, disappearanceTimer);
        float opacity = FadeInProgress * (1f - disappearanceProgress).Squared();
        Vector3 drawOffset = DrawOffset;

        bool chapterThreeCompleted = DeltaruneGlobalData.CompletedChapterAnySlot(SaveChapter.Three);
        float bottom = 211.5f;
        float top = 31.5f;
        float barSpacing = float.Abs(bottom - top) / TotalChapters;
        AtlasEntry horizontalBar = Atlases.ChapterSelect.horzBar;
        AtlasEntry icons = Atlases.ChapterSelect.chapterIcon;
        AtlasEntry soul = Atlases.ChapterSelect.heart;
        AtlasEntry stars = Atlases.ChapterSelect.ui_star;
        for (int i = 0; i < TotalChapters; i++)
        {
            Selection chapterSelection = i switch
            {
                0 => Selection.ChapterOne,
                1 => Selection.ChapterTwo,
                2 => Selection.ChapterThree,
                3 => Selection.ChapterFour,
                4 => Selection.ChapterFive,
                5 => Selection.ChapterSix,
                _ => Selection.ChapterOne,
            };
            SaveChapter chapter = i switch
            {
                0 => SaveChapter.One,
                1 => SaveChapter.Two,
                2 => SaveChapter.Three,
                3 => SaveChapter.Four,
                4 => SaveChapter.Five,
                5 => SaveChapter.Six,
                6 => SaveChapter.Seven,
                _ => SaveChapter.One,
            };

            bool locked = i >= TotalUnlockedChapters;
            bool selected = selection == chapterSelection && !locked;
            float progress = i / (float)(TotalChapters - 1f);
            float barY = float.Lerp(top, bottom, progress);
            string chapterText = LocalizationEntries.Chapter.WithArgs((i + 1).ToString()).Value;

            Color color = locked ? new Color(127, 127, 127) : Color.White;
            if (selected)
                color = new Color(255, 255, 0);
            color *= opacity;

            string chapterName = (i + 1) switch
            {
                1 => LocalizationEntries.Chaper1Name.Value,
                2 => LocalizationEntries.Chaper2Name.Value,
                3 => LocalizationEntries.Chaper3Name.Value,
                4 => LocalizationEntries.Chaper4Name.Value,
                5 => LocalizationEntries.Chaper5Name.Value,
                6 => LocalizationEntries.Chaper6Name.Value,
                _ => LocalizationEntries.ChaperLockedName.Value,
            };

            Vector3 chapterTextDrawPosition = new Vector3(54.5f, barY - barSpacing * 0.6f, 0f);
            text.Enqueue(chapterText, Font, SquishDrawPosition(chapterTextDrawPosition + drawOffset, disappearanceProgress), color, SquishDrawScale(Vector2.One * Scale, disappearanceProgress), Vector2.One * 0.5f);

            if (selected && !displayingPlayOptions)
            {
                Vector3 soulDrawPosition = new Vector3(14.5f, barY - barSpacing * 0.6f + 0.5f, 0f);
                sprites.Enqueue(soul, new SpriteData(soul)
                {
                    Position = SquishDrawPosition(soulDrawPosition + drawOffset, disappearanceProgress),
                    Tint = Color.White * opacity,
                    Scale = SquishDrawScale(Vector2.One * Scale, disappearanceProgress)
                });
            }

            Vector3 chapterNameDrawPosition = new Vector3(180f, barY - barSpacing * 0.6f, 0f);
            if (displayingPlayOptions && selected)
            {
                Color playColor = Color.White;
                Color doNotColor = Color.White;
                Vector3 soulDrawPosition = chapterNameDrawPosition;
                switch (playSelection)
                {
                    case PlaySelection.Play:
                        soulDrawPosition.X -= 65.5f;
                        soulDrawPosition.Y += 0.5f;
                        playColor = new Color(255, 255, 0);
                        break;

                    case PlaySelection.DoNot:
                        soulDrawPosition.X += 24.5f;
                        soulDrawPosition.Y += 0.5f;
                        doNotColor = new Color(255, 255, 0);
                        break;
                }

                string playText = LocalizationEntries.Play.Value;
                text.Enqueue(playText, Font, SquishDrawPosition(chapterNameDrawPosition + drawOffset - Vector3.UnitX * 41f, disappearanceProgress), playColor * opacity, SquishDrawScale(Vector2.One * Scale, disappearanceProgress), Vector2.One * 0.5f);

                string doNot = LocalizationEntries.DoNot.Value;
                text.Enqueue(doNot, Font, SquishDrawPosition(chapterNameDrawPosition + drawOffset + Vector3.UnitX * 54f, disappearanceProgress), doNotColor * opacity, SquishDrawScale(Vector2.One * Scale, disappearanceProgress), Vector2.One * 0.5f);

                sprites.Enqueue(soul, new SpriteData(soul)
                {
                    Position = SquishDrawPosition(soulDrawPosition + drawOffset, disappearanceProgress),
                    Tint = Color.White * opacity,
                    Scale = SquishDrawScale(Vector2.One * Scale, disappearanceProgress)
                });
            }
            else
                text.Enqueue(chapterName, Font, SquishDrawPosition(chapterNameDrawPosition + drawOffset, disappearanceProgress), color, SquishDrawScale(Vector2.One * Scale, disappearanceProgress), Vector2.One * 0.5f);

            sprites.Enqueue(horizontalBar, new SpriteData(horizontalBar)
            {
                Position = SquishDrawPosition(new Vector3(horizontalBar.Area.Width * 0.5f, barY, 0f) + drawOffset, disappearanceProgress),
                Tint = Color.White * opacity,
                Scale = SquishDrawScale(new Vector2(1.2f, 1f) * Scale, float.Pow(disappearanceProgress, 1.5f))
            });

            int iconFrameX = 0;

            // Tenna's smile transforms into the Knight's visor
            // if the player has completed chapter three.
            if (chapterSelection == Selection.ChapterThree && chapterThreeCompleted)
                iconFrameX = 1;

            int iconFrameY = i;
            Vector3 iconDrawPosition = new Vector3(289f, barY - barSpacing * 0.6f, 0f);
            sprites.Enqueue(icons, new SpriteData(icons, iconFrameX, iconFrameY, 2, TotalChapters)
            {
                Position = SquishDrawPosition(iconDrawPosition + drawOffset, disappearanceProgress),
                Tint = color,
                Scale = SquishDrawScale(Vector2.One * Scale, disappearanceProgress)
            });

            for (int j = 0; j < 3; j++)
            {
                SaveSlot ongoingSlot = j switch
                {
                    1 => SaveSlot.Two,
                    2 => SaveSlot.Three,
                    _ => SaveSlot.One,
                };
                bool deleted = !DeltaruneGlobalData.ChapterSaveFileExistsInSlot(chapter, ongoingSlot);
                bool completed = DeltaruneGlobalData.CompletedChapterInSlot(chapter, ongoingSlot);
                if (i == 5)
                {
                    deleted = false;
                    completed = NorthernlightSaveData.TryLoad(j, out NorthernlightSaveData? saveData) && saveData.TotalWins >= 1;
                }

                if (completed)
                {
                    Vector3 starDrawPosition = new Vector3(92.5f, barY - barSpacing * 0.6f + j * 6f - 6.25f, 0f);

                    int starFrameY = deleted ? 1 : 0;
                    sprites.Enqueue(stars, new SpriteData(stars, 0, starFrameY, 1, 2)
                    {
                        Position = SquishDrawPosition(starDrawPosition, disappearanceProgress),
                        Tint = Color.White * float.InverseLerp(0.5f, 1f, opacity).Squared(),
                        Scale = SquishDrawScale(Vector2.One * Scale * 0.5f, disappearanceProgress)
                    });
                }
            }
        }

        bool quitSelected = selection == Selection.Quit;
        string quitText = LocalizationEntries.Quit.Value;
        Vector3 quitTextDrawPosition = new Vector3(119f, 225f, 0f);
        text.Enqueue(quitText, Font, SquishDrawPosition(quitTextDrawPosition + drawOffset, disappearanceProgress), (quitSelected ? new Color(255, 255, 0) : Color.White) * opacity, SquishDrawScale(Vector2.One * Scale, disappearanceProgress), Vector2.One * 0.5f);
        if (quitSelected)
        {
            Vector3 soulDrawPosition = SquishDrawPosition(quitTextDrawPosition + new Vector3(-24.5f, 0.5f, 0f), disappearanceProgress);
            sprites.Enqueue(soul, new SpriteData(soul)
            {
                Position = SquishDrawPosition(soulDrawPosition + drawOffset, disappearanceProgress),
                Tint = Color.White * opacity,
                Scale = SquishDrawScale(Vector2.One * Scale, disappearanceProgress)
            });
        }

        RenderShadowCrystalIndicators(sprites, opacity);

        // TODO -- Maybe we could support this properly someday...
        string languageText = LocalizationEntries.LanguageJapanese.Value;
        Vector3 languageTextDrawPosition = new Vector3(194f, 224.5f, 0f);
        text.Enqueue(languageText, FontJapanese, SquishDrawPosition(languageTextDrawPosition + drawOffset, disappearanceProgress), new Color(127, 127, 127) * opacity, SquishDrawScale(Vector2.One * Scale, disappearanceProgress), Vector2.One * 0.5f);
    }

    private void RenderShadowCrystalIndicators(SpriteRenderer sprites, float opacity)
    {
        // If shadow crystal, then why black shard?
        AtlasEntry slotTexture = Atlases.ChapterSelect.ui_dot;

        int highestChapterWithCrystal = 0;
        float disappearanceProgress = float.InverseLerp(0f, DisappearanceDuration, disappearanceTimer);
        for (int x = 0; x < 5; x++)
        {
            SaveChapter chapter = (SaveChapter)(x + 1);
            if (DeltaruneGlobalData.FoughtSecretBossAnySlot(chapter))
                highestChapterWithCrystal = int.Max(highestChapterWithCrystal, x);
        }

        for (int x = 0; x <= highestChapterWithCrystal; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                SaveChapter chapter = (SaveChapter)(x + 1);
                SaveSlot slot = (SaveSlot)y;
                bool unlockedShadowCrystal = DeltaruneGlobalData.GetUraValue(chapter, slot) > 0f;
                int frame = unlockedShadowCrystal ? 1 : 0;
                Vector3 drawPosition = new Vector3(274.5f + x * 10f, 220.5f + y * 5f, 10f);
                if (unlockedShadowCrystal)
                    drawPosition.Y += 0.5f;

                sprites.Enqueue(slotTexture, new SpriteData(slotTexture, 0, frame, 1, 2)
                {
                    Position = SquishDrawPosition(drawPosition, disappearanceProgress),
                    Tint = Color.White * opacity,
                    Scale = SquishDrawScale(Vector2.One * 0.5f, disappearanceProgress)
                });
            }
        }
    }
}
