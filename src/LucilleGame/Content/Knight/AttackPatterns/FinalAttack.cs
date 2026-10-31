using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class FinalAttack : Attack<RoaringKnight>
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Uniforms
    {
        public Vector4 PassiveSilhouetteColor;

        public Vector4 ActiveSilhouetteColor;

        public Vector4 Intensity;
    }

    public readonly record struct Stats(float AnimationStartDuration, int StarDamage, int WaveDamage, float MaxKnightSlashSpeedup, float StarSpeedFactor, Vector2 StartingBoxSize);

    private int quickTimeEventCounter;

    private bool startingAnimationFinished;

    private bool depthsAreEmerging;

    private bool quickTimeEventsOngoing;

    private bool attackIsOver;

    private bool threwBlades;

    private bool attacking;

    private bool playedRoarSound;

    private bool playedDashSound;

    private float AttackerOpacity;

    private float quickTimeEventCountdown;

    private float quickTimeEventDuration;

    private float quickTimeEventImpactVisualsTimer;

    private float attackFinishTimer;

    private float stabDelay;

    private float throwTimer;

    private float knightPushBackDistance;

    private float daggerAnticipationCountdown;

    private static Color passiveSilhouetteColor;

    private static Color activeSilhouetteColor;

    private Vector2 relativeSlashHoverOffset;

    private OwnedTimer slashTimer;

    private OwnedTimer postAttackTimer;

    private DaggerPuppetAction activeDaggerAction;

    private readonly List<ThrownBlackDagger> daggers = [];

    private readonly Stats stats;

    private readonly FinalAttackDepthsHandler depthsHandler;

    private static float silhoutteIntensity;

    private static float feverAfterimageIntensity;

    private static Texture previousTargetContents;

    private static SpriteRenderer.ShaderWithPreparations? shaderPreparations;

    private delegate void DaggerPuppetAction(int index, ThrownBlackDagger dagger);

    /// <summary>
    ///     Whether the player failed the quick time event.
    /// </summary>
    public bool PlayerFailedQTE
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of quick time events required of the player.
    /// </summary>
    public static int QuickTimeEventCount => 3;

    public override Vector2 StartingBoxSize => stats.StartingBoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextFinalAttack;

    public FinalAttack(BattleHandler battle, Stats stats) : base(battle)
    {
        this.stats = stats;
        depthsHandler = new FinalAttackDepthsHandler(Scene);
        Scene.DeathScreen.OnDeath += () =>
        {
            silhoutteIntensity = 0f;
            feverAfterimageIntensity = 0f;
            Scene.BattleHandler.Attack = null;

            Scene.SoulLayer.ScreenShaders.Clear();
            Scene.BattleLayer.ScreenShaders.Clear();
        };
    }

    protected override void UpdateInternal()
    {
        if (!startingAnimationFinished)
        {
            float animationStartProgress = float.InverseLerp(0.25f, 1f, AttackTimer / stats.AnimationStartDuration);
            Scene.BattleBackBackdropOpacity = float.InverseLerp(0f, 0.7f, animationStartProgress);
            Battle.Box.Opacity = float.Pow(1f - Scene.BattleBackBackdropOpacity, 0.4f);
            Battle.Box.Size += Vector2.One * Scene.Time.DeltaTime * 300f;
        }

        if (AttackTimer >= stats.AnimationStartDuration || startingAnimationFinished)
        {
            if (!startingAnimationFinished)
            {
                startingAnimationFinished = true;
                activeSilhouetteColor = Color.Black;
                passiveSilhouetteColor = new Color(255, 0, 0);
                AttackTimer.Reset();
            }

            if (!depthsAreEmerging)
                AttackAnticipation();
            else if (!attacking)
                LetDepthsAppear();
            else if (!attackIsOver)
                Attack();
            else
                HandlePostAttackVisuals();

            ApplySilhouetteShader();

            for (int i = 0; i < daggers.Count; i++)
                activeDaggerAction(i, daggers[i]);

            // Convenient way of keeping the player from leaving the game bounds.
            Battle.Box.Position = NorthernlightGame.KnightFightResolution * 0.5f;
            Battle.Box.Size = NorthernlightGame.KnightFightResolution;
            Attacker.Opacity = AttackerOpacity;
        }
        else
            Attacker.EmitsAfterimages = false;
    }

    private void AttackAnticipation()
    {
        float knightTeleportProgress = float.InverseLerp(0f, 0.5f, AttackTimer);
        Vector2 knightTeleportStart = NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 0.6f + knightTeleportProgress * 0.05f);
        AttackerOpacity = knightTeleportProgress;
        Attacker.EmitsAfterimages = false;
        Attacker.RepositionDelay = 0.2f;
        if (stabDelay <= 0f)
            Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, Vector3.Zero, 6f, Scene.Time);

        float stabWaitTime = 1.25f;
        AtlasEntry anticipation = Atlases.KnightScene.KnightSwordThrowAnticipation;
        AtlasEntry swordThrow = Atlases.KnightScene.KnightSwordThrow;
        AtlasEntry ball = Atlases.KnightScene.KnightBallFormAnimation;
        if (knightTeleportProgress < 1f)
        {
            stabDelay = stabWaitTime;
            Attacker.Position = new Vector3(knightTeleportStart, Attacker.Position.Z);
            Attacker.SetTexture(anticipation, 5, loops: false);
            Attacker.LimitTextureFrame(0);
        }

        if (Attacker.RenderState.Texture == anticipation && Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
        {
            Scene.AddScreenShake(10f);

            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeThrowWindup.wav"), 1.6f));
            Attacker.SetTexture(swordThrow, 9, loops: false);

            for (int i = 0; i < 3; i++)
            {
                ThrownBlackDagger dagger = new ThrownBlackDagger(Scene, 0, 0f, Attacker.Position.AsVector2() + Vector2.UnitY * 100f, -Angle.PiOver2);
                Bullets.Add(dagger);
                daggers.Add(dagger);
            }
        }

        // Move the daggers above the Knight.
        float daggerAnticipationDelay = 0.9f;
        if (Attacker.RenderState.Texture == swordThrow && stabDelay > 0f)
        {
            stabDelay -= Scene.Time.DeltaTime;

            float ascentProgress = float.InverseLerp(stabWaitTime, 0f, stabDelay);
            Attacker.Velocity = -Vector3.UnitY * float.Bump(ascentProgress.Squared()).Squared() / stabWaitTime * 900f;
            activeDaggerAction = (i, d) => Blades_RiseAboveKnight(i, d, ascentProgress);

            Attacker.LimitTextureFrame(2);

            if (stabDelay <= 0f)
                daggerAnticipationCountdown = daggerAnticipationDelay;
        }

        // Sit in place, making the daggers rise up
        // slightly in anticipation of the throw.
        if (daggerAnticipationCountdown > 0f)
        {
            daggerAnticipationCountdown -= Scene.Time.DeltaTime;

            float throwWaitProgress = float.InverseLerp(daggerAnticipationDelay, 0f, daggerAnticipationCountdown);
            activeDaggerAction = (i, d) => Blades_Anticipate(i, d, throwWaitProgress);
            Attacker.LimitTextureFrame(2);
        }

        // Throw the blades down.
        if (Attacker.RenderState.Texture == swordThrow)
        {
            if (!threwBlades && Attacker.RenderState.CurrentFrame == 6)
            {
                throwTimer = 0f;
                activeDaggerAction = Blades_GetThrown;
                threwBlades = true;
                Scene.AddScreenShake(20f);
            }
            throwTimer += Scene.Time.DeltaTime;
        }

        if (AttackTimer >= 5f)
        {
            throwTimer = 0f;
            Bullets.Clear();
            daggers.Clear();
            AttackTimer.Reset();
            threwBlades = false;
        }
    }

    private Vector2 CalculateBladeOffset(int index, float ascentProgress, float anticipationProgress)
    {
        int centeredIndex = index - daggers.Count / 2;
        Vector2 idealOffset = Vector2.Lerp(Vector2.UnitY * 100f, Vector2.UnitY * -200f, ascentProgress);
        idealOffset.X += centeredIndex * ascentProgress.Squared() * 100f;

        float indexProgress = index / (float)(daggers.Count - 1f);
        float anticipation = float.InverseLerp(0.85f, 1f, indexProgress * 0.3f + anticipationProgress);
        idealOffset.Y -= anticipation.Squared() * 80f;

        return idealOffset;
    }

    private void Blades_RiseAboveKnight(int index, ThrownBlackDagger dagger, float ascentProgress)
    {
        int centeredIndex = index - daggers.Count / 2;

        Vector2 idealOffset = CalculateBladeOffset(index, ascentProgress, 0f);

        Vector2 idealPosition = Attacker.Position.AsVector2() + idealOffset;
        Vector2 idealVelocity = (idealPosition - dagger.Position) * 12f;
        dagger.Velocity = Vector2.LerpDt(dagger.Velocity, idealVelocity, 14.5f, Scene.Time);

        dagger.Rotation = Angle.Tau * ascentProgress * 3.25f;
        dagger.Rotation += centeredIndex * ascentProgress * 0.125f;
    }

    private void Blades_Anticipate(int index, ThrownBlackDagger dagger, float anticipationProgress)
    {
        int centeredIndex = index - daggers.Count / 2;
        dagger.Rotation = Angle.PiOver2 + centeredIndex * anticipationProgress * 0.1f;

        Vector2 idealOffset = CalculateBladeOffset(index, 1f, anticipationProgress);

        Vector2 idealPosition = Attacker.Position.AsVector2() + idealOffset;
        Vector2 idealVelocity = (idealPosition - dagger.Position) * 12f;
        dagger.Velocity = Vector2.LerpDt(dagger.Velocity, idealVelocity, 14.5f, Scene.Time);
    }

    private void Blades_GetThrown(int index, ThrownBlackDagger dagger)
    {
        float timeThreshold = index * 0.3f / daggers.Count;

        if (throwTimer >= timeThreshold)
        {
            dagger.Throw();
            for (int i = 0; i < daggers.Count; i++)
            {
                if (daggers[i].Position.Y >= NorthernlightGame.KnightFightResolution.Y + 150f)
                {
                    depthsAreEmerging = true;
                    daggers[i].Kill();
                    daggers.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    private void LetDepthsAppear()
    {
        float riseProgress = float.InverseLerp(1.4f, 4f, depthsHandler.ActiveTimer);

        Attacker.RepositionDelay = 0.2f;
        Attacker.EmitsAfterimages = false;

        if (riseProgress >= 0.9f)
        {
            Attacker.SilhouetteMode = true;
            Attacker.SetTexture(Atlases.KnightScene.KnightBallFormTransitionForward, 5, loops: false);
        }
        else
            Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, Vector3.Zero, 10f, Scene.Time);

        Vector2 ballVelocity = -Vector2.UnitY.RotatedBy(Scene.Rng.NextFloat(-0.33f, 0.33f)) * 300f;
        depthsHandler.CreateBall(NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 1.175f), ballVelocity);

        depthsHandler.Update();

        depthsHandler.SeaRiseHeight = float.Lerp(-100f, NorthernlightGame.KnightFightResolution.Y, riseProgress);

        silhoutteIntensity = float.InverseLerp(4f, 5.4f, depthsHandler.ActiveTimer).Squared();
        if (silhoutteIntensity >= 0.925f)
            depthsHandler.ClearBalls();

        if (depthsHandler.ActiveTimer >= 5.5f)
        {
            AttackTimer.Reset();
            attacking = true;
        }
    }

    private void Attack()
    {
        slashTimer ??= Scene.Time.Rent();

        Attacker.RepositionDelay = 0.2f;
        Attacker.EmitsAfterimages = false;

        float attackDuration = 24f;
        bool done = slashTimer >= attackDuration;
        if (done)
            Attacker.SetTexture(Atlases.KnightScene.KnightOpenEyeForward, 6, loops: false);
        else
            Attacker.SetTexture(Atlases.KnightScene.KnightOpenEyeForwardSlash, 6, loops: true);

        Vector3 idealPosition = new Vector3(TargetPosition - Vector2.UnitY * 432f + relativeSlashHoverOffset, Attacker.Position.Z);
        Vector3 idealVelocity = (idealPosition - Attacker.Position) * 8.1f;
        if (done)
            idealVelocity = -Vector3.UnitY * (slashTimer - attackDuration) * 700f;

        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealVelocity, 20f, Scene.Time);

        if (Attacker.JustSlashed && !done)
        {
            Attacker.SlashAnimationSpeedup = float.Clamp(Attacker.SlashAnimationSpeedup + 0.16f, 1f, 1f + stats.MaxKnightSlashSpeedup);

            float previousHoverOffsetX = relativeSlashHoverOffset.X;
            float hoverOffsetVariance = 720f;
            do
            {
                relativeSlashHoverOffset = Scene.Rng.NextCircular(hoverOffsetVariance * 0.5f) * new Vector2(1f, 0.1f);
            }
            while (float.Abs(relativeSlashHoverOffset.X - previousHoverOffsetX) < hoverOffsetVariance * 0.3f);

            Vector2 directionToTarget = (TargetPosition - Attacker.Position.AsVector2()).SafeNormalize(Vector2.Zero);
            Attacker.Velocity -= new Vector3(directionToTarget, 0f) * 1485f;

            Vector2 spawnPosition = Attacker.Position.AsVector2();
            for (int i = 0; i < 5; i++)
            {
                int starDamage = stats.StarDamage;
                float scale = Scene.Rng.NextFloat(1.7f, 2.1f);
                Vector2 direction = (float.Tau * i / 5f + Scene.Rng.NextFloat(Angle.Tau)).ToUnit();
                Vector2 velocity = direction * Scene.Rng.NextFloat(356f, 635f) / scale * stats.StarSpeedFactor;

                bool flyingAtTarget = Vector2.Dot(direction, directionToTarget) >= 0.84f;
                if (!flyingAtTarget && slashTimer < attackDuration - 4f)
                    Bullets.Add(new LargeStar(Scene, this, spawnPosition, velocity, starDamage, 0f, scale, 4.25f));
            }

            int waveDamage = stats.WaveDamage;
            Vector2 aimDirection = (TargetPosition - spawnPosition).SafeNormalize(Vector2.UnitY);
            spawnPosition += aimDirection * 100f;

            Bullets.Add(new Wave(Scene, this, spawnPosition, aimDirection * 100f, waveDamage, 0f)
            {
                ScaleFactor = 2f
            });

            Scene.AddScreenShake(10f);
        }

        float speedupBoost = Attacker.SlashAnimationSpeedup - 1f;
        Scene.Time.GlobalSpeed = 1f + speedupBoost * 0.645f;

        float feverWave = float.Cos01(Scene.Time.ElapsedTime * 20f);
        feverAfterimageIntensity = float.Clamp(speedupBoost * 1.3f, 0f, 1f) * feverWave * 0.96f;

        if (slashTimer >= attackDuration + 1.25f)
            attackIsOver = true;
    }

    private void HandlePostAttackVisuals()
    {
        Attacker.EmitsAfterimages = false;
        postAttackTimer ??= Scene.Time.Rent();

        Scene.BattleHandler.SoulMovementDisabled = true;
        Attacker.RepositionDelay = 1f;

        feverAfterimageIntensity = float.Cos01(Scene.Time.ElapsedTime * 32f) * 0.97f;

        float baseCameraZoom = 1f;
        if (PlayerFailedQTE)
        {
            Scene.MainCamera.Zoom = baseCameraZoom;
            Scene.initializedCamera = false;
            Scene.RoaringKnight.Position = Vector3.One * -999999f;
            foreach (Hero hero in Scene.Heroes.All)
                hero.HP = 0;
            Scene.DeathScreen.FailedFinalAttackQuickTimeEvent = true;
            Scene.Game.BlackCountdown = 0.2f;

            return;
        }

        int attackDirection = TargetPosition.X < NorthernlightGame.KnightFightResolution.X * 0.5f ? -1 : 1;

        float dashDelay = 0.16f;
        float dashDuration = 0.5f;
        float struggleDuration = 1.4f;
        float relativeTimer = postAttackTimer;
        Soul soul = Battle.Soul;
        Color startingActiveSilhouetteColor = Color.Black;
        Color startingPassiveSilhouetteColor = new Color(255, 0, 0);

        if (relativeTimer <= dashDelay)
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightBladeAttack, 1);
            Attacker.Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f - Vector2.UnitY * 9999f, 0f);
            Attacker.Velocity = Vector3.Zero;
            Attacker.Rotation = 0f;
            Scene.MainCamera.Zoom = float.LerpDt(Scene.MainCamera.Zoom, baseCameraZoom, 6f, Scene.Time);
            activeSilhouetteColor = startingActiveSilhouetteColor;
            passiveSilhouetteColor = startingPassiveSilhouetteColor;
            return;
        }

        if (!playedDashSound)
        {
            playedDashSound = true;
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), 0.75f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightDaggerDash.wav"), 0.8f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightAppearanceDash.wav"), 1.2f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightDaggerDash.wav")));
        }

        relativeTimer -= dashDelay;
        float dashProgress = float.InverseLerp(0f, dashDuration, relativeTimer);
        float zoomInProgress = float.InverseLerp(0.925f, 1f, dashProgress);
        if (zoomInProgress > 0f)
            Scene.CameraPanPosition = TargetPosition.AsVector3() + Vector3.UnitY * 75f;

        if (relativeTimer <= dashDuration + 0.6f)
            Scene.MainCamera.Zoom = baseCameraZoom * float.Smoothstep(1f, 1.75f, zoomInProgress);

        if (relativeTimer <= dashDuration)
        {
            KeepKnightLockedOnSoul(dashProgress, attackDirection);

            return;
        }

        relativeTimer -= dashDuration;
        float struggleProgress = float.InverseLerp(0f, struggleDuration, relativeTimer);
        bool done = quickTimeEventCounter >= QuickTimeEventCount - 1;
        if (!done || quickTimeEventImpactVisualsTimer <= 0.3f)
            KeepKnightLockedOnSoul(dashProgress, attackDirection);

        float colorSwapProgress = float.InverseLerp(0f, 0.1f, struggleProgress);
        passiveSilhouetteColor = Color.Lerp(startingPassiveSilhouetteColor, new Color(0, 0, 0), colorSwapProgress);
        activeSilhouetteColor = new Color(255, 0, 0);

        float destinationX = NorthernlightGame.KnightFightResolution.X * 0.5f + attackDirection * 100f;
        float resistance = Scene.Rng.NextFloat(-1f, 1f) * (150f + struggleProgress * 200f) * (1f - quickTimeEventImpactVisualsTimer).Cubed();
        soul.Position = Vector3.LerpDt(soul.Position, new Vector3(destinationX, soul.Position.Y, soul.Position.Z), 5f, Scene.Time);
        soul.Position += new Vector3(resistance, resistance * Scene.Rng.NextFloat(-1f, 1f) * 0.275f, 0f) * Scene.Time.DeltaTime;
        Attacker.Rotation += Scene.Rng.NextFloat(-1f, 1f) * Scene.Time.DeltaTime * 0.1f;

        float quickTimeEventProgress = float.InverseLerp(0f, QuickTimeEventCount - 1f, quickTimeEventCounter);
        float nextCuickTimeEventDuration = float.Lerp(2.2f, 0.5f, quickTimeEventProgress);
        if (struggleProgress >= 1f && quickTimeEventImpactVisualsTimer <= 0f)
        {
            if (quickTimeEventDuration <= 0f)
            {
                quickTimeEventDuration = nextCuickTimeEventDuration;
                quickTimeEventCountdown = quickTimeEventDuration;
            }

            quickTimeEventCountdown -= Scene.Time.DeltaTime;

            bool failed = false;
            if (quickTimeEventCountdown <= -0.45f)
                failed = true;

            bool readyForInputs = quickTimeEventCountdown <= quickTimeEventDuration * 0.5f;
            if (readyForInputs && ActiveKeymap.Confirm.WasJustPressed)
            {
                bool success = quickTimeEventCountdown <= quickTimeEventDuration * 0.24f;
                if (success)
                {
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MetalHit.wav"), 1.1f));
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightRadialSplit.wav")));
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), 1.1f));

                    int variant = Scene.Rng.Next(2);
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav($"KnightHurt{variant + 1}.wav"), 0.9f));

                    quickTimeEventImpactVisualsTimer = 0.001f;
                    if (quickTimeEventCounter >= QuickTimeEventCount - 1)
                        Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SoulShine.wav"), 1.67f));

                    Scene.AddScreenShake(32f);
                }
                else
                    failed = true;
            }

            if (failed)
            {
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SoulInjureReverb.wav"), 1.2f));
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SliceTelegraph.wav"), 0.9f));
                PlayerFailedQTE = true;
            }

            quickTimeEventsOngoing = true;
        }

        HandleMusicReplacement(struggleProgress * 0.97f, done);

        if (quickTimeEventImpactVisualsTimer > 0f)
        {
            knightPushBackDistance += Scene.Time.DeltaTime * (1f - quickTimeEventImpactVisualsTimer).Squared() * 3200f;

            float backgroundBrightness = float.Bump(quickTimeEventImpactVisualsTimer) * quickTimeEventProgress * 0.6f;
            if (done)
            {
                backgroundBrightness = 0f;

                float blastSpeed = float.InverseLerp(0.61f, 1f, quickTimeEventImpactVisualsTimer) * 2400f;
                if (blastSpeed >= 3f)
                {
                    Attacker.Velocity = (TargetPosition - Attacker.Position.AsVector2()).AsVector3() * Scene.Time.DeltaTime * -blastSpeed;
                    Attacker.SetTexture(Atlases.KnightScene.KnightSlashFlurryAnticipation, 1);

                    if (!playedRoarSound)
                    {
                        playedRoarSound = true;

                        Sound rawr = new Sound(AudioBuffer.FromWav("KnightRoar.wav"), 2.8f);
                        Scene.Audio.Play(rawr);
                    }
                }
                else
                    Attacker.Velocity = Vector3.Zero;
            }

            passiveSilhouetteColor = Color.Lerp(passiveSilhouetteColor, Color.White, backgroundBrightness);

            float updateSpeed = done ? 0.54f : 1.5f;
            int effectiveSoulFrame = (int)float.Round(quickTimeEventImpactVisualsTimer * 18f);
            if (done && effectiveSoulFrame == 10)
                updateSpeed *= 0.333f;

            quickTimeEventImpactVisualsTimer = float.Clamp(quickTimeEventImpactVisualsTimer + Scene.Time.DeltaTime * updateSpeed, 0f, 1f);
            if (quickTimeEventImpactVisualsTimer >= 1f && !done)
            {
                quickTimeEventCounter++;
                quickTimeEventImpactVisualsTimer = 0f;
                quickTimeEventDuration = nextCuickTimeEventDuration;
                quickTimeEventCountdown = quickTimeEventDuration;
            }

            if (quickTimeEventImpactVisualsTimer >= 1f && done && attackFinishTimer <= 0f)
            {
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("ReverseCymbal.wav")));
                attackFinishTimer = 0.0001f;
            }
        }

        knightPushBackDistance = float.LerpDt(knightPushBackDistance, 0f, 17.5f, Scene.Time);

        if (attackFinishTimer > 0f)
        {
            attackFinishTimer += Scene.Time.DeltaTime;

            float finishProgress = float.InverseLerp(0f, 5f, attackFinishTimer);
            passiveSilhouetteColor = Color.Lerp(passiveSilhouetteColor, Color.White, finishProgress);
            if (finishProgress >= 1f)
            {
                End();
                Battle.CurrentState = BattleHandler.State.WaitingForDialogue;
            }
        }
    }

    private void HandleMusicReplacement(float replacementInterpolant, bool done)
    {
        if (Scene.Music.Tracks.Length <= 0)
        {
            if (done)
                Scene.Music.Stop();

            return;
        }

        if (Scene.Music.Tracks.Length < 2)
            Scene.Music.Add(new Sound(AudioBuffer.FromWav("Heartbeat.wav")));

        float globalFadeout = 1f;
        if (done)
            globalFadeout = 1f - quickTimeEventImpactVisualsTimer;

        Sound blackKnife = Scene.Music.Tracks[0];
        Sound heartbeat = Scene.Music.Tracks[1];

        // The acoustic energy of a given sound is proportional
        // to the square of its amplitude.
        // Therefore, in order to maintain consistent energy across
        // the entire crossfade, without any dips near
        // the 0.5 midpoint, the following constraint must be
        // satified:

        // x^2 + y^2 = c, where x and y correspond to the outputs
        // of the two aural layers.

        // Fortunately, sinusoids are especially well equipped
        // for this problem, since they have the innate property
        // of sin(x)^2 + cos(x)^2 = 1.

        // Beyond that, the choice of pi/2 as a coefficient
        // exists to satisfy boundary conditions, whereupon
        // we want the following conditions to be true:

        // 1. x(0) = 0
        // 2. x(1) = 1
        // 3. y(0) = 1
        // 4. y(1) = 0
        // 5. Monotonicity across the range. (This eliminates the myriad
        // solutions involving coefficients with a cyclic 2pi * n added)
        blackKnife.Stream.Volume = float.Cos(Angle.PiOver2 * replacementInterpolant) * globalFadeout;
        heartbeat.Stream.Volume = float.Sin(Angle.PiOver2 * replacementInterpolant) * globalFadeout * 3f;

        if (done && globalFadeout <= 0.1f)
            Scene.Music.Stop();
    }

    public override void OnEnd()
    {
        silhoutteIntensity = 0f;

        Attacker.Velocity = Vector3.Zero;
        Attacker.Position = Attacker.SpawnPosition;
        Attacker.HorizontallyFlip = false;
        Attacker.FireUpGlow = 0f;
        Attacker.SilhouetteMode = false;
        Attacker.Rotation = 0f;
        Battle.screenWhitening = 1f;
        Battle.BattleIsDoneSideA = true;
        Scene.BattleBackBackdropOpacity = 0f;
        Scene.MainCamera.Zoom = 1f;
        Scene.initializedCamera = false;

        // OK Susie we are NOT having you stuck on the ground
        // you're supposed to go and kill the Knight this is your moment
        // sis you are not going to ruin it for yourself.
        Scene.Heroes.Susie.HP = int.Max(1, Scene.Heroes.Susie.HP);

        // Make the battle menu go away, we're done with it
        // now.
        Scene.battleMenu.FadeOutTimer = 1f;
    }

    private void KeepKnightLockedOnSoul(float dashProgress, int attackDirection)
    {
        Vector3 forward = Vector2.UnitX.RotatedBy(Attacker.Rotation).AsVector3();
        Vector3 down = Vector2.UnitY.RotatedBy(Attacker.Rotation).AsVector3();

        Vector3 offset = forward * float.Smoothstep(785f, 97f + knightPushBackDistance, float.Pow(dashProgress, 4.5f)) * attackDirection;
        offset += down * 30f;

        Attacker.Position = new Vector3(TargetPosition, Attacker.SpawnPosition.Z) + offset;
        Attacker.HorizontallyFlip = attackDirection == -1;
    }

    private void ApplySilhouetteShader()
    {
        depthsHandler.OverlayOpacity = float.InverseLerp(0.54f, 0f, silhoutteIntensity);
        if (silhoutteIntensity <= 0f)
            return;

        if (shaderPreparations is null)
            shaderPreparations ??= new SpriteRenderer.ShaderWithPreparations(Shaders.AwesomeSilhouetteShader, ApplyShaderPreparations);

        Scene.SoulLayer.ScreenShaders.Activate(shaderPreparations);
        Scene.BattleLayer.ScreenShaders.Activate(shaderPreparations);

        previousTargetContents = Scene.BattleLayer.ScreenShaders.AltScreenTarget.Texture;
    }

    private static void ApplyShaderPreparations(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            ActiveSilhouetteColor = activeSilhouetteColor.AsVector4(),
            PassiveSilhouetteColor = passiveSilhouetteColor.AsVector4(),
            Intensity = new Vector4(silhoutteIntensity, feverAfterimageIntensity * 0.5f, 0f, 0f),
        });

        if (previousTargetContents is not null)
            pass.BindFragmentSamplerTextures(1, new RenderPass.SamplerTexture(frame.Device.PointWrap, previousTargetContents));
    }

    private void RenderQuickTimeEventUI(Renderer renderer)
    {
        bool done = quickTimeEventCounter >= QuickTimeEventCount - 1;
        float opacity = 1f;
        if (done)
            opacity = float.InverseLerp(0.1f, 0f, quickTimeEventImpactVisualsTimer);

        float normalizedTime = float.InverseLerp(quickTimeEventDuration, 0f, quickTimeEventCountdown);
        normalizedTime *= float.Smoothstep(float.InverseLerp(0.85f, 0f, quickTimeEventImpactVisualsTimer));

        float scale = 1.5f;
        AtlasEntry box = Atlases.KnightScene.AttackBoxInner;
        AtlasEntry boxOuter = Atlases.KnightScene.AttackBoxOuter;
        AtlasEntry pressIndicator = Atlases.KnightScene.AttackPressIndicator;
        AtlasEntry reticleCurrent = Atlases.KnightScene.AttackBarReticleThingGlow;
        AtlasEntry reticleIdeal = Atlases.KnightScene.AttackBarReticleThing;

        Vector2 shake = Scene.Rng.NextCircular(float.Bump(quickTimeEventImpactVisualsTimer.Squared()).Squared() * 20.5f);
        Vector3 drawPosition = new Vector3(Scene.MainCamera.Position.AsVector2() + shake + Vector2.UnitY * 160f, 400f);
        if (Scene.MainCamera is RestrictedOrthographicCamera c)
        {
            drawPosition += new Vector3(c.Width, c.Height, 0f) / Scene.MainCamera.Zoom * 0.5f;
            drawPosition.X += (c.EffectiveWidth - c.Width) * 0.25f;
        }

        SpriteRenderer sprites = renderer.Get<SpriteRenderer>();

        for (int i = 0; i < 3; i++)
        {
            float outerBoxPulse = (Scene.Time.ElapsedTime * 1.9f + i * 0.5f) % 1f;
            float outerBoxScale = (1f + outerBoxPulse * 0.33f) * 2f;
            sprites.Enqueue(boxOuter, new SpriteData(boxOuter)
            {
                Position = drawPosition,
                Tint = Color.White * (1f - outerBoxPulse).Squared() * opacity * 0.8f,
                Scale = Vector2.One * scale * outerBoxScale,
            });
        }

        sprites.Enqueue(box, new SpriteData(box)
        {
            Position = drawPosition,
            Tint = Color.White * opacity,
            Scale = Vector2.One * scale * 2f,
        }, BlendState.Additive);
        sprites.Enqueue(pressIndicator, new SpriteData(pressIndicator)
        {
            Position = drawPosition + new Vector3(-36f, -6f, 0f) * scale * 2f + Vector3.UnitZ * 8f,
            Tint = Color.White * opacity * 0.9f,
            Scale = Vector2.One * scale,
        });

        Vector3 endingReticlePosition = drawPosition + new Vector3(56f, 0f, 0f) * scale * 2f + Vector3.UnitZ * 8f;
        sprites.Enqueue(reticleIdeal, new SpriteData(reticleIdeal)
        {
            Position = endingReticlePosition,
            Tint = new Color(0, 148, 255) * opacity * 0.99f,
            Scale = Vector2.One * scale * new Vector2(1f, 1.57f),
        });
        sprites.Enqueue(reticleCurrent, new SpriteData(reticleCurrent)
        {
            Position = endingReticlePosition + new Vector3((1f - normalizedTime) * scale * -150f, 0f, 50f),
            Tint = Color.White * opacity * 0.99f,
            Scale = Vector2.One * scale * new Vector2(1f, 1.56f),
        }, BlendState.Additive);
    }

    private void RenderSoulGlowVisuals(Renderer renderer, int frameLimitY, float glowProgress)
    {
        int totalHorizontalFrames = 4;
        int totalVerticalFrames = 5;
        int frame = (int)float.Round(glowProgress * frameLimitY);
        AtlasEntry glow = Atlases.LargeTextures.SoulShine;
        Vector3 drawPosition = new Vector3(TargetPosition - Vector2.UnitY * 20f, 400f);
        if (Scene.MainCamera is RestrictedOrthographicCamera c)
        {
            Vector3 baseDisplacement = new Vector3(c.Width - c.EffectiveWidth, c.Height - c.EffectiveHeight, 0f) * 0.5f;
            Vector3 displacementFactor = new Vector3(baseDisplacement.X / c.EffectiveWidth, baseDisplacement.Y / c.EffectiveHeight, 0f);
            Vector3 displacement = new Vector3(c.Width, 0f, 1f) * displacementFactor / c.Zoom;
            drawPosition -= displacement;
        }

        Vector2 baseSize = new Vector2(glow.Area.Width / totalHorizontalFrames - 2, glow.Area.Height / totalVerticalFrames - 2);
        Vector2 scale = NorthernlightGame.KnightFightResolution / Scene.MainCamera.Zoom / baseSize * 1.5f;

        renderer.Get<SpriteRenderer>().Enqueue(glow, new SpriteData(glow, frame / totalVerticalFrames, frame % totalVerticalFrames, totalHorizontalFrames, totalVerticalFrames)
        {
            Position = drawPosition,
            Tint = Color.White,
            Scale = scale
        });
    }

    public override void ExtraPrepareRender_SoulLayer(SpriteRenderer sprites, FrameContext context)
    {
        // Ensure that the Knight renders in front
        // of the darkening visual once they're
        // ready.
        if (startingAnimationFinished)
        {
            Attacker.FireUpGlow = 0f;
            Attacker.PrepareRender(sprites);
        }

        if (depthsAreEmerging)
        {
            depthsHandler.Render(sprites);
            foreach (Bullet b in Bullets)
                b.PrepareRender(sprites, context);
        }

        if (quickTimeEventsOngoing)
            Scene.SoulLayer.PostDrawActions.Enqueue(RenderQuickTimeEventUI);
        if (quickTimeEventImpactVisualsTimer > 0f)
        {
            bool done = quickTimeEventCounter >= QuickTimeEventCount - 1;
            Scene.SoulLayer.PostDrawActions.Enqueue(r => RenderSoulGlowVisuals(r, done ? 18 : 10, quickTimeEventImpactVisualsTimer));
        }
    }
}
