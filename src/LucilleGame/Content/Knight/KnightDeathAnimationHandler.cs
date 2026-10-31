using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.Credits;
using Northernlight.Content.Knight.Dialogue;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Content.Knight.Particles;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     The handler responsible for this Side B Knight
///     death animation.
/// </summary>
public sealed class KnightDeathAnimationHandler
{
    public enum Substate
    {
        FadeToWhite,
        AwkwardPause,
        KnightFakeStaticDeath,
        KrisWalksForward,
        KnightAttemptsToImpaleKris,
        AwaitPlayerPrompt,
        JumpCutToBlack,
        Snowgrave
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Uniforms
    {
        public Vector4 GrayscaleInterpolant;

        public Vector4 GrayscaleExemptionBrightness;

        public Vector4 GrayscaleExemptionTolerance;

        public Vector4 FadeToBlack;
    }

    private int staticReboundCounter;

    private bool madeSoulAppear;

    private bool playedSnowgraveSound;

    private bool createdDamageVisual;

    private float snowScrollX;

    private float snowPillarHeight;

    private float ballFormCountdown;

    private float staticPitchChangeCountdown;

    private float whitening;

    private float snowOpacity;

    private Sound staticSound;

    private readonly OwnedTimer neverendingTimer;

    private static bool jumpCutActive;

    private static float grayscaleInterpolant;

    private static SpriteRenderer.ShaderWithPreparations? shaderPreparations;

    /// <summary>
    ///     The battle handler overseeing this handerl
    /// </summary>
    public BattleHandler Battle
    {
        get;
    }

    /// <summary>
    ///     The current substate of this handler.
    /// </summary>
    public Substate CurrentSubstate
    {
        get;
        set;
    }

    /// <summary>
    ///     The timer used by this handler.
    /// </summary>
    public OwnedTimer Timer => Battle.StateTimer;

    /// <summary>
    ///     The scene associated with this handler.
    /// </summary>
    public KnightFightScene Scene => Battle.Scene;

    public KnightDeathAnimationHandler(BattleHandler battle)
    {
        Battle = battle;
        neverendingTimer = Scene.Time.Rent();
    }

