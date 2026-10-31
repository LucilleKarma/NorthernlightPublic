using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.Borders;
using Northernlight.Content.ChapterSelect;
using Northernlight.Content.Knight;
using Northernlight.Content.Knight.Items;
using Northernlight.Content.SaveData;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Menu;

public sealed class Menu : DrawableObject
{
    public enum Selection
    {
        SaveFileA,
        SaveFileB,
        SaveFileC,
        Copy,
        Erase,
        ChapterSelect,
        Chapter5FileLoad,
        // SwitchLanguage,
        EndProgram,
        Config,

        SaveFileA_Chapter5Load,
        SaveFileB_Chapter5Load,
        SaveFileC_Chapter5Load,
        DontUseChapter5File,

        SaveFileA_Copy,
        SaveFileB_Copy,
        SaveFileC_Copy,
        CancelCopy,

        SaveFileA_Erase,
        SaveFileB_Erase,
        SaveFileC_Erase,
        CancelErase,

        MusicVolume,
        SoundVolume,
        WasdControls,
        BorderConfig
    }

    public enum Subselection
    {
        None,
        SaveFile_Select,
        SaveFile_Back,
    }

    public enum SpecialActionState
    {
        None,
        SelectingFile,
        SelectingFile_Chapter5Load
    }

    public enum Submenu
    {
        None,
        SelectingChapter5File,
        Copy,
        Erase,
        EnteringNameForNewFile,
        ConfirmingNameForNewFile,
        ChoosingSideForFile,
        Config
    }

    public enum BinaryPlayerChoiceChoice
    {
        Undecided,
        Left,
        Right,
    }

    private bool selectingMusicVolume;

    private bool selectingSoundVolume;

    private int activeSaveFileNumber;

    private int fileToCopy = -1;

    private int fileToErase = -1;

    private int creatorLineIndexX;

    private int creatorLineIndexY;

    private bool triedToCopyEmptySave;

    private bool triedToCopySaveToItself;

    private bool erasureDoubleConfirmation;

    private bool triedToEraseNothing;

    private float menuInteractionCountdown;

    private float screenDarkening = 1f;

    private float creatorNamePromptOpacity;

    private float creatorNameConfirmOpacity;

    private float sideSelectOpacity;

    private string creatorName;

    private ChapterFiveSaveData evaluatedSaveData;

    internal float screenWhitening;

    private readonly OwnedTimer timer;

    private readonly OwnedTimer nameCharacterTimer;

    private readonly Scene scene;

    /// <summary>
    ///     The player's current selection.
    /// </summary>
    public Selection CurrentSelection
    {
        get;
        set;
    }

    /// <summary>
    ///     The current special action state.
    /// </summary>
    public SpecialActionState CurrentActionState
    {
        get;
        set;
    }

    /// <summary>
    ///     The player's current subselection.
    /// </summary>
    public Subselection CurrentSubselection
    {
        get;
        set;
    }

    /// <summary>
    ///     The current submenu.
    /// </summary>
    public Submenu CurrentSubmenu
    {
        get;
        set;
    }

    /// <summary>
    ///     The name confirmation state of the player.
    /// </summary>
    public BinaryPlayerChoiceChoice NameConfirmationState
    {
        get;
        set;
    }

