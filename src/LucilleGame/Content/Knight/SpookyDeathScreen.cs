using System.Numerics;
using Northernlight.Content.Menu;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     The screen responsible for the displaying of
///     glowing voice dialogue and the retrying of the
///     fight.
/// </summary>
public sealed class SpookyDeathScreen
{
    public enum PlayerSelection
    {
        Undecided,
        Left,
        Right
    }

    private record Dialogue(LanguageAgnosticText[] Lines);

    private int inputSpamCounter;

    private bool manuallyStarted;

    private bool initialized;

    private float postDeathTimer;

    private float retrySelectionPopupProgress;

    private float whitening;

    private int spelledOutCharacterCount;

    private int dialogueIndex;

    private bool playerMayMakeSelection;

    private Dialogue dialogue;

    private Sound respawn;

    private PlayerSelection selection;

    private Vector3 soulPositionAtTimeOfDeath;

    private readonly OwnedTimer speakTimer;

    private static readonly Font font = new Font("8bitoperator.ttf", 48f);

    /// <summary>
    ///     Whether the player has died or not.
    /// </summary>
    public bool PlayerDied
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the player died due to failing the
    ///     quick time events of the final attack.
    /// </summary>
    public bool FailedFinalAttackQuickTimeEvent
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the player won naturally
    ///     against the Knight or not.
    /// </summary>
    public bool PlayerWon
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the game screen should be overridden or not.
    /// </summary>
    public bool OverrideGameScreen => PlayerWon || postDeathTimer >= FreezeFrameDuration;

    /// <summary>
    ///     Whether game freeze frames are ongoing or not.
    /// </summary>
    public bool FreezeFrameOngoing => postDeathTimer > 0f && postDeathTimer < FreezeFrameDuration;