    /// <summary>
    ///     Updates this handler.
    /// </summary>
    public void Update()
    {
        int ballTransformationFrames = 4;
        RoaringKnight knight = Battle.Scene.RoaringKnight;
        knight.EmitsAfterimages = false;
        knight.RepositionDelay = 0.2f;

        Scene.Backdrop.AnimationSpeed = 1f;

        Hero kris = Scene.Heroes.Kris;
        Hero noelle = Scene.Heroes.Noelle;

        shaderPreparations ??= new SpriteRenderer.ShaderWithPreparations(Shaders.KnightDeathAnimationShader, ApplyShaderPreparations);

        switch (CurrentSubstate)
        {
            case Substate.FadeToWhite:
                Scene.BattleHandler.SoulBodyExitVisualsProgress = 0f;

                float whiteningAnimationDuration = 2.85f;
                float whiteningAnimationProgress = Timer / whiteningAnimationDuration;

                whitening = float.InverseLerpBump(0f, 0.25f, 0.8f, 1f, whiteningAnimationProgress);
                if (Scene.Music.Player.Volume <= 0f)
                    Scene.Music.Stop();
                else
                    Scene.Music.Player.Volume = float.Min(Scene.Music.Player.Volume, Scene.Game.RemapVolume((1f - whitening).Squared(), music: true));

                knight.EmitsAfterimages = true;
                knight.Velocity = Vector3.LerpDt(knight.Velocity, Vector3.Zero, 10f, Scene.Time);

                if (whitening >= 1f)
                {
                    knight.Position = knight.SpawnPosition;
                    foreach (Hero hero in Scene.Heroes.All)
                        hero.ActiveAnimation = hero.StandStillAnimation;

                    // Kris get up NOW!!!!
                    kris.HP = 1;
                    noelle.HP = 1;
                }

                if (whiteningAnimationProgress >= 1f)
                {
                    Scene.Music.Player.Volume = Scene.Game.RemapVolume(1f, music: true);
                    Scene.Music.Add(new Sound(AudioBuffer.FromWav("BlizzardWindLoop.wav"), 0.5f));

                    CurrentSubstate = Substate.AwkwardPause;
                    Timer.Reset();
                }

                break;
            case Substate.AwkwardPause:
                Scene.BattleHandler.SoulBodyExitVisualsProgress = 0f;

                float staticDelay = 0.25f;
                float delayProgress = float.InverseLerp(0f, staticDelay, Timer);
                knight.EmitsAfterimages = true;

                if (delayProgress >= 1f)
                {
                    RestartStaticSound();
                    knight.SetTexture(Atlases.KnightScene.KnightBallFormTransition, ballTransformationFrames, loops: false);

                    CurrentSubstate = Substate.KnightFakeStaticDeath;
                    Timer.Reset();
                }
                break;

            case Substate.KnightFakeStaticDeath:
                bool goBackToStaticAfterBallAnimation = knight.RenderState.Texture == Atlases.KnightScene.KnightBallFormAnimation && ballFormCountdown <= 0f;
                bool goToStaticAfterTransition = knight.RenderState.Texture == Atlases.KnightScene.KnightBallFormTransition && knight.RenderState.CurrentFrame >= ballTransformationFrames - 1;

                if (goBackToStaticAfterBallAnimation)
                    knight.SetTexture(Atlases.KnightScene.KnightBallFormTransition, ballTransformationFrames, loops: true);

                if (goToStaticAfterTransition)
                    knight.SetTexture(Atlases.KnightScene.KnightFakeDeath, 3, loops: true);

                knight.FrameUpdateRateSpeedup = 1.75f;
                if (knight.RenderState.Texture == Atlases.KnightScene.KnightFakeDeath)
                    knight.FrameUpdateRateSpeedup = 1f;

                float transitionProgress = float.InverseLerp(0f, 5f, staticReboundCounter);
                float ballEnterRate = float.Lerp(0.67f, 0.4f, transitionProgress);
                if (transitionProgress < 1f && Timer.Cycle(ballEnterRate))
                {
                    staticReboundCounter++;
                    if (ballFormCountdown <= 0f)
                        ballFormCountdown = 0.2f;
                    knight.SetTexture(Atlases.KnightScene.KnightBallFormAnimation, 5, loops: true);
                    knight.Velocity = new Vector3(Scene.Rng.NextCircularEdge(200f), 0f);
                    Scene.AddScreenShake(3f);
                    staticSound.Stream.FrequencyRatio = Scene.Rng.NextFloat(0.75f, 1.5f);
                }

                if (ballFormCountdown <= 0f)
                {
                    knight.Velocity = Vector3.LerpDt(knight.Velocity, Vector3.Zero, 32f, Scene.Time);
                    CreateKnightAfterimages();
                }
                if (transitionProgress >= 1f)
                {
                    knight.RepositionDelay = 0f;

                    if (staticPitchChangeCountdown <= 0f)
                    {
                        staticPitchChangeCountdown = 0.6f;
                        staticSound.Stream.FrequencyRatio = Scene.Rng.NextFloat(0.6f, 1.6f);
                    }
                    if (Timer >= 2.4f)
                    {
                        CurrentSubstate = Substate.KrisWalksForward;
                        Timer.Reset();
                    }
                }

                break;
            case Substate.KrisWalksForward:
                float walkDuration = 0.8f;
                float walkProgress = float.InverseLerp(0f, walkDuration, Timer);
                float transitionDelay = 2.6f;
                Vector3 start = kris.StartingPosition;
                Vector3 end = kris.StartingPosition + new Vector3(160f, 30f, 0f);
                kris.Position = Vector3.Lerp(start, end, walkProgress);
                if (walkProgress >= 1f)
                {
                    if (Timer >= transitionDelay * 0.5f)
                        kris.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.KrisStandStillLookingDown, totalHorizontalFrames: 1, game: Scene.Game);
                    else
                        kris.ActiveAnimation = kris.StandStillAnimation;

                    if (!madeSoulAppear && Timer >= transitionDelay - 0.15f)
                    {
                        madeSoulAppear = true;
                        Battle.SoulBodyExitVisualsProgress = 0.001f;
                    }
                }
                else if (kris.ActiveAnimation.Texture != Atlases.KnightScene.KrisWalk)
                {
                    float walkSpeed = 15.6f / (Vector3.Distance(start, end) + 1f) / walkDuration;
                    kris.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.KrisWalk, totalHorizontalFrames: 4, game: Scene.Game, loops: true, animationRateSeconds: walkSpeed);
                }