    /// <summary>
    ///     The side choice state of the player.
    /// </summary>
    public BinaryPlayerChoiceChoice SideChoiceState
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the player has unlocked the option to
    ///     start in Side B from the selection menu.
    /// </summary>
    public static bool UnlockedSideB
    {
        get
        {
            for (int i = 0; i < 3; i++)
            {
                if (NorthernlightSaveData.TryLoad(i, out NorthernlightSaveData? data) && data.TotalWins >= 1)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     The main English font.
    /// </summary>
    public Font Font
    {
        get;
        private set;
    } = new Font("8bitoperator.ttf", 16f);

    /// <summary>
    ///     The main Japanese font.
    /// </summary>
    public Font FontJapanese
    {
        get;
        private set;
    } = Fonts.GetFont(Fonts.Type.fnt_ja_main);

    /// <summary>
    ///     The current soul position.
    /// </summary>
    public Vector2 SoulPosition
    {
        get;
        set;
    }

    /// <summary>
    ///     The ideal soul position.
    /// </summary>
    public Vector2 IdealSoulPosition
    {
        get;
        set;
    }

    /// <summary>
    ///     The global text opacity.
    /// </summary>
    public float TextOpacity => (1f - screenDarkening) * (1f - screenWhitening).Squared();

    /// <summary>
    ///     The text color used to indicate that a feature
    ///     is inoperable.
    /// </summary>
    public static Color UnusedColor => new Color(132, 132, 132);

    /// <summary>
    ///     The maximum amount of characters that can be
    ///     entered when selecting a name.
    /// </summary>
    public static int NameCharacterLimit => 12;

    /// <summary>
    ///     The mappings of what selections are available at
    ///     what sides depending on input directions.
    /// </summary>
    public readonly Dictionary<Selection, SelectionAdjacencies> AdjacenciesMap = [];

    /// <summary>
    ///     The set of all names that cause the game
    ///     to crash if inputted.
    /// </summary>
    public static readonly string[] IllegalNames =
    [
        "GASTER"
    ];

    public readonly record struct SelectionAdjacencies(Selection? Left = null, Selection? Right = null, Selection? Up = null, Selection? Down = null);

    public Menu(Scene scene)
    {
        this.scene = scene;
        timer = scene.Time.Rent();
        nameCharacterTimer = scene.Time.Rent();

        AdjacenciesMap[Selection.SaveFileA] = new SelectionAdjacencies(Down: Selection.SaveFileB);
        AdjacenciesMap[Selection.SaveFileB] = new SelectionAdjacencies(Up: Selection.SaveFileA, Down: Selection.SaveFileC);
        AdjacenciesMap[Selection.SaveFileC] = new SelectionAdjacencies(Up: Selection.SaveFileB, Down: Selection.Copy);

        AdjacenciesMap[Selection.Copy] = new SelectionAdjacencies(Up: Selection.SaveFileC, Down: Selection.Chapter5FileLoad, Right: Selection.Erase);
        AdjacenciesMap[Selection.Erase] = new SelectionAdjacencies(Up: Selection.SaveFileC, Left: Selection.Copy, Right: Selection.ChapterSelect, Down: Selection.Config);
        AdjacenciesMap[Selection.ChapterSelect] = new SelectionAdjacencies(Up: Selection.SaveFileC, Left: Selection.Erase, Down: Selection.EndProgram);
        AdjacenciesMap[Selection.Chapter5FileLoad] = new SelectionAdjacencies(Up: Selection.Copy, Right: Selection.Config);
        AdjacenciesMap[Selection.Config] = new SelectionAdjacencies(Up: Selection.Copy, Left: Selection.Chapter5FileLoad, Right: Selection.EndProgram);
        AdjacenciesMap[Selection.EndProgram] = new SelectionAdjacencies(Up: Selection.ChapterSelect, Left: Selection.Config, Right: Selection.EndProgram);

        AdjacenciesMap[Selection.SaveFileA_Chapter5Load] = new SelectionAdjacencies(Down: Selection.SaveFileB_Chapter5Load);
        AdjacenciesMap[Selection.SaveFileB_Chapter5Load] = new SelectionAdjacencies(Up: Selection.SaveFileA_Chapter5Load, Down: Selection.SaveFileC_Chapter5Load);
        AdjacenciesMap[Selection.SaveFileC_Chapter5Load] = new SelectionAdjacencies(Up: Selection.SaveFileB_Chapter5Load, Down: Selection.DontUseChapter5File);

        AdjacenciesMap[Selection.DontUseChapter5File] = new SelectionAdjacencies(Up: Selection.SaveFileC_Chapter5Load);

        AdjacenciesMap[Selection.SaveFileA_Copy] = new SelectionAdjacencies(Down: Selection.SaveFileB_Copy);
        AdjacenciesMap[Selection.SaveFileB_Copy] = new SelectionAdjacencies(Up: Selection.SaveFileA_Copy, Down: Selection.SaveFileC_Copy);
        AdjacenciesMap[Selection.SaveFileC_Copy] = new SelectionAdjacencies(Up: Selection.SaveFileB_Copy, Down: Selection.CancelCopy);
        AdjacenciesMap[Selection.CancelCopy] = new SelectionAdjacencies(Up: Selection.SaveFileC_Copy);

        AdjacenciesMap[Selection.SaveFileA_Erase] = new SelectionAdjacencies(Down: Selection.SaveFileB_Erase);
        AdjacenciesMap[Selection.SaveFileB_Erase] = new SelectionAdjacencies(Up: Selection.SaveFileA_Erase, Down: Selection.SaveFileC_Erase);
        AdjacenciesMap[Selection.SaveFileC_Erase] = new SelectionAdjacencies(Up: Selection.SaveFileB_Erase, Down: Selection.CancelErase);
        AdjacenciesMap[Selection.CancelErase] = new SelectionAdjacencies(Up: Selection.SaveFileC_Erase);

        AdjacenciesMap[Selection.MusicVolume] = new SelectionAdjacencies(Down: Selection.SoundVolume);
        AdjacenciesMap[Selection.SoundVolume] = new SelectionAdjacencies(Down: Selection.WasdControls, Up: Selection.MusicVolume);
        AdjacenciesMap[Selection.WasdControls] = new SelectionAdjacencies(Down: Selection.BorderConfig, Up: Selection.SoundVolume);
        AdjacenciesMap[Selection.BorderConfig] = new SelectionAdjacencies(Up: Selection.WasdControls);
    }

    protected override void UpdateSelf()
    {
        base.UpdateSelf();

        if (CurrentSubmenu == Submenu.EnteringNameForNewFile)
            creatorNamePromptOpacity = float.Min(1f, creatorNamePromptOpacity + scene.Time.DeltaTime * 2f);
        else
            creatorNamePromptOpacity = float.Max(0f, creatorNamePromptOpacity - scene.Time.DeltaTime * 2f);

        if (CurrentSubmenu == Submenu.ConfirmingNameForNewFile)
            creatorNameConfirmOpacity = float.Min(1f, creatorNameConfirmOpacity + scene.Time.DeltaTime * 2f);
        else
            creatorNameConfirmOpacity = float.Max(0f, creatorNameConfirmOpacity - scene.Time.DeltaTime * 2f);

        if (CurrentSubmenu == Submenu.ChoosingSideForFile)
            sideSelectOpacity = float.Min(1f, sideSelectOpacity + scene.Time.DeltaTime * 2f);
        else
            sideSelectOpacity = float.Max(0f, sideSelectOpacity - scene.Time.DeltaTime * 2f);

        if (screenDarkening > 0f)
            screenDarkening = float.Max(0f, screenDarkening - scene.Time.DeltaTime * 0.9f);

        if (screenWhitening > 0f)
        {
            screenWhitening += scene.Time.DeltaTime * 0.333f;
            if (screenWhitening >= 1f)
            {
                scene.Music.Stop();
                screenWhitening = 0f;
                KnightFightScene fight = new KnightFightScene(scene.Game, activeSaveFileNumber);
                fight.Initialize();
                scene.Game.PresentScene(fight);
            }

            scene.Music.Player.Volume = scene.Game.RemapVolume((1f - screenWhitening).Cubed(), music: true);
            return;
        }

        if (CurrentActionState == SpecialActionState.None)
        {
            if (CurrentSubmenu != Submenu.EnteringNameForNewFile && CurrentSubmenu != Submenu.ChoosingSideForFile)
                ProcessSelectionMovement();
            ProcessSimpleConfirmations();
            if (CurrentSubmenu == Submenu.SelectingChapter5File && ActiveKeymap.Cancel.WasJustPressed)
            {
                CurrentSubmenu = Submenu.None;
                CurrentSelection = Selection.Chapter5FileLoad;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
            }
        }

        else if (CurrentActionState == SpecialActionState.SelectingFile)
        {
            Subselection previousSubselection = CurrentSubselection;
            if (ActiveKeymap.Left.WasJustPressed)
                CurrentSubselection = Subselection.SaveFile_Select;
            if (ActiveKeymap.Right.WasJustPressed)
                CurrentSubselection = Subselection.SaveFile_Back;

            if (CurrentSubselection != previousSubselection)
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));

            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                if (CurrentSubmenu == Submenu.Erase)
                {
                    if (CurrentSubselection == Subselection.SaveFile_Select)
                    {
                        if (erasureDoubleConfirmation)
                        {
                            erasureDoubleConfirmation = false;
                            CurrentSubmenu = Submenu.None;
                            CurrentSelection = Selection.Erase;
                            CurrentActionState = SpecialActionState.None;
                            CurrentSubselection = Subselection.None;
                            menuInteractionCountdown = 0.1f;

                            if (fileToErase != -1)
                            {
                                NorthernlightSaveData.TryDelete(fileToErase);
                                fileToErase = -1;
                            }

                            scene.Audio.Play(new Sound(AudioBuffer.FromWav("EraseFile.wav")));
                        }
                        else
                        {
                            erasureDoubleConfirmation = true;
                            scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                        }
                    }
                    if (CurrentSubselection == Subselection.SaveFile_Back)
                    {
                        erasureDoubleConfirmation = false;
                        CurrentSubmenu = Submenu.None;
                        CurrentSelection = Selection.Erase;
                        CurrentActionState = SpecialActionState.None;
                        CurrentSubselection = Subselection.None;
                        menuInteractionCountdown = 0.1f;
                        scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                    }
                }

                else
                {
                    if (CurrentSubselection == Subselection.SaveFile_Select)
                    {
                        if (NorthernlightSaveData.TryLoad(activeSaveFileNumber, out _))
                            screenWhitening = 0.0001f;
                        else
                        {
                            creatorName = string.Empty;
                            creatorLineIndexX = 0;
                            creatorLineIndexY = 0;

                            CurrentActionState = SpecialActionState.None;
                            CurrentSubselection = Subselection.None;
                            menuInteractionCountdown = 0.1f;
                            CurrentSubmenu = Submenu.EnteringNameForNewFile;
                        }
                    }

                    if (CurrentSubselection == Subselection.SaveFile_Back)
                    {
                        CurrentActionState = SpecialActionState.None;
                        CurrentSubselection = Subselection.None;
                        menuInteractionCountdown = 0.1f;
                    }
                }
            }
        }

        else if (CurrentActionState == SpecialActionState.SelectingFile_Chapter5Load)
        {
            Subselection previousSubselection = CurrentSubselection;
            if (ActiveKeymap.Left.WasJustPressed)
                CurrentSubselection = Subselection.SaveFile_Select;
            if (ActiveKeymap.Right.WasJustPressed)
                CurrentSubselection = Subselection.SaveFile_Back;

            if (CurrentSubselection != previousSubselection)
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));

            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                if (CurrentSubselection == Subselection.SaveFile_Select)
                {
                    CreateNewSaveFile();
                    screenWhitening = 0.0001f;
                }
                else if (CurrentSubselection == Subselection.SaveFile_Back)
                {
                    CurrentActionState = SpecialActionState.None;
                    CurrentSubselection = Subselection.None;
                    menuInteractionCountdown = 0.1f;
                }
            }
        }

        if (SoulPosition != IdealSoulPosition && IdealSoulPosition != Vector2.Zero)
        {
            Vector2 offset = IdealSoulPosition - SoulPosition;
            if (offset.Length() <= 1f)
                SoulPosition = IdealSoulPosition;
            else
            {
                float soulSpeed = 650f;
                if (CurrentSubmenu == Submenu.EnteringNameForNewFile)
                    soulSpeed = 200f;

                Vector2 direction = offset / offset.Length();
                SoulPosition += direction * scene.Time.DeltaTime * soulSpeed;

                // Snap back to the destination if the
                // soul overshot past it for some reason.
                if (Vector2.Dot(direction, IdealSoulPosition - SoulPosition) <= 0f)
                    SoulPosition = IdealSoulPosition;
            }
        }

        if (ActiveKeymap.Cancel.WasJustPressed && CurrentSubselection == Subselection.None && (CurrentSubmenu == Submenu.Erase || CurrentSubmenu == Submenu.Copy))
        {
            CurrentSelection = Selection.SaveFileA;
            CurrentSubmenu = Submenu.None;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
        }

        menuInteractionCountdown = float.Max(0f, menuInteractionCountdown - scene.Time.DeltaTime);
    }

    private void CreateNewSaveFile()
    {
        if (evaluatedSaveData is null)
            return;

        NorthernlightSaveData data = new NorthernlightSaveData(activeSaveFileNumber, evaluatedSaveData.CreatorName)
        {
            AbortedChapter5WeirdRoute = evaluatedSaveData.AbortedChapter5WeirdRoute
        };
        data.Inventory.AddRange(VanillaGameItemRegistry.FilterValidIds(CollectionsMarshal.AsSpan(evaluatedSaveData.Items)));

        // Give the player some Chapter 6 healing items for free!
        data.WithChapter6HeaingItems();

        data.TimePlayedSeconds = evaluatedSaveData.PlayTime / 30f;

        data.Save();
    }

    private static void CopySaveFile(int existingSlot, int newSlot)
    {
        NorthernlightSaveData data = NorthernlightSaveData.Load(existingSlot, string.Empty);
        data.CreateCopy(newSlot).Save();
    }

    private void ProcessSelectionMovement()
    {
        if (!AdjacenciesMap.TryGetValue(CurrentSelection, out SelectionAdjacencies adjacencies))
            return;

        if (selectingMusicVolume || selectingSoundVolume)
            return;

        Selection previousSelection = CurrentSelection;
        if (ActiveKeymap.Left.WasJustPressed && adjacencies.Left is { } left)
            CurrentSelection = left;
        if (ActiveKeymap.Right.WasJustPressed && adjacencies.Right is { } right)
            CurrentSelection = right;
        if (ActiveKeymap.Up.WasJustPressed && adjacencies.Up is { } up)
            CurrentSelection = up;
        if (ActiveKeymap.Down.WasJustPressed && adjacencies.Down is { } down)
            CurrentSelection = down;

        if (CurrentSelection != previousSelection)
        {
            CurrentSubselection = Subselection.None;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));
        }
    }

    private void ProcessSimpleConfirmations()
    {
        if (CurrentSelection == Selection.EndProgram && ActiveKeymap.Confirm.WasJustPressed)
            Environment.Exit(0);
    }

    protected override void PrepareRenderSelf(RenderLayer layer)
    {
        base.PrepareRenderSelf(layer);

        SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();
        TextRenderer text = layer.OutputRenderer.Get<TextRenderer>();

        RenderDoor(sprites);
        RenderChapterTextOnTopLeft(text);

        if (CurrentSubmenu == Submenu.ChoosingSideForFile)
            RenderSideSelection(text);
        else if (CurrentSubmenu == Submenu.EnteringNameForNewFile)
            RenderNewNameStuff(text);
        else if (CurrentSubmenu == Submenu.ConfirmingNameForNewFile)
            RenderNameConfirmationStuff(text);
        else if (CurrentSubmenu == Submenu.Config)
        {
            RenderVolumeConfig(text, music: true);
            RenderVolumeConfig(text, music: false);
            RenderWasdConfig(text);
            RenderBordersConfig(text);
        }
        else
        {
            RenderTopLeftPrompt(text);
            for (int i = 0; i < 3; i++)
            {
                if (CurrentSubmenu == Submenu.None || CurrentSubmenu == Submenu.Copy || CurrentSubmenu == Submenu.Erase)
                    RenderSaveFileSelection(sprites, text, i);
                else if (CurrentSubmenu == Submenu.SelectingChapter5File)
                    RenderChapter5SaveFileSelection(sprites, text, i);
            }

            if (CurrentSubmenu == Submenu.None)
            {
                RenderCopyText(text);
                RenderEraseText(text);
                RenderChapterSelectText(text);
                RenderChapter5FilesText(text);
                // RenderLanguageText(text);
                RenderConfigText(text);
                RenderEndProgramText(text);
            }
            else if (CurrentSubmenu == Submenu.SelectingChapter5File)
                RenderDontUseChapter5Text(text);
            else if (CurrentSubmenu == Submenu.Copy || CurrentSubmenu == Submenu.Erase)
                RenderCancelText(text);

            RenderCopyrightText(text);
        }

        RenderSoul(sprites);

        AtlasEntry pixel = Atlases.Menu.Pixel;
        if (screenDarkening > 0f)
        {
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = Vector3.UnitZ * 99f,
                Scale = Vector2.One * 99999f,
                Tint = Color.Black * screenDarkening
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }
        if (screenWhitening > 0f)
        {
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = Vector3.UnitZ * 99f,
                Scale = Vector2.One * 99999f,
                Tint = Color.White * screenWhitening
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }
    }

    private void RenderDoor(SpriteRenderer sprites)
    {
        float time = timer * 30f;
        Vector2 doorScale = Vector2.One * 2f;
        Texture fancyDoor = Atlases.Menu.GiantDarkDoor.BackingTexture;
        Vector3 drawOrigin = new Vector3(229f, 304f, 0f);

        // Translucent back doors.
        float pulse = float.Max(0f, 0.03f + float.Sin(time / 20f) * 0.04f);
        for (int i = 0; i < 4; i++)
        {
            float dx = i % 2 == 0 ? -2f : 2f;
            float dy = i / 2 == 0 ? -2f : 2f;

            sprites.Enqueue(fancyDoor, new SpriteData(fancyDoor)
            {
                Position = drawOrigin + new Vector3(dx, dy, i + 1f),
                Scale = Vector2.One * doorScale,
                Tint = new Color(1f, 1f, 1f) * (1f - screenWhitening) * pulse
            }, BlendState.Merge, drawToDepthBuffer: false);
        }

        // Main door.
        sprites.Enqueue(fancyDoor, new SpriteData(fancyDoor)
        {
            Position = drawOrigin,
            Scale = Vector2.One * doorScale,
            Tint = Color.White * (1f - screenWhitening) * 0.25f
        });
    }

    private void RenderChapterTextOnTopLeft(TextRenderer text)
    {
        string chapterText = LocalizationEntries.ChapterUppercase.WithArgs("6").Value;
        Vector3 textDrawPosition = new Vector3(8f, 4f, 0f);
        text.Enqueue(chapterText, Font, textDrawPosition, Color.White * TextOpacity, Vector2.One);
    }

    private void RenderTopLeftPrompt(TextRenderer text)
    {
        string textString = LocalizationEntries.FileSelect.Value;

        if (CurrentSubmenu == Submenu.SelectingChapter5File)
        {
            textString = LocalizationEntries.StartFromChapter5.Value;
            if (CurrentActionState == SpecialActionState.SelectingFile_Chapter5Load)
                textString = LocalizationEntries.ThisWillStartChapter6.WithArgs((activeSaveFileNumber + 1).ToString()).Value;
        }
        else if (CurrentSubmenu == Submenu.Copy)
        {
            textString = LocalizationEntries.ChooseFileToCopy.Value;
            if (triedToCopySaveToItself)
                textString = LocalizationEntries.YouCantCopyThere.Value;
            if (triedToCopyEmptySave)
                textString = LocalizationEntries.ItCantBeCopied.Value;
        }
        else if (CurrentSubmenu == Submenu.Erase)
        {
            textString = LocalizationEntries.ChooseFileToErase.Value;
            if (triedToEraseNothing)
                textString = LocalizationEntries.NothingToErase.Value;
        }

        Vector3 textDrawPosition = new Vector3(40f, 30f, 0f);
        text.Enqueue(textString, Font, textDrawPosition, Color.White * TextOpacity, Vector2.One);
    }

    private void RenderSaveFileSelection(SpriteRenderer sprites, TextRenderer text, int index)
    {
        Vector2 boxCenter = new Vector2(160f, 95f + index * 45f);
        Rectangle boxArea = new Rectangle(boxCenter, new Vector2(212f, 41.5f));
        Vector2 pixelBoxOffset = Vector2.UnitY * 0.5f;

        bool selected = false;
        if (index == 0 && (CurrentSelection == Selection.SaveFileA || CurrentSelection == Selection.SaveFileA_Copy || CurrentSelection == Selection.SaveFileA_Erase))
            selected = true;
        if (index == 1 && (CurrentSelection == Selection.SaveFileB || CurrentSelection == Selection.SaveFileB_Copy || CurrentSelection == Selection.SaveFileB_Erase))
            selected = true;
        if (index == 2 && (CurrentSelection == Selection.SaveFileC || CurrentSelection == Selection.SaveFileC_Copy || CurrentSelection == Selection.SaveFileC_Erase))
            selected = true;

        NorthernlightSaveData.TryLoad(index, out NorthernlightSaveData? data);

        bool soulUninitialized = SoulPosition == Vector2.Zero && IdealSoulPosition == Vector2.Zero;
        if (selected)
        {
            activeSaveFileNumber = index;
            IdealSoulPosition = PositionInBox(0.0725f, 0.765f);

            // For some reason Deltarune starts the soul off
            // slightly to the right of the chapter selection location.
            if (soulUninitialized)
                SoulPosition = IdealSoulPosition + Vector2.UnitX * 50f;

            if (menuInteractionCountdown <= 0f && screenWhitening <= 0f)
            {
                if (CurrentSubmenu == Submenu.Erase)
                {
                    if (data is null)
                    {
                        if (ActiveKeymap.Confirm.WasJustPressed)
                        {
                            triedToEraseNothing = true;
                            scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
                        }
                    }

                    else
                    {
                        if (CurrentActionState == SpecialActionState.None)
                        {
                            if (ActiveKeymap.Confirm.WasJustPressed)
                            {
                                fileToErase = index;
                                CurrentActionState = SpecialActionState.SelectingFile;
                                CurrentSubselection = Subselection.SaveFile_Select;
                                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                            }
                        }
                        else if (CurrentActionState == SpecialActionState.SelectingFile)
                        {
                            if (ActiveKeymap.Cancel.WasJustPressed)
                            {
                                erasureDoubleConfirmation = false;
                                CurrentActionState = SpecialActionState.None;
                                CurrentSubselection = Subselection.None;
                                scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
                            }
                        }
                    }
                }
                else if (CurrentSubmenu == Submenu.Copy)
                {
                    if (ActiveKeymap.Confirm.WasJustPressed)
                    {
                        bool alreadyCopyingAFile = fileToCopy != -1;
                        if (alreadyCopyingAFile)
                        {
                            ChapterFiveSaveData? saveData = null;
                            DeltaruneGlobalData.SaveProvider?.TryToLoadChapter5Data(index, out saveData);

                            triedToCopySaveToItself = fileToCopy == index;
                            triedToCopyEmptySave = saveData is null;
                            if (!triedToCopySaveToItself && !triedToCopyEmptySave)
                            {
                                CurrentSelection = Selection.SaveFileA;
                                CurrentSubmenu = Submenu.None;
                                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                                CopySaveFile(fileToCopy, index);
                            }

                            fileToCopy = -1;
                        }
                        else
                        {
                            fileToCopy = index;
                            CurrentSelection = Selection.SaveFileA_Copy;
                        }
                    }
                }
                else if (CurrentActionState == SpecialActionState.None)
                {
                    if (ActiveKeymap.Confirm.WasJustPressed)
                    {
                        CurrentActionState = SpecialActionState.SelectingFile;
                        CurrentSubselection = Subselection.SaveFile_Select;
                        scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                    }
                }
                else if (CurrentActionState == SpecialActionState.SelectingFile)
                {
                    if (ActiveKeymap.Cancel.WasJustPressed)
                    {
                        CurrentActionState = SpecialActionState.None;
                        CurrentSubselection = Subselection.None;
                        scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
                    }
                }
            }
        }

        AtlasEntry pixel = Atlases.Menu.Pixel;
        Color color = ChooseColor(selected);
        if (fileToCopy == index)
            color = new Color(255, 255, 102);
        Color borderColor = color;
        Color nameColor = color;

        string name = LocalizationEntries.FileEmpty.Value;
        if (data is not null)
            name = data.Name;
        string time = LocalizationEntries.FileTimeFiller.Value;
        if (data is not null)
        {
            int seconds = (int)data.TimePlayedSeconds % 60;
            int minutes = (int)data.TimePlayedSeconds / 60 % 60;
            int hours = (int)data.TimePlayedSeconds / 3600 % 60;
            time = $"{hours}:{minutes:00}:{seconds:00}";
        }
        string confirmation = LocalizationEntries.Start.Value;
        string rejection = LocalizationEntries.Back.Value;
        if (CurrentSubmenu == Submenu.Erase && CurrentActionState == SpecialActionState.SelectingFile && selected)
        {
            name = LocalizationEntries.EraseThisFile.Value;
            confirmation = LocalizationEntries.Yes.Value;
            rejection = LocalizationEntries.No.Value;
            if (erasureDoubleConfirmation)
            {
                name = LocalizationEntries.EraseThisFileReally.Value;
                confirmation = LocalizationEntries.YesExclamationPoint.Value;
                rejection = LocalizationEntries.NoExclamationPoint.Value;
                borderColor = new Color(255, 0, 0);
                nameColor = new Color(255, 0, 0);
            }

            time = string.Empty;
        }

        Vector2 PositionInBox(float xInterpolant, float yInterpolant)
        {
            float x = float.Lerp(boxArea.Left, boxArea.Right, xInterpolant);
            float y = float.Lerp(boxArea.Top, boxArea.Bottom, yInterpolant - 0.7f);
            return new Vector2(x, y);
        }

        // Left bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.TopLeft + pixelBoxOffset, 10f),
            Scale = new Vector2(2f, boxArea.Height),
            Tint = borderColor
        });

        // Right bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.TopRight + pixelBoxOffset, 10f),
            Scale = new Vector2(2f, boxArea.Height),
            Tint = borderColor
        });

        // Top bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.TopLeft + boxArea.Size * new Vector2(1f, -1f) * 0.5f + pixelBoxOffset, 10f),
            Scale = new Vector2(boxArea.Width + 2f, 2f),
            Tint = borderColor
        });

        // Bottom bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.BottomLeft + boxArea.Size * new Vector2(1f, -1f) * 0.5f + pixelBoxOffset, 10f),
            Scale = new Vector2(boxArea.Width + 2f, 2f),
            Tint = borderColor
        });

        Vector3 nameTextDrawPosition = new Vector3(PositionInBox(0.1225f, 0.36f), 10f);
        text.Enqueue(name, Font, nameTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(name, Font, nameTextDrawPosition, nameColor * TextOpacity, Vector2.One);

        Vector3 timeTextDrawPosition = new Vector3(PositionInBox(0.8533f, 0.36f), 10f);
        Vector2 normalizedOrigin = new Vector2(1f, 0f);
        text.Enqueue(time, Font, timeTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One, normalizedOrigin);
        text.Enqueue(time, Font, timeTextDrawPosition, color * TextOpacity, Vector2.One, normalizedOrigin);

        if (selected && CurrentActionState == SpecialActionState.SelectingFile)
        {
            Color startColor = ChooseColor(CurrentSubselection == Subselection.SaveFile_Select);
            Color backColor = ChooseColor(CurrentSubselection == Subselection.SaveFile_Back);

            Vector3 confirmationTextDrawPosition = new Vector3(PositionInBox(0.17f, 0.775f), 10f);
            text.Enqueue(confirmation, Font, confirmationTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
            text.Enqueue(confirmation, Font, confirmationTextDrawPosition, startColor * TextOpacity, Vector2.One);

            Vector3 rejectionTextDrawPosition = new Vector3(PositionInBox(0.595f, 0.775f), 10f);
            text.Enqueue(rejection, Font, rejectionTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
            text.Enqueue(rejection, Font, rejectionTextDrawPosition, backColor * TextOpacity, Vector2.One);

            if (CurrentSubselection == Subselection.SaveFile_Select)
                IdealSoulPosition = confirmationTextDrawPosition.AsVector2() + new Vector2(-10.5f, 8.5f);
            else
                IdealSoulPosition = rejectionTextDrawPosition.AsVector2() + new Vector2(-10.5f, 8.5f);
        }
        else
        {
            string location = LocalizationEntries.FileLocationFiller.Value;
            if (data is not null)
                location = LocalizationEntries.FileLocationChapter6.Value;

            Vector3 locationTextDrawPosition = new Vector3(PositionInBox(0.1225f, 0.775f), 10f);
            text.Enqueue(location, Font, locationTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
            text.Enqueue(location, Font, locationTextDrawPosition, color * TextOpacity, Vector2.One);
        }
    }

    private void RenderChapter5SaveFileSelection(SpriteRenderer sprites, TextRenderer text, int index)
    {
        Vector2 boxCenter = new Vector2(160f, 95f + index * 45f);
        Rectangle boxArea = new Rectangle(boxCenter, new Vector2(212f, 41.5f));
        Vector2 pixelBoxOffset = Vector2.UnitY * 0.5f;

        bool selected = false;
        if (index == 0 && CurrentSelection == Selection.SaveFileA_Chapter5Load)
            selected = true;
        if (index == 1 && CurrentSelection == Selection.SaveFileB_Chapter5Load)
            selected = true;
        if (index == 2 && CurrentSelection == Selection.SaveFileC_Chapter5Load)
            selected = true;

        ChapterFiveSaveData? saveData = null;
        DeltaruneGlobalData.SaveProvider?.TryToLoadChapter5Data(index, out saveData);

        if (selected)
        {
            activeSaveFileNumber = index;
            IdealSoulPosition = PositionInBox(0.0725f, 0.765f);

            if (menuInteractionCountdown <= 0f && screenWhitening <= 0f && saveData is not null)
            {
                if (CurrentActionState == SpecialActionState.None)
                {
                    if (ActiveKeymap.Confirm.WasJustPressed)
                    {
                        evaluatedSaveData = saveData;
                        CurrentActionState = SpecialActionState.SelectingFile_Chapter5Load;
                        CurrentSubselection = Subselection.SaveFile_Select;
                        scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                    }
                }
                else if (CurrentActionState == SpecialActionState.SelectingFile_Chapter5Load)
                {
                    if (ActiveKeymap.Cancel.WasJustPressed)
                    {
                        CurrentActionState = SpecialActionState.None;
                        CurrentSubselection = Subselection.None;
                        scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
                    }
                }
            }

            if (saveData is null && ActiveKeymap.Confirm.WasJustPressed)
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("Error.wav")));
        }

        AtlasEntry pixel = Atlases.Menu.Pixel;
        Color color = ChooseColor(selected);

        Vector2 PositionInBox(float xInterpolant, float yInterpolant)
        {
            float x = float.Lerp(boxArea.Left, boxArea.Right, xInterpolant);
            float y = float.Lerp(boxArea.Top, boxArea.Bottom, yInterpolant - 0.7f);
            return new Vector2(x, y);
        }

        // Left bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.TopLeft + pixelBoxOffset, 10f),
            Scale = new Vector2(2f, boxArea.Height),
            Tint = color
        });

        // Right bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.TopRight + pixelBoxOffset, 10f),
            Scale = new Vector2(2f, boxArea.Height),
            Tint = color
        });

        // Top bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.TopLeft + boxArea.Size * new Vector2(1f, -1f) * 0.5f + pixelBoxOffset, 10f),
            Scale = new Vector2(boxArea.Width + 2f, 2f),
            Tint = color
        });

        // Bottom bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(boxArea.BottomLeft + boxArea.Size * new Vector2(1f, -1f) * 0.5f + pixelBoxOffset, 10f),
            Scale = new Vector2(boxArea.Width + 2f, 2f),
            Tint = color
        });

        if (saveData is null)
        {
            string name = LocalizationEntries.CompletionFileNotFound.Value;
            Vector3 nameTextDrawPosition = new Vector3(PositionInBox(0.1225f, 0.36f), 10f);
            text.Enqueue(name, Font, nameTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
            text.Enqueue(name, Font, nameTextDrawPosition, color * TextOpacity, Vector2.One);

            string credits = LocalizationEntries.MadeOnSeeingCredits.Value;
            Vector3 creditsTextDrawPosition = new Vector3(PositionInBox(0.1225f, 0.775f), 10f);
            text.Enqueue(credits, Font, creditsTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
            text.Enqueue(credits, Font, creditsTextDrawPosition, color * TextOpacity, Vector2.One);
        }
        else
        {
            string name = saveData.CreatorName;
            Vector3 nameTextDrawPosition = new Vector3(PositionInBox(0.1225f, 0.36f), 10f);
            text.Enqueue(name, Font, nameTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
            text.Enqueue(name, Font, nameTextDrawPosition, color * TextOpacity, Vector2.One);

            if (selected && CurrentActionState == SpecialActionState.SelectingFile_Chapter5Load)
            {
                Color startColor = ChooseColor(CurrentSubselection == Subselection.SaveFile_Select);
                Color backColor = ChooseColor(CurrentSubselection == Subselection.SaveFile_Back);

                string start = LocalizationEntries.Start.Value;
                Vector3 startTextDrawPosition = new Vector3(PositionInBox(0.17f, 0.775f), 10f);
                text.Enqueue(start, Font, startTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
                text.Enqueue(start, Font, startTextDrawPosition, startColor * TextOpacity, Vector2.One);

                string back = LocalizationEntries.Back.Value;
                Vector3 backTextDrawPosition = new Vector3(PositionInBox(0.595f, 0.775f), 10f);
                text.Enqueue(back, Font, backTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
                text.Enqueue(back, Font, backTextDrawPosition, backColor * TextOpacity, Vector2.One);

                if (CurrentSubselection == Subselection.SaveFile_Select)
                    IdealSoulPosition = startTextDrawPosition.AsVector2() + new Vector2(-10.5f, 8.5f);
                else
                    IdealSoulPosition = backTextDrawPosition.AsVector2() + new Vector2(-10.5f, 8.5f);
            }
            else
            {
                string room = LocalizationEntries.Chapter5EndingRoom.Value;
                Vector3 roomTextDrawPosition = new Vector3(PositionInBox(0.1225f, 0.775f), 10f);
                text.Enqueue(room, Font, roomTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
                text.Enqueue(room, Font, roomTextDrawPosition, color * TextOpacity, Vector2.One);
            }

            float totalSeconds = saveData.PlayTime / 30f;
            int seconds = (int)totalSeconds % 60;
            int minutes = (int)totalSeconds / 60 % 60;
            int hours = (int)totalSeconds / 3600 % 60;
            string time = $"{hours}:{minutes:00}:{seconds:00}";

            Vector3 timeTextDrawPosition = new Vector3(PositionInBox(0.8533f, 0.36f), 10f);
            Vector2 normalizedOrigin = new Vector2(1f, 0f);
            text.Enqueue(time, Font, timeTextDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One, normalizedOrigin);
            text.Enqueue(time, Font, timeTextDrawPosition, color * TextOpacity, Vector2.One, normalizedOrigin);
        }
    }

    private void RenderCopyText(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.Copy;
        string copy = LocalizationEntries.Copy.Value;
        Vector3 textDrawPosition = new Vector3(54f, 190f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(copy, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(copy, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
        {
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                CurrentSelection = Selection.SaveFileA_Copy;
                CurrentSubmenu = Submenu.Copy;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
            }
        }
    }

    private void RenderEraseText(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.Erase;
        string erase = LocalizationEntries.Erase.Value;
        Vector3 textDrawPosition = new Vector3(140f, 190f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(erase, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(erase, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
        {
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
            if (ActiveKeymap.Confirm.WasJustPressed && menuInteractionCountdown <= 0f && screenWhitening <= 0f)
            {
                CurrentSelection = Selection.SaveFileA_Erase;
                CurrentSubmenu = Submenu.Erase;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
            }
        }
    }

    private void RenderChapterSelectText(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.ChapterSelect;
        string chapterSelect = LocalizationEntries.ChapterSelect.Value;
        Vector3 textDrawPosition = new Vector3(204f, 190f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(chapterSelect, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(chapterSelect, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
        {
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                scene.Music.Stop();
                ChapterSelectScene chapterSelectScene = new ChapterSelectScene(scene.Game);
                chapterSelectScene.Initialize();
                scene.Game.PresentScene(chapterSelectScene);
            }
        }
    }

    private void RenderDontUseChapter5Text(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.DontUseChapter5File;
        string chapterSelect = LocalizationEntries.DontUseChapter5File.Value;
        Vector3 textDrawPosition = new Vector3(55f, 190f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(chapterSelect, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(chapterSelect, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
        {
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                CurrentSelection = Selection.Chapter5FileLoad;
                CurrentSubmenu = Submenu.None;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
            }
        }
    }

    private void RenderCancelText(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.CancelCopy || CurrentSelection == Selection.CancelErase;
        string chapterSelect = LocalizationEntries.Cancel.Value;
        Vector3 textDrawPosition = new Vector3(55f, 190f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(chapterSelect, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(chapterSelect, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
        {
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                fileToCopy = -1;
                triedToCopyEmptySave = false;
                triedToCopySaveToItself = false;
                CurrentSelection = Selection.SaveFileA;
                CurrentSubmenu = Submenu.None;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
            }
        }
    }

    private void RenderChapter5FilesText(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.Chapter5FileLoad;
        string files = LocalizationEntries.Chapter5Files.Value;
        Vector3 textDrawPosition = new Vector3(54f, 210f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(files, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(files, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
        {
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                CurrentSubmenu = Submenu.SelectingChapter5File;
                CurrentSelection = Selection.SaveFileA_Chapter5Load;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
            }
        }
    }

    private void RenderLanguageText(TextRenderer text)
    {
        string language = LocalizationEntries.LanguageJapanese.Value;
        Vector3 textDrawPosition = new Vector3(136f, 210f, 10f);
        Color color = UnusedColor;
        text.Enqueue(language, FontJapanese, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(language, FontJapanese, textDrawPosition, color * TextOpacity, Vector2.One);
    }

    private void RenderConfigText(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.Config;

        string config = LocalizationEntries.Config.Value;
        Vector3 textDrawPosition = new Vector3(136f, 210f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(config, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(config, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
        {
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                CurrentSelection = Selection.MusicVolume;
                CurrentSubmenu = Submenu.Config;
                selectingMusicVolume = false;
                selectingSoundVolume = false;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
            }
        }
    }

    private void RenderEndProgramText(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.EndProgram;

        string end = LocalizationEntries.EndProgram.Value;
        Vector3 textDrawPosition = new Vector3(204f, 210f, 10f);
        Color color = ChooseColor(selected);
        text.Enqueue(end, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(end, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);
    }

    private void RenderCopyrightText(TextRenderer text)
    {
        int currentYear = DateTime.Now.Year;
        string copyright = LocalizationEntries.DeltaruneCopyrightTextFull.WithArgs(currentYear.ToString()).Value;
        float scale = 0.5f;
        Vector3 textDrawPosition = new Vector3(184f, 228f, 10f);
        Color color = Color.White * 0.4f;
        text.Enqueue(copyright, Font, textDrawPosition, color * TextOpacity, Vector2.One * scale);
    }

    private void RenderNewNameStuff(TextRenderer text)
    {
        string[] lineA = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"];
        string[] lineB = ["K", "L", "M", "N", "O", "P", "Q", "R", "S", "T"];
        string[] lineC = ["U", "V", "W", "X", "Y", "Z", LocalizationEntries.BackUppercase.Value, LocalizationEntries.EndUppercase.Value];
        List<string[]> lines = [lineA, lineB, lineC];

        bool wrapAround = false;
        string[] activeLine = lines[creatorLineIndexY];
        float rapidMoveDelay = 0.15f;
        float rapidMoveRate = 0.125f;
        if (ActiveKeymap.Left.WasJustPressed || (ActiveKeymap.Left.PressDuration >= rapidMoveDelay && nameCharacterTimer.Cycle(rapidMoveRate)))
        {
            creatorLineIndexX--;
            if (creatorLineIndexX <= -1)
            {
                creatorLineIndexX = activeLine.Length - 1;
                wrapAround = true;
            }
        }
        if (ActiveKeymap.Right.WasJustPressed || (ActiveKeymap.Right.PressDuration >= rapidMoveDelay && nameCharacterTimer.Cycle(rapidMoveRate)))
        {
            creatorLineIndexX++;
            if (creatorLineIndexX >= activeLine.Length)
            {
                creatorLineIndexX = 0;
                wrapAround = true;
            }
        }
        if (ActiveKeymap.Up.WasJustPressed || (ActiveKeymap.Up.PressDuration >= rapidMoveDelay && nameCharacterTimer.Cycle(rapidMoveRate)))
        {
            bool onBackButton = creatorLineIndexY == 2 && creatorLineIndexX == lineC.Length - 2;
            bool onEndButton = creatorLineIndexY == 2 && creatorLineIndexX == lineC.Length - 1;

            creatorLineIndexY--;
            if (creatorLineIndexY <= -1)
            {
                creatorLineIndexY = lines.Count - 1;
                wrapAround = true;
            }

            activeLine = lines[creatorLineIndexY];
            creatorLineIndexX = int.Clamp(creatorLineIndexX, 0, activeLine.Length - 1);

            // Special case: Map to the nearest X index if on the back or end buttons.
            if (onBackButton)
                creatorLineIndexX = activeLine.Length - 4;
            if (onEndButton)
                creatorLineIndexX = activeLine.Length - 2;
        }
        if (ActiveKeymap.Down.WasJustPressed || (ActiveKeymap.Down.PressDuration >= rapidMoveDelay && nameCharacterTimer.Cycle(rapidMoveRate)))
        {
            creatorLineIndexY++;
            if (creatorLineIndexY >= lines.Count)
            {
                creatorLineIndexY = 0;
                wrapAround = true;
            }

            activeLine = lines[creatorLineIndexY];
            creatorLineIndexX = int.Clamp(creatorLineIndexX, 0, activeLine.Length - 1);
        }

        Vector2 drawPosition = new Vector2(68f, 70f);
        Vector2 startingDrawPosition = drawPosition;
        for (int i = 0; i < lines.Count; i++)
        {
            string[] line = lines[i];
            for (int j = 0; j < line.Length; j++)
            {
                bool selected = creatorLineIndexX == j && creatorLineIndexY == i;
                string lineContents = line[j];
                Color color = Color.White;
                if (selected)
                    color = new Color(255, 255, 0);

                text.Enqueue(lineContents, Font, new Vector3(drawPosition, 100f), color * creatorNamePromptOpacity, Vector2.One);

                Vector2 size = text.MeasureString(lineContents, Font);
                if (size.X >= 8f)
                    size.X--;

                if (selected)
                {
                    IdealSoulPosition = drawPosition + size * 0.5f + new Vector2(-0.5f, 0.5f);
                    if (wrapAround || creatorNamePromptOpacity <= 0.5f)
                        SoulPosition = IdealSoulPosition;

                    if (ActiveKeymap.Confirm.WasJustPressed && menuInteractionCountdown <= 0f)
                    {
                        if (lineContents == LocalizationEntries.BackUppercase.Value)
                        {
                            if (creatorName.Length >= 1)
                                creatorName = creatorName[..^1];
                        }
                        else if (lineContents == LocalizationEntries.EndUppercase.Value)
                        {
                            if (creatorName.Length >= 1)
                            {
                                creatorLineIndexX = 0;
                                creatorLineIndexY = 0;
                                NameConfirmationState = BinaryPlayerChoiceChoice.Undecided;
                                CurrentSubmenu = Submenu.ConfirmingNameForNewFile;
                                menuInteractionCountdown = 0.1f;
                            }
                        }
                        else if (creatorName.Length + lineContents.Length <= NameCharacterLimit)
                            creatorName += lineContents;

                        if (IllegalNames.Any(n => creatorName.Contains(n)))
                        {
                            Environment.Exit(0);
                            return;
                        }
                    }
                }

                drawPosition.X += size.X + 13f;
            }
            drawPosition.X = startingDrawPosition.X;
            drawPosition.Y += 20f;
        }

        float outlineOpacity = float.Lerp(0.25f, 0.5f, float.Cos01(timer * 1.75f));
        string title = LocalizationEntries.EnterYourName.Value;
        Vector3 p = new Vector3(71.5f, 28f, 100f);
        foreach (char c in title)
        {
            float titleScale = 1f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (float.Tau * i / 4f).ToUnit() * titleScale;
                text.Enqueue(c.ToString(), Font, p + new Vector3(drawOffset, -20f), Color.White * outlineOpacity * creatorNamePromptOpacity.Squared(), Vector2.One * titleScale, Vector2.One * 0.5f);
            }

            text.Enqueue(c.ToString(), Font, p, Color.White * creatorNamePromptOpacity, Vector2.One * titleScale, Vector2.One * 0.5f);

            p.X += titleScale * 9.5f;
        }

        Vector3 creatorNameDrawPosition = new Vector3(142.5f, 40f, 100f);
        text.Enqueue(creatorName, Font, creatorNameDrawPosition, Color.White * creatorNamePromptOpacity, Vector2.One);
    }

    private void RenderNameConfirmationStuff(TextRenderer text)
    {
        float outlineOpacity = float.Lerp(0.25f, 0.5f, float.Cos01(timer * 1.75f));
        string title = LocalizationEntries.ThisIsYourName.Value;
        Vector3 p = new Vector3(66f, 20f, 100f);
        foreach (char c in title)
        {
            float titleScale = 1f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (float.Tau * i / 4f).ToUnit() * titleScale;
                text.Enqueue(c.ToString(), Font, p + new Vector3(drawOffset, -20f), Color.White * outlineOpacity * creatorNameConfirmOpacity.Squared(), Vector2.One * titleScale);
            }

            text.Enqueue(c.ToString(), Font, p, Color.White * creatorNameConfirmOpacity, Vector2.One * titleScale);

            p.X += titleScale * 9.5f;
        }

        float nameScale = float.Lerp(1f, 2f, creatorNameConfirmOpacity);
        float nameRotation = float.Sin(timer * 60f) * -0.025f - 0.04f;
        Vector3 namePosition = new Vector3(156f, 50f + float.Smoothstep(creatorNameConfirmOpacity) * 46f, 100f);
        text.Enqueue(creatorName, Font, namePosition, Color.White * creatorNameConfirmOpacity, Vector2.One * nameScale, normalizedOrigin: new Vector2(0.5f, 0f), rotation: nameRotation);

        string no = LocalizationEntries.NoUppercase.Value;
        string yes = LocalizationEntries.YesUppercase.Value;
        Vector3 noPosition = new Vector3(110f, 180f, 100f);
        Vector3 yesPosition = new Vector3(190f, 180f, 100f);
        Color noColor = Color.White;
        Color yesColor = Color.White;

        Vector2 soulOffset = new Vector2(10f, 10f);

        if (screenWhitening <= 0f)
        {
            if (NameConfirmationState == BinaryPlayerChoiceChoice.Undecided)
                IdealSoulPosition = SoulPosition = (noPosition + yesPosition).AsVector2() * 0.5f + soulOffset;
            else if (NameConfirmationState == BinaryPlayerChoiceChoice.Right)
            {
                IdealSoulPosition = yesPosition.AsVector2() + soulOffset;
                yesColor = new Color(255, 255, 0);

                if (ActiveKeymap.Confirm.WasJustPressed)
                {
                    if (!UnlockedSideB)
                    {
                        screenWhitening = 0.0001f;

                        NorthernlightSaveData data = new NorthernlightSaveData(activeSaveFileNumber, creatorName);

                        // Give the player some Chapter 6 healing items for free!
                        data.WithChapter6HeaingItems();

                        data.Save();
                    }
                    else
                    {
                        CurrentSubmenu = Submenu.ChoosingSideForFile;
                        SideChoiceState = BinaryPlayerChoiceChoice.Undecided;
                    }
                }
            }
            else if (NameConfirmationState == BinaryPlayerChoiceChoice.Left)
            {
                IdealSoulPosition = noPosition.AsVector2() + soulOffset - Vector2.UnitX * 3f;
                noColor = new Color(255, 255, 0);

                if (ActiveKeymap.Confirm.WasJustPressed)
                {
                    creatorName = string.Empty;
                    creatorLineIndexX = 0;
                    creatorLineIndexY = 0;
                    CurrentSubmenu = Submenu.EnteringNameForNewFile;
                }
            }
        }

        if (ActiveKeymap.Left.WasJustPressed)
            NameConfirmationState = BinaryPlayerChoiceChoice.Left;
        if (ActiveKeymap.Right.WasJustPressed)
            NameConfirmationState = BinaryPlayerChoiceChoice.Right;

        text.Enqueue(no, Font, noPosition, noColor * creatorNameConfirmOpacity, Vector2.One);
        text.Enqueue(yes, Font, yesPosition, yesColor * creatorNameConfirmOpacity, Vector2.One);
    }

    private void RenderSideSelection(TextRenderer text)
    {
        float outlineOpacity = float.Lerp(0.25f, 0.5f, float.Cos01(timer * 1.75f));
        string title = LocalizationEntries.ChooseOne.Value;
        Vector3 p = new Vector3(66f, 20f, 100f);
        foreach (char c in title)
        {
            float titleScale = 1f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (float.Tau * i / 4f).ToUnit() * titleScale;
                text.Enqueue(c.ToString(), Font, p + new Vector3(drawOffset, -20f), Color.White * outlineOpacity * sideSelectOpacity.Squared(), Vector2.One * titleScale);
            }

            text.Enqueue(c.ToString(), Font, p, Color.White * sideSelectOpacity, Vector2.One * titleScale);

            p.X += titleScale * 9.5f;
        }

        string sideA = LocalizationEntries.SideA.Value;
        string sideB = LocalizationEntries.SideB.Value;
        Vector3 sideAPosition = new Vector3(100f, 180f, 100f);
        Vector3 sideBPosition = new Vector3(190f, 180f, 100f);
        Color sideAColor = Color.White * (1f - screenWhitening);
        Color sideBColor = Color.White * (1f - screenWhitening);

        Vector2 soulOffset = new Vector2(17f, 10f);

        if (screenWhitening <= 0f)
        {
            if (SideChoiceState == BinaryPlayerChoiceChoice.Undecided)
                IdealSoulPosition = SoulPosition = (sideAPosition + sideBPosition).AsVector2() * 0.5f + soulOffset;
            else if (ActiveKeymap.Confirm.WasJustPressed)
            {
                screenWhitening = 0.0001f;

                NorthernlightSaveData data = new NorthernlightSaveData(activeSaveFileNumber, creatorName)
                {
                    AbortedChapter5WeirdRoute = SideChoiceState == BinaryPlayerChoiceChoice.Right
                };

                // Give the player some Chapter 6 healing items for free!
                data.WithChapter6HeaingItems();

                data.Save();
            }

            if (SideChoiceState == BinaryPlayerChoiceChoice.Right)
            {
                sideBColor = new Color(255, 255, 0);
                IdealSoulPosition = sideBPosition.AsVector2() + soulOffset;
            }
            else if (SideChoiceState == BinaryPlayerChoiceChoice.Left)
            {
                sideAColor = new Color(255, 255, 0);
                IdealSoulPosition = sideAPosition.AsVector2() + soulOffset + Vector2.UnitX * 3f;
            }
        }

        if (ActiveKeymap.Left.WasJustPressed)
            SideChoiceState = BinaryPlayerChoiceChoice.Left;
        if (ActiveKeymap.Right.WasJustPressed)
            SideChoiceState = BinaryPlayerChoiceChoice.Right;

        text.Enqueue(sideA, Font, sideAPosition, sideAColor * sideSelectOpacity, Vector2.One);
        text.Enqueue(sideB, Font, sideBPosition, sideBColor * sideSelectOpacity, Vector2.One);
    }

    private void RenderVolumeConfig(TextRenderer text, bool music)
    {
        bool hoveredOver = CurrentSelection == Selection.MusicVolume;
        float currentVolume = scene.Game.Config.MusicVolume;
        string volume = LocalizationEntries.MusicVolume.Value;
        ref bool selected = ref selectingMusicVolume;
        if (!music)
        {
            hoveredOver = CurrentSelection == Selection.SoundVolume;
            currentVolume = scene.Game.Config.SoundVolume;
            volume = LocalizationEntries.SoundVolume.Value;
            selected = ref selectingSoundVolume;
        }

        Vector3 textDrawPosition = new Vector3(110f, 64f, 10f);
        if (!music)
            textDrawPosition.Y += 24f;

        Color color = selected ? new Color(255, 255, 0) : Color.White;
        text.Enqueue(volume, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(volume, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (hoveredOver)
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);

        textDrawPosition.X += text.MeasureString(volume, Font).X;

        int volumePercentage = (int)float.Round(currentVolume * 100f);
        string volumePercentageText = LocalizationEntries.ArbitraryPercentage.WithArgs(volumePercentage.ToString()).Value;
        text.Enqueue(volumePercentageText, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(volumePercentageText, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (hoveredOver)
        {
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                selected = !selected;
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
            }
            if (ActiveKeymap.Cancel.WasJustPressed)
            {
                if (selected)
                {
                    selected = false;
                    scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
                }
                else
                {
                    CurrentSubmenu = Submenu.None;
                    CurrentSelection = Selection.Config;
                    scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
                }
            }

            if (selected)
            {
                float moveDirection = 0f;
                if (ActiveKeymap.Up.PressDuration > 0f)
                    moveDirection++;
                if (ActiveKeymap.Down.PressDuration > 0f)
                    moveDirection--;

                float delta = scene.Time.DeltaTime * moveDirection * 0.2f;
                if (music)
                    scene.Game.Config.MusicVolume = float.Clamp(scene.Game.Config.MusicVolume + delta, 0f, 1f);
                else
                    scene.Game.Config.SoundVolume = float.Clamp(scene.Game.Config.SoundVolume + delta, 0f, 1f);

                scene.Audio.Volume = scene.Game.RemapVolume(1f, music: false);
                scene.Music.Player.Volume = scene.Game.RemapVolume(1f, music: true);
            }
        }
    }

    private void RenderWasdConfig(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.WasdControls;
        Vector3 textDrawPosition = new Vector3(110f, 112f, 10f);

        string wasd = LocalizationEntries.UseWasdForInput.Value;
        Color color = selected ? new Color(255, 255, 0) : Color.White;
        text.Enqueue(wasd, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(wasd, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);

        textDrawPosition.X += text.MeasureString(wasd, Font).X + 4f;

        string optionText = scene.Game.Config.WasdControlsOverride ? LocalizationEntries.Yes.Value : LocalizationEntries.No.Value;
        text.Enqueue(optionText, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(optionText, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected && ActiveKeymap.Confirm.WasJustPressed)
        {
            scene.Game.Config.WasdControlsOverride = !scene.Game.Config.WasdControlsOverride;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
        }
        if (selected && ActiveKeymap.Cancel.WasJustPressed)
        {
            CurrentSubmenu = Submenu.None;
            CurrentSelection = Selection.Config;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
        }
    }

    private void RenderBordersConfig(TextRenderer text)
    {
        bool selected = CurrentSelection == Selection.BorderConfig;
        Vector3 textDrawPosition = new Vector3(110f, 136f, 10f);

        string borderTitle = LocalizationEntries.Border.Value;
        Color color = selected ? new Color(255, 255, 0) : Color.White;
        text.Enqueue(borderTitle, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(borderTitle, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected)
            IdealSoulPosition = textDrawPosition.AsVector2() + new Vector2(-9.5f, 9.5f);

        textDrawPosition.X += text.MeasureString(borderTitle, Font).X + 4f;

        string optionText = string.Empty;
        BorderMode nextBorderMode = BorderMode.None;
        switch (scene.Game.Config.BorderMode)
        {
            case BorderMode.None:
                nextBorderMode = BorderMode.Simple;
                optionText = LocalizationEntries.None.Value;
                break;
            case BorderMode.Simple:
                nextBorderMode = BorderMode.Dynamic;
                optionText = LocalizationEntries.Simple.Value;
                break;
            case BorderMode.Dynamic:
                nextBorderMode = BorderMode.None;
                optionText = LocalizationEntries.Dynamic.Value;
                break;
        }

        text.Enqueue(optionText, Font, textDrawPosition + new Vector3(1f, 1f, -1f), Color.Black * TextOpacity, Vector2.One);
        text.Enqueue(optionText, Font, textDrawPosition, color * TextOpacity, Vector2.One);

        if (selected && ActiveKeymap.Confirm.WasJustPressed)
        {
            scene.Game.Config.BorderMode = nextBorderMode;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));
        }
        if (selected && ActiveKeymap.Cancel.WasJustPressed)
        {
            CurrentSubmenu = Submenu.None;
            CurrentSelection = Selection.Config;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("swing.wav")));
        }
    }

    private void RenderSoul(SpriteRenderer sprites)
    {
        float opacity = 1f;
        AtlasEntry soul = Atlases.Menu.SoulMini;
        if (CurrentSubmenu == Submenu.EnteringNameForNewFile || CurrentSubmenu == Submenu.ConfirmingNameForNewFile || CurrentSubmenu == Submenu.ChoosingSideForFile)
        {
            soul = Atlases.Menu.SoulBlur;
            opacity = 0.7f;
            if (CurrentSubmenu == Submenu.ConfirmingNameForNewFile)
                opacity *= creatorNameConfirmOpacity.Squared();
            if (CurrentSubmenu == Submenu.ChoosingSideForFile)
                opacity *= 1f - screenWhitening;
        }

        sprites.Enqueue(soul, new SpriteData(soul)
        {
            Position = new Vector3(SoulPosition, 20f),
            Scale = Vector2.One,
            Tint = Color.White * opacity
        }, BlendState.NonPremultiplied);
    }

    private static Color ChooseColor(bool selected) => selected ? Color.White : new Color(180, 154, 154);
}