    /// <summary>
    ///     The game responsible for this screen.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
        private set;
    }

    /// <summary>
    ///     An event that is fired the moment the
    ///     player dies.
    /// </summary>
    public event Action OnDeath;

    /// <summary>
    ///     An event that is fired the moment the
    ///     player restarts the game.
    /// </summary>
    public event Action OnRestart;

    /// <summary>
    ///     How long the game freeze frame lasts
    ///     from the moment the player dies.
    /// </summary>
    public static float FreezeFrameDuration => 1.2f;

    /// <summary>
    ///     The horizontal spacing between choices.
    /// </summary>
    public static float HorizontalChoiceOffset => NorthernlightGame.KnightFightResolution.X * 0.16f;

    /// <summary>
    ///     The origin point of the choice offset.
    /// </summary>
    public static Vector3 ChoiceOrigin => new Vector3(NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 0.9f), 400f);

    public SpookyDeathScreen(KnightFightScene scene)
    {
        Scene = scene;
        speakTimer = scene.Time.Rent();
        OnRestart += () =>
        {
            scene.Music.Stop();

            Scene.RespawnKnight();
            Scene.BattleHandler = new BattleHandler(Scene, [Scene.RoaringKnight]);
            Scene.Heroes.Initialize();

            inputSpamCounter = 0;
            selection = PlayerSelection.Undecided;
            dialogueIndex = 0;
            spelledOutCharacterCount = 0;
            postDeathTimer = 0f;
            whitening = 0f;
            retrySelectionPopupProgress = 0f;
            initialized = false;
            playerMayMakeSelection = false;
            PlayerDied = false;
            PlayerWon = false;
            manuallyStarted = false;
            respawn?.Stop();
            FailedFinalAttackQuickTimeEvent = false;
            Scene.BattleHandler.Soul.RenderBlurred = false;

            Scene.battleMenu = new BattleMenu(Scene)
            {
                GameResolution = NorthernlightGame.KnightFightResolution
            };

            scene.BattleBackBackdropOpacity = 0f;
            scene.SaveData.TotalAttempts++;

            scene.SaveData.Save();

            HeroesInitializer.OnGameRestart();
        };
        OnDeath += () =>
        {
            if (Scene.SideB)
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("WeirdJingle.wav"), volume: 1.33f));
        };
    }

    /// <summary>
    ///     Starts this animation sequence.
    /// </summary>
    public void StartAnimation() => manuallyStarted = true;

    /// <summary>
    ///     Updates the death screen.
    /// </summary>
    public void Update()
    {
        if (!PlayerDied && !manuallyStarted)
        {
            PlayerDied = true;
            foreach (Hero hero in Scene.Heroes.All)
            {
                if (hero.HP >= 1)
                {
                    PlayerDied = false;
                    break;
                }
            }

            soulPositionAtTimeOfDeath = Scene.BattleHandler.Soul.Position;
            postDeathTimer = 0f;
            if (PlayerDied)
                OnDeath?.Invoke();

            return;
        }

        if (!PlayerWon)
            HandleInputSpamSkip();

        postDeathTimer += Scene.Time.RawDeltaTime;

        Soul soul = Scene.BattleHandler.Soul;
        soul.Rotation = float.LerpDt(soul.Rotation, 0f, 3f, Scene.Time);
        soul.Update();
        if (FreezeFrameOngoing)
        {
            soul.Opacity = 1f;
            soul.ImmunityFramesCountdown = 0.0001f; // Done to prevent the soul hit sound playing repeatedly.
            soul.GrazeIndicatorIntensity = 0f;
            soul.Velocity = Vector3.Zero;
            soul.Position = soulPositionAtTimeOfDeath;

            if (!PlayerWon)
                Scene.Music.Stop();
            else
                soul.Position = Vector3.One * -99999f;

            Scene.Time.GlobalSpeed = 0f;
            speakTimer.Reset();
            return;
        }

        if (!initialized)
        {
            initialized = true;
            if (Scene.SaveData.TotalAttempts == 0)
            {
                dialogue = new Dialogue(
                [
                    LocalizationEntries.GlowingVoiceAttempts1Line1,
                    LocalizationEntries.GlowingVoiceAttempts1Line2,
                    LocalizationEntries.GlowingVoiceAttempts1Line3
                ]);
            }
            else if (Scene.SaveData.TotalAttempts == 1)
            {
                dialogue = new Dialogue(
                [
                    LocalizationEntries.GlowingVoiceAttempts2Line1,
                    LocalizationEntries.GlowingVoiceAttempts2Line2
                ]);
            }
            else
            {
                dialogue = new Dialogue(
                [
                    LocalizationEntries.GlowingVoiceAttempts3Line1
                ]);
            }

            if (FailedFinalAttackQuickTimeEvent)
            {
                dialogue = new Dialogue(
                [
                    LocalizationEntries.GlowingVoiceYouFailedTheQuickTimeEventButHow
                ]);
            }

            if (PlayerWon)
            {
                dialogue = new Dialogue(
                [
                    LocalizationEntries.GlowingVoiceWinLine1,
                    LocalizationEntries.GlowingVoiceWinLine2,
                    LocalizationEntries.GlowingVoiceWinLine3.WithArgs(Scene.SaveData.Name.ToUpper()),
                    LocalizationEntries.GlowingVoiceWinLine4
                ]);
            }

            bool cheater = Scene.RoaringKnight.HP <= 0;
            if (cheater)
            {
                dialogue = new Dialogue(
                [
                    LocalizationEntries.GlowingVoiceCheatText
                ]);
            }

            if (!PlayerWon)
                Scene.Music.Add(new Sound(AudioBuffer.FromWav("AUDIO_DRONE.wav", musicDirectory: true)));
        }

        bool canSpeak = whitening <= 0f && MoveSoulUp();
        if (canSpeak)
            UpdateDialogue();

        if (playerMayMakeSelection)
        {
            if (PlayerWon)
            {
                Scene.Music.Player.Volume = float.LerpDt(Scene.Music.Player.Volume, 0f, 2.1f, Scene.Time);
                if (Scene.Music.Player.Volume <= 0.0005f)
                {
                    Scene.Music.Stop();
                    Scene.Music.Player.Volume = Scene.Game.RemapVolume(1f, music: true);
                    Scene.SaveData.TotalWins++;
                    Scene.SaveData.Save();

                    MenuScene menu = new MenuScene(Scene.Game);
                    menu.Initialize();
                    Scene.Game.PresentScene(menu);
                }
            }
            else
            {
                soul.Opacity = 1f - retrySelectionPopupProgress;
                if (retrySelectionPopupProgress >= 1f)
                {
                    soul.RenderBlurred = true;
                    soul.Opacity = 1f;
                }

                bool popupWasInProgress = retrySelectionPopupProgress < 1f;
                retrySelectionPopupProgress = float.Clamp(retrySelectionPopupProgress + Scene.Time.DeltaTime * 2f, 0f, 1f);
                if (retrySelectionPopupProgress >= 1f && popupWasInProgress)
                    soul.Position = ChoiceOrigin;

                if (retrySelectionPopupProgress >= 1f)
                {
                    Vector3 destinationOffset = Vector3.Zero;
                    switch (selection)
                    {
                        case PlayerSelection.Left:
                            destinationOffset.X -= HorizontalChoiceOffset;
                            break;
                        case PlayerSelection.Right:
                            destinationOffset.X += HorizontalChoiceOffset * 0.99f;
                            break;
                    }

                    soul.Position = Vector3.LerpDt(soul.Position, ChoiceOrigin + destinationOffset, 50f, Scene.Time);

                    if (whitening <= 0f)
                    {
                        if (ActiveKeymap.Left.WasJustPressed)
                            selection = PlayerSelection.Left;
                        else if (ActiveKeymap.Right.WasJustPressed)
                            selection = PlayerSelection.Right;

                        if (selection != PlayerSelection.Undecided && ActiveKeymap.Confirm.WasJustPressed)
                            BeginRestart();
                    }
                }
            }
        }

        if (whitening > 0f)
        {
            whitening += Scene.Time.DeltaTime * 0.25f;

            respawn.Stream.FrequencyRatio = 0.6f;
            respawn.Stream.Volume = whitening * 1.75f;
            if (whitening >= 1f)
            {
                whitening = 0f;
                respawn.Stream.Volume = 0f;
                respawn.Stop();

                OnRestart?.Invoke();
            }
        }
    }

    private void HandleInputSpamSkip()
    {
        if (ActiveKeymap.Confirm.WasJustPressed || ActiveKeymap.Cancel.WasJustPressed)
        {
            inputSpamCounter++;
            if (inputSpamCounter == 16)
                BeginRestart();
        }
    }

    private void BeginRestart()
    {
        if (whitening > 0f)
            return;

        respawn?.Dispose();
        respawn = new Sound(AudioBuffer.FromWav("Restart.wav"))
        {
            Loop = true
        };
        Scene.Audio.Play(respawn);
        whitening = 0.001f;
    }

    private bool MoveSoulUp()
    {
        float moveUpDelay = FreezeFrameDuration + 2.5f;
        float moveUpTime = 0.75f;
        float speakDelay = 1.85f;
        bool done = postDeathTimer >= moveUpDelay + moveUpTime + speakDelay;
        if (done)
            return true;

        Soul soul = Scene.BattleHandler.Soul;
        float soulMoveProgress = float.InverseLerp(0f, moveUpTime, postDeathTimer - moveUpDelay);
        Vector3 topCenter = new Vector3(NorthernlightGame.KnightFightResolution.X * 0.5f, NorthernlightGame.KnightFightResolution.X * 0.08f, soul.Position.Z);

        soul.Velocity = Vector3.Zero;
        soul.Position = Vector3.Lerp(soulPositionAtTimeOfDeath, topCenter, float.Smoothstep(soulMoveProgress));

        return false;
    }

    private void UpdateDialogue()
    {
        void AdvanceLine()
        {
            if (dialogueIndex < dialogue.Lines.Length - 1)
            {
                spelledOutCharacterCount = 0;
                dialogueIndex++;
            }
            else
                playerMayMakeSelection = true;
        }

        string line = dialogue.Lines[dialogueIndex].Value;
        if (ActiveKeymap.Confirm.WasJustPressed)
        {
            if (spelledOutCharacterCount == line.Length)
                AdvanceLine();
            else
                spelledOutCharacterCount = line.Length;
            speakTimer.Reset();
        }

        if (spelledOutCharacterCount < line.Length)
        {
            float speakRate = 0.2f;
            if (line[spelledOutCharacterCount] == '\n')
                speakRate *= 6f;

            if (speakTimer.Cycle(speakRate))
                spelledOutCharacterCount++;
        }

        else
        {
            float lineTransitionDelay = PlayerWon ? 2.67f : 2f;
            if (speakTimer.Cycle(lineTransitionDelay))
                AdvanceLine();
        }
    }

    /// <summary>
    ///     Prepraes this death screen for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, TextRenderer text)
    {
        if (!OverrideGameScreen)
            return;

        if (dialogue is null)
        {
            sprites.Enqueue(Atlases.KnightScene.Pixel, new SpriteData(Atlases.KnightScene.Pixel)
            {
                Position = new Vector3(0f, 0f, 700f),
                Scale = Vector2.One * 99999f,
                Tint = Color.Black
            }, BlendState.AlphaBlend, drawToDepthBuffer: false);
            return;
        }

        int remainingCharacters = spelledOutCharacterCount;
        float scale = 1f;
        float horizontalCharacterSpacing = 10f;
        float verticalLineSpacing = NorthernlightGame.KnightFightResolution.X * 0.085f;
        float HorizontalSpace(char c)
        {
            return (text.MeasureString(c.ToString(), font).X + horizontalCharacterSpacing) * scale;
        }

        string line = dialogue.Lines[dialogueIndex].Value;
        Vector3 center = new Vector3(NorthernlightGame.KnightFightResolution.X * 0.5f, NorthernlightGame.KnightFightResolution.X * 0.15f, 400f);

        Vector3 movingStepBack = Vector3.Zero;
        List<Vector3> newlineOffsets = [];
        for (int i = 0; i < line.Length; i++)
        {
            movingStepBack.X -= HorizontalSpace(line[i]) * 0.5f;
            if (i == line.Length - 1 || line[i] == '\n')
            {
                newlineOffsets.Add(movingStepBack);
                movingStepBack = Vector3.Zero;
            }
        }

        int lineIndex = 1;
        float outlineOpacity = float.Lerp(0.35f, 0.65f, float.Cos01(postDeathTimer * 2.3f));
        Vector3 p = center + newlineOffsets[0];

        foreach (char c in line)
        {
            if (remainingCharacters <= 0)
                break;

            remainingCharacters--;

            if (c == '\n')
            {
                p.X = center.X + newlineOffsets[lineIndex].X;
                p.Y += verticalLineSpacing * scale;
                lineIndex++;
                continue;
            }

            p.X += HorizontalSpace(c);
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (float.Tau * i / 4f).ToUnit() * scale * 3f;
                text.Enqueue(c.ToString(), font, p + new Vector3(drawOffset, -20f), Color.White * outlineOpacity * (1f - whitening), Vector2.One * scale, Vector2.One * 0.5f);
            }

            text.Enqueue(c.ToString(), font, p, Color.White * (1f - whitening), Vector2.One * scale, Vector2.One * 0.5f);
        }

        LanguageAgnosticText retryText = LocalizationEntries.GoBackText;
        if (retrySelectionPopupProgress > 0f)
        {
            float descent = float.Smoothstep(1f - retrySelectionPopupProgress) * 120f;
            Vector3 left = ChoiceOrigin - Vector3.UnitX * HorizontalChoiceOffset;
            Vector3 right = ChoiceOrigin + Vector3.UnitX * HorizontalChoiceOffset;
            left.Y += descent;
            right.Y += descent;

            Color hoverColor = new Color(255, 255, 0);

            text.Enqueue(retryText.Value, font, left, (selection == PlayerSelection.Left ? hoverColor : Color.White) * (1f - whitening), Vector2.One * scale, new Vector2(0.5f, 0.5f));
            text.Enqueue(retryText.Value, font, right, (selection == PlayerSelection.Right ? hoverColor : Color.White) * (1f - whitening), Vector2.One * scale, new Vector2(0.5f, 0.5f));
        }

        sprites.Enqueue(Atlases.KnightScene.Pixel, new SpriteData(Atlases.KnightScene.Pixel)
        {
            Position = new Vector3(0f, 0f, 700f),
            Scale = Vector2.One * 99999f,
            Tint = Color.White * whitening
        }, BlendState.Additive, drawToDepthBuffer: false);
    }
}