                float idealKnightXPosition = end.X + 600f;
                float idealKnightYPosition = kris.Position.Y - 54f;
                Vector3 idealKnightVelocity = new Vector3((idealKnightXPosition - knight.Position.X) * 0.9f, (idealKnightYPosition - knight.Position.Y) * 0.9f, 0f);
                knight.Velocity = Vector3.LerpDt(knight.Velocity, idealKnightVelocity, 22.5f, Scene.Time);

                CreateKnightAfterimages();
                if (staticPitchChangeCountdown <= 0f)
                {
                    staticPitchChangeCountdown = 0.6f;
                    staticSound.Stream.FrequencyRatio = Scene.Rng.NextFloat(0.6f, 1.6f);
                }

                if (Timer >= transitionDelay)
                {
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), 0.75f));
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightDaggerDash.wav"), 0.8f));
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightAppearanceDash.wav"), 1.2f));

                    knight.SetTexture(Atlases.KnightScene.KnightImpalingAttack, 1);
                    knight.Velocity = Vector3.UnitX * -5900f;

                    Scene.AddScreenShake(20f);

                    Angle rotation = -0.38f;
                    Vector3 clashSpawnPosition = knight.Position;
                    Vector2 direction = Vector2.UnitX;
                    Scene.ClashParticles.Add(new ClashParticle()
                    {
                        Texture = Atlases.KnightScene.SusieClashParticle,
                        Velocity = Vector3.Zero,
                        Position = clashSpawnPosition,
                        TotalFrames = 4,
                        Animates = true,
                        Rotation = rotation
                    });
                    Scene.ClashParticles.Add(new ClashParticle()
                    {
                        Texture = Atlases.KnightScene.SusieClashParticle,
                        Velocity = direction.AsVector3() * 100f,
                        Position = clashSpawnPosition,
                        TotalFrames = 4,
                        Animates = false,
                        Rotation = rotation
                    });

                    CurrentSubstate = Substate.KnightAttemptsToImpaleKris;
                    Timer.Reset();
                    StopStaticSound();
                }

                break;

            case Substate.KnightAttemptsToImpaleKris:
                knight.Velocity = Vector3.LerpDt(knight.Velocity, Vector3.Zero, 30f, Scene.Time);
                knight.EmitsAfterimages = knight.Velocity.Length() >= 500f;

                whitening = float.InverseLerp(0f, 0.25f, 0.4f);
                if (whitening >= 0.7f)
                    Battle.SoulBodyExitVisualsProgress = 0f;

                if (Timer >= 0.7f)
                {
                    CurrentSubstate = Substate.AwaitPlayerPrompt;
                    Timer.Reset();
                }

                break;
            case Substate.AwaitPlayerPrompt:
                whitening = 1f;
                grayscaleInterpolant = 1f;
                Battle.SoulBodyExitVisualsProgress = 0f;
                Scene.DefaultLayer.ForceDeferment();
                Scene.UILayer.ForceDeferment();
                Scene.SoulLayer.ForceDeferment();
                Scene.SpecialImageLayer.ForceDeferment();
                Scene.ShatterLayer.ForceDeferment();
                if (Scene.DialogueBox is null)
                {
                    PlayerSelectionDialogueBoxUI.Option option = new PlayerSelectionDialogueBoxUI.Option(LocalizationEntries.Nothing, b =>
                    {
                        noelle.Position = Vector3.One * -99999f;

                        whitening = 0f;
                        CurrentSubstate = Substate.JumpCutToBlack;
                        Timer.Reset();

                        b.Close();
                    });
                    Scene.DialogueBox = new PlayerSelectionDialogueBoxUI([option, option, option, option])
                    {
                        Scene = Scene,
                        OutlineColor = new Color(255, 0, 0)
                    };

                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("Text.wav")));
                }
                break;
            case Substate.JumpCutToBlack:
                float jumpCutDuration = 0.4f;
                bool jumpCutWasActive = jumpCutActive;
                jumpCutActive = Timer <= jumpCutDuration;
                grayscaleInterpolant = 1f;

                whitening = 1f;
                if (Timer >= jumpCutDuration * 0.333f)
                    whitening = 0f;

                if (jumpCutWasActive != jumpCutActive)
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("Jumpcut.wav"), 1.2f));

                Scene.Backdrop.AnimationSpeed = 0f;

                if (Timer >= 2f)
                {
                    snowPillarHeight = -1600f;
                    CurrentSubstate = Substate.Snowgrave;
                    Timer.Reset();
                }
                break;

            case Substate.Snowgrave:
                Scene.Backdrop.AnimationSpeed = 0f;
                if (!playedSnowgraveSound)
                {
                    playedSnowgraveSound = true;
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("Snowgrave.wav"), 1.55f));
                }

                snowPillarHeight += Scene.Time.DeltaTime * snowOpacity * 2000f;

                if (Timer >= 1.4f && knight.RenderState.Texture != Atlases.KnightScene.KnightFrozen)
                    knight.SetTexture(Atlases.KnightScene.KnightFrozen, 1);

                if (Timer >= 3f && !createdDamageVisual)
                {
                    int damage = (int)float.Ceiling(knight.MaxHP * Scene.Rng.NextFloat(2f, 3f));
                    Vector3 indicatorSpawnPosition = knight.Position + new Vector3(0f, Scene.Rng.NextFloat(-120f, 100f), 120f);
                    Vector3 indicatorVelocity = new Vector3(Scene.Rng.NextFloat(700f, 800f), Scene.Rng.NextFloat(-210f, -150f), 0f);

                    createdDamageVisual = true;
                    Scene.DamageIndicators.Add(new DamageIndicator()
                    {
                        Value = damage,
                        Miss = false,
                        Position = indicatorSpawnPosition,
                        Velocity = indicatorVelocity,
                        Scale = Vector2.One,
                        Color = noelle.DamageColor
                    });
                }

                snowScrollX -= float.Clamp(Timer * 300f + 400f, 400f, 1400f) * snowOpacity * Scene.Time.DeltaTime;
                snowOpacity = float.InverseLerpBump(0f, 0.85f, 5f, 10f, Timer);
                if (snowOpacity >= 0.6f)
                    Scene.AddScreenShake(snowOpacity);

                Scene.Music.Player.Volume = Scene.Game.RemapVolume(snowOpacity.Squared(), music: true);

                if (!jumpCutActive && Timer >= 11.75f)
                {
                    Scene.SaveData.TotalWins++;
                    Scene.SaveData.Save();

                    jumpCutActive = true;
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("Jumpcut.wav"), 1.2f));
                }

                if (Timer >= 15f)
                {
                    CreditsScene credits = new CreditsScene(Scene.Game, Scene.SaveData.Slot)
                    {
                        CloseGameAtEnd = true
                    };
                    credits.Initialize();
                    Scene.Game.PresentScene(credits);
                }

                break;
        }

        ballFormCountdown = float.Max(0f, ballFormCountdown - Scene.Time.DeltaTime);
        staticPitchChangeCountdown = float.Max(0f, staticPitchChangeCountdown - Scene.Time.DeltaTime);

        if (grayscaleInterpolant > 0f || jumpCutActive)
            Battle.SoulBodyExitVisualsProgress = 0f;
        if (Battle.SoulBodyExitVisualsProgress > 0f)
        {
            float max = neverendingTimer * 2.5f % 1f + 0.001f;
            Battle.SoulBodyExitVisualsProgress = float.Clamp(Battle.SoulBodyExitVisualsProgress + Scene.Time.DeltaTime * 0.85f, 0f, max);
        }

        Scene.DefaultLayer.ScreenShaders.Activate(shaderPreparations, true);
    }

    private void CreateKnightAfterimages()
    {
        RoaringKnight knight = Battle.Scene.RoaringKnight;
        float afterimageLifetime = 0.9f;
        knight.Afterimages.Add(new KnightAfterimage()
        {
            RenderState = knight.RenderState,
            Lifetime = afterimageLifetime,
            Position = knight.Position + new Vector3(Scene.Rng.NextCircular(25f), 0f) + Vector3.UnitZ * 10f,
            HorizontallyFlip = knight.HorizontallyFlip,
            Scale = Vector2.One * knight.Scale,
            Velocity = new Vector3(Scene.Rng.NextCircularEdge(74f) / afterimageLifetime, 0f)
        });
    }

    private void RestartStaticSound()
    {
        float pitch = 1f;
        if (staticSound is not null)
            staticSound?.Stop();
        else
            pitch = 1.8f;

        AudioBuffer buffer = AudioBuffer.FromWav("Static.wav");
        staticSound = new Sound(buffer)
        {
            Loop = true,
        };
        staticSound.Stream.FrequencyRatio = pitch;
        Scene.Audio.Play(staticSound);
    }

    private void StopStaticSound() => staticSound?.Stop();

    /// <summary>
    ///     Resets this handler's internal state.
    /// </summary>
    public void Reset()
    {
        staticReboundCounter = 0;
        madeSoulAppear = false;
        playedSnowgraveSound = false;
        createdDamageVisual = false;
        snowScrollX = 0f;
        snowPillarHeight = 0f;
        ballFormCountdown = 0f;
        staticPitchChangeCountdown = 0f;
        whitening = 0f;
        snowOpacity = 0f;
        jumpCutActive = false;
        grayscaleInterpolant = 0f;
        staticSound?.Stop();
        CurrentSubstate = Substate.FadeToWhite;
        staticSound?.Stop();

        Scene.Heroes.Kris.ActiveAnimation = Scene.Heroes.Kris.StandStillAnimation;
        Scene.Heroes.Kris.Position = Scene.Heroes.Kris.StartingPosition;
        Scene.Heroes.Noelle.Position = Scene.Heroes.Noelle.StartingPosition;
    }

    /// <summary>
    ///     Prepares this handler for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        if (whitening > 0f)
        {
            AtlasEntry pixel = Atlases.Menu.Pixel;
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = Vector3.UnitZ * 100f,
                Scale = Vector2.One * 99999f,
                Tint = Color.White * whitening
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }
        if (snowOpacity > 0f)
            Scene.DefaultLayer.PostDrawActions.Enqueue(RenderSnowgraveVisuals);
    }

    private void RenderSnowgraveVisuals(Renderer renderer)
    {
        float scale = 4f;
        AtlasEntry snowBackground = Atlases.KnightScene.SnowgraveSnowParticles;
        SpriteRenderer sprites = renderer.Get<SpriteRenderer>();
        Vector2 areaCoverage = Scene.Window.Size;
        RectangleF area = new RectangleF(-100f, -100f, areaCoverage.X + 200f, areaCoverage.Y + 200f);

        Vector3 WrapDrawPosition(Vector3 drawPosition)
        {
            float xInterpolant = float.InverseLerp(area.Left, area.Right, drawPosition.X, clamp: false).Modulo(1f);
            float yInterpolant = float.InverseLerp(area.Top, area.Bottom, drawPosition.Y, clamp: false).Modulo(1f);
            float x = float.Lerp(area.Left, area.Right, xInterpolant);
            float y = float.Lerp(area.Top, area.Bottom, yInterpolant);

            return new Vector3(x, y, drawPosition.Z);
        }

        AtlasEntry snowflake = Atlases.KnightScene.IceShockSnowflake;
        Vector3 origin = Scene.RoaringKnight.Position + Vector3.UnitX * 64f;
        if (Scene.MainCamera is RestrictedOrthographicCamera camera)
        {
            origin.X += (Scene.Window.Width - camera.EffectiveWidth) * 0.5f;
            origin.Y += (Scene.Window.Height - camera.EffectiveHeight) * 0.5f;
        }

        Random rng = new Random(771);
        for (int y = 0; y < 200; y++)
        {
            float yScale = 1f;
            for (int x = -2; x <= 2; x++)
            {
                float sineTimer = Timer * 20f + x * 0.5f + y * 0.25f;
                float xScale = float.Sin01(sineTimer + x * 2f + y * 3f) + 1f;

                Vector3 drawPosition = origin + new Vector3(float.Sin(sineTimer / 7f) * x * 85f, y * 30f - snowPillarHeight, 0f);
                drawPosition.X += rng.NextFloat(-5f, 5f);
                drawPosition.Y += rng.NextFloat(-5f, 5f);
                origin.Z = rng.NextFloat(-10f, 10f);

                sprites.Enqueue(snowflake, new SpriteData(snowflake)
                {
                    Position = drawPosition,
                    Scale = new Vector2(xScale, yScale) * 1.2f,
                    Tint = Color.White
                }, drawToDepthBuffer: false);
            }
        }

        for (float y = area.Top; y < area.Bottom; y += snowBackground.Area.Height * scale)
        {
            for (float x = area.Left; x < area.Right; x += snowBackground.Area.Width * scale)
            {
                Vector3 drawPosition = new Vector3(x, y, 100f);
                sprites.Enqueue(snowBackground, new SpriteData(snowBackground)
                {
                    Position = WrapDrawPosition(drawPosition + new Vector3(snowScrollX * 0.667f, neverendingTimer * -180f, 0f) * scale),
                    Scale = Vector2.One * scale,
                    Tint = Color.White * snowOpacity * 0.5f
                }, BlendState.NonPremultiplied);
                sprites.Enqueue(snowBackground, new SpriteData(snowBackground)
                {
                    Position = WrapDrawPosition(drawPosition + new Vector3(snowScrollX, neverendingTimer * -240f, 0f) * scale),
                    Scale = Vector2.One * scale,
                    Tint = Color.White * snowOpacity
                }, BlendState.NonPremultiplied);
            }
        }

        AtlasEntry gradient = Atlases.LargeTextures.SnowgraveGradient;
        Vector2 gradientScale = areaCoverage / gradient.Area.Size;
        sprites.Enqueue(gradient, new SpriteData(gradient)
        {
            Position = new Vector3(areaCoverage * 0.5f, 20f),
            Scale = gradientScale,
            Tint = Color.White * snowOpacity * 0.6f
        }, BlendState.Additive);
    }

    private static void ApplyShaderPreparations(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            FadeToBlack = Vector4.One * (jumpCutActive ? 1f : 0f),
            GrayscaleExemptionBrightness = Vector4.One * 0.75f,
            GrayscaleExemptionTolerance = Vector4.One * 0.33f,
            GrayscaleInterpolant = Vector4.One * grayscaleInterpolant
        });
    }
}
