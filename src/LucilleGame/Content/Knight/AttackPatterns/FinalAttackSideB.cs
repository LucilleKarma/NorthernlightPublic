using System.Numerics;
using Northernlight.Content.Borders;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Content.Knight.Particles;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class FinalAttackSideB : Attack<RoaringKnight>
{
    public enum Substate
    {
        SpecialImage,
        Ascent,
        KnightTeleportsAway,
        FeverishDesperationTelegraph,
        FeverishDesperation,
        KnightFlyAnimation,
        PushKnightBack,
        ThisTimeItsTheHeartThatPierces
    }

    public readonly record struct Stats(float AttackDuration, float ImageDuration, float AscentSwordGapSize, float AscentDuration, int AscentSwordDamage, int DaggerDamage, int TotalTurnArounds, float SpinSpeedFactor, float AscentSwordGrazePoints,
        int SlashDamage, float SlashGrazePoints, float SwordWallWaveFrequency, float SwordWallAmplitude, float DaggerSummonDelay, float DaggerSummonRateSpeedupFactor, int DaggerWaveCycleRate, Vector2 BoxSize);

    private int ascentDaggerShotCounter;

    private int initialSpinDirection;

    private int spinTurnAroundCounter;

    private bool shatteredScreen;

    private bool playedFlyDashSound;

    private bool initializedDesperationTelegraphStuff;

    private bool initializedDeperationSpinStuff;

    private bool createdSoulDashVisuals;

    private bool createdSoulImpactVisuals;

    private bool quickTimeEventsOngoing;

    private bool attackIsOver;

    private float musicVolumeFactor;

    private float specialAnimationProgress;

    private float ascentWindTimer;

    private float ascentWindIntensity;

    private float vignetteIntensity;

    private float movingSpinSpeed;

    private float idealSpinSpeed;

    private float cameraZoom;

    private float soulShineIntensity;

    private float quickTimeEventCountdown;

    private float quickTimeEventDuration;

    private float quickTimeEventImpactVisualsTimer;

    private Vector2 windDirection = -Vector2.UnitY;

    private Vector2 spinOrigin;

    private Vector2 knightPushOrigin;

    private Vector2 soulDashOrigin;

    private Substate currentSubstate;

    private Sound? staticSound;

    private OwnedTimer ascentSwordWallTimer;

    private OwnedTimer ascentBladefallTimer;

    private OwnedTimer teleportAfterimageTimer;

    private OwnedTimer desperationTimer;

    private OwnedTimer switchSpinDirectionTimer;

    private OwnedTimer attackEndTimer;

    private RedFlurryTelegraph telegraph;

    private Vector2 spinRotation;

    private readonly Stats stats;

    public Vector2 SoulAimPosition => Attacker.Position.AsVector2() - Vector2.UnitY * 30f;

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override bool SkipsBoxAppearance => false;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextFinalAttackSideB;

    public FinalAttackSideB(BattleHandler battle, Stats stats) : base(battle) => this.stats = stats;

    public override void OnStart() => Scene.SpecialImageLayer.Preparations += RenderSpecialLayer;

    public override void OnEnd()
    {
        Scene.SpecialImageLayer.Preparations -= RenderSpecialLayer;

        Scene.MainCamera.Zoom = 1f;
        Scene.initializedCamera = false;

        Attacker.Velocity = Vector3.Zero;
        Attacker.Position = Attacker.SpawnPosition;

        // Get up.
        foreach (Hero hero in Scene.Heroes.All)
            hero.HP = int.Max(1, hero.HP);

        // Make the battle menu go away, we're done with it
        // now.
        Scene.battleMenu.FadeOutTimer = 1f;

        Battle.BattleIsDoneSideB = true;
    }

    protected override void UpdateInternal()
    {
        switch (currentSubstate)
        {
            case Substate.SpecialImage:
                SpecialImage();
                break;
            case Substate.Ascent:
                Ascent();
                break;
            case Substate.KnightTeleportsAway:
                KnightTeleportsAway();
                break;
            case Substate.FeverishDesperationTelegraph:
                FeverishDesperationTelegraph();
                break;
            case Substate.FeverishDesperation:
                FeverishDesperation();
                break;
            case Substate.KnightFlyAnimation:
                KnightFlyAnimation();
                break;
            case Substate.PushKnightBack:
                PushKnightBack();
                break;
            case Substate.ThisTimeItsTheHeartThatPierces:
                ThisTimeItsTheHeartThatPierces();
                break;
        }

        if (currentSubstate != Substate.SpecialImage)
        {
            KnightBorder.Instance.BiasToRedInterpolant = 1f;
            KnightBorder.Instance.ShineIntensityFactor = 3f;
            KnightBorder.Instance.TwinkleSpeedupFactor = 2f;
        }

        float windSpeed = float.Lerp(0.3f, 1f, float.Pow(ascentWindIntensity, 0.25f));
        ascentWindTimer += Scene.Time.DeltaTime * windSpeed;

        Attacker.RepositionDelay = 0.25f;

        // Convenient way of keeping the player from leaving the game bounds.
        Battle.Box.Size = NorthernlightGame.KnightFightResolution;
        Battle.Box.Position = Battle.Box.Size * 0.5f;

        Scene.Music.Player.Volume = Scene.Game.RemapVolume(musicVolumeFactor, music: true);

        Scene.MainCamera.Zoom = 1f + cameraZoom;
        if (cameraZoom > 0f)
            Scene.CameraPanPosition = Vector3.LerpDt(Scene.CameraPanPosition, TargetPosition.AsVector3(), 50f, Scene.Time);
    }

    private void SpecialImage()
    {
        Scene.UILayer.ForceDeferment();
        Scene.SoulLayer.ForceDeferment();
        Scene.BattleLayer.ForceDeferment();
        Scene.DefaultLayer.ForceDeferment();
        Scene.SpecialImageLayer.ForceDeferment();
        Scene.ShatterLayer.ForceDeferment();

        specialAnimationProgress = float.InverseLerp(0f, 2.4f, AttackTimer);

        if (shatteredScreen)
        {
            ascentSwordWallTimer = Scene.Time.Rent();
            ascentBladefallTimer = Scene.Time.Rent();

            currentSubstate = Substate.Ascent;
            ascentDaggerShotCounter = 0;
            specialAnimationProgress = 0f;
            musicVolumeFactor = 1f;
            AttackTimer.Reset();
        }
    }

    private void Ascent()
    {
        float ascentProgress = float.InverseLerp(0f, stats.AscentDuration, AttackTimer);

        ascentWindIntensity = float.InverseLerp(0f, 2.5f, AttackTimer);
        Scene.BattleBackBackdropOpacity = 1f;
        Battle.Box.Opacity = 0f;

        float swordFlySpeed = float.Smoothstep(0.3f, 1f, ascentWindIntensity) * 900f;
        Vector2 swordMoveDirection = Vector2.UnitY;
        Vector2 resolution = NorthernlightGame.KnightFightResolution;
        foreach (Bullet b in Bullets)
        {
            if (b is BlackSword sword)
            {
                float acceleration = 1f + sword.ExistenceTimer * 1.1f;
                sword.PreviousPosition = sword.Position;
                sword.Position += swordMoveDirection * swordFlySpeed * acceleration * Scene.Time.DeltaTime * Scene.Rng.NextFloat(0.9f, 1f);
            }
        }

        // Calculate a sine wave with mildly fractalline bumps on it
        // as a baseline for the horizontal offset of the gap opening.
        float x = AttackTimer * stats.SwordWallWaveFrequency;
        float wave = float.Sin(x);
        for (int i = 0; i < 3; i++)
        {
            float intermediateCoefficient = float.Exp(i);
            wave += float.Sin(intermediateCoefficient * x) / intermediateCoefficient * 0.16f;
        }

        Vector2 tunnelOrigin = resolution * new Vector2(0.5f, -0.06f);
        tunnelOrigin.X += wave * resolution.X * stats.SwordWallAmplitude;
        if (ascentSwordWallTimer.Cycle(0.09f + (1f - ascentWindIntensity) * 0.3f))
            SummonSwordWaveInstance(tunnelOrigin, swordMoveDirection);

        bool spawnDaggerWave = ascentDaggerShotCounter % stats.DaggerWaveCycleRate == stats.DaggerWaveCycleRate - 1;
        float bladeSpawnRate = spawnDaggerWave ? 0.7f : 0.12f;
        bladeSpawnRate += (1f - ascentWindIntensity) * 2f;
        bladeSpawnRate /= stats.DaggerSummonRateSpeedupFactor;

        // Keep the bladefall timer in stasis until
        // ready.
        if (AttackTimer <= stats.DaggerSummonDelay)
            ascentBladefallTimer.Reset();

        if (ascentBladefallTimer.Cycle(bladeSpawnRate))
        {
            int daggerDamage = stats.DaggerDamage;
            float daggerGrazePoints = 4f;
            float daggerTelegraphDuration = spawnDaggerWave ? 0.45f : 0.25f;
            if (spawnDaggerWave)
            {
                Vector2 aimDirection = Vector2.UnitY;

                // Determine the Y position based on the
                // position of the soul.
                // In the interest of visual clarity, it appears
                // a bit below the top of the screen by default, but
                // if the soul moves up to meet that, then the
                // blades move up accordingly, so that the attack
                // can't be cheesed by flying above the invisible
                // spawn position line.
                float y = float.Min(resolution.Y * 0.25f, TargetPosition.Y - 200f);

                // But also make sure that daggers don't appear
                // above the game's screen. That would have
                // horrible visibility implications.
                if (y < 54f)
                    y = 54f;

                for (int i = -2; i <= 2; i++)
                {
                    Vector2 daggerSpawnPosition = new Vector2(TargetPosition.X + i * 132f, y);
                    Bullets.Add(new BlackDagger(this, Scene, daggerDamage, daggerGrazePoints, daggerSpawnPosition, aimDirection, 200f, telegraphDuration: daggerTelegraphDuration)
                    {
                        Scale = 0.85f,
                        CanPlayDashSound = i == 0,
                        IncludeWhiteLine = false
                    });
                }

                // Guarantee a cooldown before the next dagger.
                ascentBladefallTimer.Set(-0.6f / stats.DaggerSummonRateSpeedupFactor);
            }
            else
            {
                Vector2 aimDirection = Vector2.UnitX * (Scene.Rng.NextBool() ? -1f : 1f);
                Vector2 daggerSpawnPosition = TargetPosition - aimDirection * 150f;
                Bullets.Add(new BlackDagger(this, Scene, daggerDamage, daggerGrazePoints, daggerSpawnPosition, aimDirection, 95f, telegraphDuration: daggerTelegraphDuration)
                {
                    Scale = 0.7f,
                    IncludeWhiteLine = false
                });
            }

            ascentDaggerShotCounter++;
        }

        LimitBulletQuantity(3700);

        Vector3 knightAbsolutePosition = new Vector3(tunnelOrigin.X, tunnelOrigin.Y, Attacker.Position.Z);
        knightAbsolutePosition.X = float.Lerp(knightAbsolutePosition.X, resolution.X * 0.5f, 0.4f);
        knightAbsolutePosition.Y += 185f;
        knightAbsolutePosition.Y += float.Sin(AttackTimer * 3f) * 16f;

        float verticalRetreat = float.InverseLerp(resolution.Y, resolution.Y * 0.15f, TargetPosition.Y) * 80f;
        knightAbsolutePosition.Y -= verticalRetreat;

        Vector3 knightRelativePosition = knightAbsolutePosition - Vector3.UnitY * float.InverseLerp(0.75f, 0f, ascentProgress) * 5000f;
        Attacker.Position = knightRelativePosition;
        Attacker.SetTexture(Atlases.KnightScene.KnightOpenEyeForward, 6, loops: false);

        if (ascentProgress >= 1f)
        {
            currentSubstate = Substate.KnightTeleportsAway;
            teleportAfterimageTimer = Scene.Time.Rent();
            AttackTimer.Reset();
        }
    }

    private void KnightTeleportsAway()
    {
        float teleportProgress = float.InverseLerp(0f, 0.9f, AttackTimer);
        int maxBullets = (int)((1f - teleportProgress).Squared() * 3700f);
        LimitBulletQuantity(maxBullets);

        int afterimageCountPerSecond = 24;
        float afterimageLifetime = 1.5f;
        float afterimageTravelDistance = float.Lerp(100f, 500f, float.Sqrt(teleportProgress));

        Attacker.Opacity = (1f - teleportProgress).Cubed();
        Attacker.Velocity = Vector3.Zero;
        Attacker.EmitsAfterimages = false;

        if (teleportProgress < 0.8f && teleportAfterimageTimer.Cycle(afterimageLifetime / afterimageCountPerSecond))
        {
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightVanish.wav"), 0.4f));

            Attacker.Afterimages.Add(new KnightAfterimage()
            {
                RenderState = Attacker.RenderState,
                Lifetime = afterimageLifetime,
                Position = Attacker.Position + Vector3.UnitZ * (Attacker.Afterimages.Count * 2f + 10f),
                HorizontallyFlip = Attacker.HorizontallyFlip,
                Scale = Vector2.One * Attacker.Scale,
                Velocity = Vector3.UnitX * (Attacker.HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / afterimageLifetime * (1f - teleportProgress)
            });

            Attacker.Afterimages.Add(new KnightAfterimage()
            {
                RenderState = Attacker.RenderState,
                Lifetime = afterimageLifetime,
                Position = Attacker.Position + Vector3.UnitZ * (Attacker.Afterimages.Count * 2f + 10f),
                HorizontallyFlip = Attacker.HorizontallyFlip,
                Scale = Vector2.One * Attacker.Scale,
                Velocity = Vector3.UnitX * (Attacker.HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / afterimageLifetime * -(1f - teleportProgress)
            });
        }

        vignetteIntensity = teleportProgress.Squared();
        ascentWindIntensity = (1f - vignetteIntensity).Squared();

        if (AttackTimer >= 1.5f)
        {
            initializedDesperationTelegraphStuff = false;
            currentSubstate = Substate.FeverishDesperationTelegraph;
            AttackTimer.Reset();
        }
    }

    private void FeverishDesperationTelegraph()
    {
        float telegraphDuration = 1f;

        Attacker.EmitsAfterimages = false;

        if (!initializedDesperationTelegraphStuff)
        {
            // The game crashes without this.
            RedFlurrySlash.InitializeBoxAndScene(Battle.Box, Scene);

            Vector2 resolution = NorthernlightGame.KnightFightResolution;
            spinOrigin = Vector2.Clamp(TargetPosition, resolution * 0.25f, resolution * 0.75f);

            initializedDesperationTelegraphStuff = true;

            initialSpinDirection = Scene.Rng.NextBool() ? -1 : 1;
            Vector2 startingDirection = Scene.Rng.NextCircularEdge(1f);
            Angle idleTelegraphAngularVelocity = Angle.Pi * 0.2f;
            telegraph = new RedFlurryTelegraph(this, Scene, spinOrigin, startingDirection, initialSpinDirection, telegraphDuration, idleTelegraphAngularVelocity)
            {
                IgnoreBoxBounds = true
            };
            Bullets.Add(telegraph);

            Scene.AddScreenShake(7.5f);
        }

        if (AttackTimer >= telegraphDuration)
        {
            currentSubstate = Substate.FeverishDesperation;
            initializedDeperationSpinStuff = false;
            spinTurnAroundCounter = 0;
            AttackTimer.Reset();
            desperationTimer = Scene.Time.Rent();
            switchSpinDirectionTimer = Scene.Time.Rent(ignoreGlobalSpeed: true);
        }
    }

    private void FeverishDesperation()
    {
        float attackWindup = float.InverseLerp(0f, 5f, AttackTimer);
        Scene.Time.GlobalSpeed = 1f + attackWindup * 0.9f;

        Attacker.EmitsAfterimages = false;

        if (!initializedDeperationSpinStuff)
        {
            initializedDeperationSpinStuff = true;

            desperationTimer.Reset();
            switchSpinDirectionTimer.Reset();
            spinRotation = telegraph.AimDirection;

            movingSpinSpeed = Angle.Pi * initialSpinDirection * stats.SpinSpeedFactor * 0.72f;
            idealSpinSpeed = movingSpinSpeed;

            Scene.AddScreenShake(24f);
        }

        float spinTurnAroundDuration = 4f;
        if (switchSpinDirectionTimer.Cycle(spinTurnAroundDuration))
        {
            idealSpinSpeed *= -1f;
            spinTurnAroundCounter++;

            if (spinTurnAroundCounter >= stats.TotalTurnArounds + 1)
            {
                currentSubstate = Substate.KnightFlyAnimation;
                AttackTimer.Reset();
                return;
            }
        }

        windDirection = Vector2.UnitX;
        ascentWindIntensity = float.LerpDt(ascentWindIntensity, 0.1f, 1.6f, Scene.Time);

        movingSpinSpeed = float.LerpDt(movingSpinSpeed, idealSpinSpeed, 1.4f, Scene.Time);
        spinRotation = spinRotation.RotatedBy(movingSpinSpeed * Scene.Time.DeltaTime);

        bool turningAround = float.Abs(idealSpinSpeed - movingSpinSpeed) >= 2f;
        float sliceRate = 0.09f;
        if (turningAround)
            sliceRate *= 2f;

        spinOrigin.X += float.Sin(float.Tau * AttackTimer / 8f) * Scene.Time.DeltaTime * 40f;

        if (desperationTimer.Cycle(sliceRate))
        {
            Scene.AddScreenShake(2f);

            Sound sound = new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), 0.7f);
            sound.Stream.FrequencyRatio = 1f + float.Bump(switchSpinDirectionTimer / spinTurnAroundDuration) * 0.5f;

            Scene.Audio.Play(sound);
            Bullets.Add(new RedFlurrySlash(this, Scene, spinOrigin, spinRotation, turningAround ? 0 : stats.SlashDamage, stats.SlashGrazePoints, sliceRate * 3f)
            {
                IgnoreBoxBounds = true
            });

            Vector2 forward = spinRotation;
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0)
                    continue;

                Vector2 spawnPosition = spinOrigin + forward * i * 200f;
                for (int j = 0; j < 4; j++)
                {
                    Vector2 velocity = forward.RotatedBy(Scene.Rng.NextFloat(-0.7f, 0.7f)) * i * Scene.Rng.NextFloat(300f, 590f);
                    Bullets.Add(new RedFlurryImpactParticle(Scene, 0.45f, spawnPosition, velocity));
                }
            }

            float shockwaveSpawnChance = float.Clamp(spinTurnAroundCounter / (float)stats.TotalTurnArounds, 0f, 1f) * 0.85f;
            if (Scene.Rng.NextBool(shockwaveSpawnChance))
            {
                float maxScale = float.Lerp(105f, 300f, float.Pow(Scene.Rng.NextFloat(), 0.65f));
                Vector2 shockwaveSpawnPosition = spinOrigin + Scene.Rng.NextCircular(540f);
                Bullets.Add(new ShockwaveParticle(Scene, 0.5f, maxScale, shockwaveSpawnPosition)
                {
                    BaseColor = new Color(199, 0, 0)
                });
            }

            spinOrigin += Scene.Rng.NextCircular(11f);
        }
    }

    private void KnightFlyAnimation()
    {
        float flyDelay = 0.6f;
        float flyAnimationDuration = 0.6f;
        float flyProgress = float.InverseLerp(0f, flyAnimationDuration, AttackTimer - flyDelay);

        if (!playedFlyDashSound && flyProgress > 0f)
        {
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightAppearanceDash.wav"), 1.2f));
            playedFlyDashSound = true;
        }

        Attacker.SetTexture(Atlases.KnightScene.KnightFlyingAnimationFast, 4);
        Attacker.Opacity = 1f;
        Attacker.AfterimageCountPerSecondOverride = 2;
        ascentWindIntensity = float.LerpDt(ascentWindIntensity, flyProgress * 1.3f + 0.2f, 12f, Scene.Time);

        Vector2 knightPosition = TargetPosition + new Vector2(60f, -4f);
        knightPosition.X += float.Smoothstep(1f - flyProgress) * 2000f;

        Attacker.Position = new Vector3(knightPosition, Scene.BattleHandler.Soul.Position.Z + 20f);
        Battle.SoulMovementDisabled = flyProgress > 0f;

        if (AttackTimer >= flyDelay + flyAnimationDuration + 1f)
        {
            currentSubstate = Substate.PushKnightBack;
            knightPushOrigin = Attacker.Position.AsVector2();

            int variant = Scene.Rng.Next(2);
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav($"KnightHurt{variant + 1}.wav"), 0.8f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SoulShine.wav"), 1.67f));

            AttackTimer.Reset();
        }

        cameraZoom = flyProgress * 0.1f;
    }

    private void PushKnightBack()
    {
        float shineDuration = 1.1f;
        float stateTransitionDelay = 2f;
        soulShineIntensity = float.InverseLerp(0f, shineDuration, AttackTimer);
        Battle.SoulMovementDisabled = true;

        Attacker.FrameUpdateRateSpeedup = 1f;
        Attacker.Position = new Vector3(knightPushOrigin, Attacker.Position.Z);
        if (soulShineIntensity >= 0.25f)
        {
            float pushProgress = float.InverseLerp(0.25f, 1f, soulShineIntensity);
            float pushOffset = float.Smoothstep(0f, 350f, float.Pow(pushProgress, 0.7f));
            Vector2 pushDirection = -(TargetPosition - knightPushOrigin).SafeNormalize(Vector2.UnitX);
            pushDirection.Y += 0.4f;

            // As the Knight gets knocked back, the start
            // with the defensive anticipatory pose from
            // the flurry slashes, before transitioning to the
            // standard ball form and then becoming static-y.
            if (soulShineIntensity <= 0.4f)
                Attacker.SetTexture(Atlases.KnightScene.KnightSlashFlurryAnticipation, 1);
            else if (soulShineIntensity <= 0.65f)
                Attacker.SetTexture(Atlases.KnightScene.KnightBallFormAnimation, 5);
            else if (soulShineIntensity < 1f)
            {
                Attacker.SetTexture(Atlases.KnightScene.KnightBallFormTransition, 4, loops: false);
                Attacker.FrameUpdateRateSpeedup = 2f;
            }

            // Aim the soul towards the Knight.
            if (soulShineIntensity < 0.6f)
                Battle.Soul.Rotation = -Angle.PiOver2;

            Attacker.Position += pushDirection.AsVector3() * pushOffset;
        }

        // Awesome visuals! Yay!
        cameraZoom = float.LerpDt(cameraZoom, (1f - soulShineIntensity) * 0.2f, 5f, Scene.Time);
        ascentWindIntensity = float.LerpDt(ascentWindIntensity, 1.2f, 10f, Scene.Time);
        vignetteIntensity = float.LerpDt(vignetteIntensity, 2.4f, 8f, Scene.Time);
        ascentWindTimer += Scene.Time.DeltaTime * soulShineIntensity.Cubed() * 0.1f;

        if (soulShineIntensity >= 1f)
        {
            UpdateStaticSound();

            float relativeTimer = AttackTimer - shineDuration;
            Attacker.Position += Vector3.UnitX * float.Sin(relativeTimer * 4f) * 30f;
            Attacker.Position += Vector3.UnitY * float.Sin(relativeTimer * 3f + Attacker.Position.X / 120f) * 30f;

            Attacker.SetTexture(Atlases.KnightScene.KnightFakeDeath, 3);
        }

        if (AttackTimer >= shineDuration + stateTransitionDelay)
        {
            currentSubstate = Substate.ThisTimeItsTheHeartThatPierces;
            soulDashOrigin = TargetPosition;
            AttackTimer.Reset();
        }
    }

    private void ThisTimeItsTheHeartThatPierces()
    {
        if (!attackIsOver)
            UpdateStaticSound();

        Vector2 forward = (Battle.Soul.Rotation + Angle.PiOver2).Unit;
        Battle.SoulMovementDisabled = true;

        float reelBackDuration = 0.24f;
        float dashWaitDelay = 0.7f;
        float dashDuration = 0.2f;
        float reelBackProgress = float.InverseLerp(0f, reelBackDuration, AttackTimer);
        float dashProgress = float.InverseLerp(0f, dashDuration, AttackTimer - dashWaitDelay);

        float dashDistance = reelBackProgress.Cubed() * -100f + float.Smoothstep(dashProgress) * 512f;

        Vector2 jitter = Scene.Rng.NextCircular(dashProgress * 3f);
        Vector2 soulPosition = soulDashOrigin + forward * dashDistance + jitter;
        Battle.Soul.Position = new Vector3(soulPosition, Battle.Soul.Position.Z);
        Battle.Soul.AfterimageCount = 16;
        Battle.Soul.AfterimageIncrement = float.Bump(reelBackProgress) * 24f + float.Bump(dashProgress) * 50f;
        Battle.Soul.AfterimageDirection = forward;
        if (dashProgress > 0f)
        {
            Battle.Soul.AfterimageDirection *= -1f;
            if (!createdSoulDashVisuals)
            {
                Scene.ClashParticles.Add(new ClashParticle()
                {
                    Texture = Atlases.KnightScene.SusieClashParticle,
                    Velocity = Vector3.Zero,
                    Position = (soulPosition - forward * 232f).AsVector3(),
                    Rotation = new Angle(forward) - 0.44f,
                    Animates = true,
                    TotalFrames = 4
                });
                for (int i = 0; i < 4; i++)
                {
                    Scene.ClashParticles.Add(new ClashParticle()
                    {
                        Texture = Atlases.KnightScene.SusieClashParticle,
                        Velocity = forward.AsVector3() * i * 300f,
                        Position = (soulPosition - forward * 232f).AsVector3(),
                        Rotation = new Angle(forward) - 0.44f,
                        Animates = false,
                        TotalFrames = 4
                    });
                }

                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("CrossSlash.wav"), 1.2f));
                createdSoulDashVisuals = true;
            }
        }

        if (dashProgress >= 0.6f && !createdSoulImpactVisuals)
        {
            int variant = Scene.Rng.Next(2);
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav($"KnightHurt{variant + 1}.wav"), 1.2f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MetalHit.wav"), 1.7f));
            createdSoulImpactVisuals = true;

            Vector2 impactPosition = TargetPosition;
            for (int i = 0; i < 6; i++)
            {
                float spawnProgress = i / 5f;
                float maxScale = float.Lerp(200f, 700f, float.Pow(spawnProgress, 0.65f));
                Bullets.Add(new ShockwaveParticle(Scene, 0.25f, maxScale, impactPosition));
            }

            for (int i = 0; i < 40; i++)
            {
                float maxScale = Scene.Rng.NextFloat(0.8f, 1.2f);
                float residueLifetime = Scene.Rng.NextFloat(0.1f, 0.4f) * maxScale;
                Vector2 residueSpawnPosition = impactPosition + Scene.Rng.NextCircular(Battle.Box.Size.X * 0.45f);
                Bullets.Add(new BattleBoxResidueParticle(Scene, residueLifetime, maxScale, residueSpawnPosition));
            }
        }

        if (createdSoulImpactVisuals)
        {
            Attacker.Position += forward.AsVector3() * Scene.Time.DeltaTime * Scene.Rng.NextFloat(-1f, 1f) * 100f;
            Scene.AddScreenShake(Scene.Time.DeltaTime * 50f);
        }

        cameraZoom = dashProgress * 0.3f;
        ascentWindTimer += Scene.Time.DeltaTime * dashProgress * 1.4f;

        if (dashProgress >= 1f && !quickTimeEventsOngoing)
        {
            quickTimeEventsOngoing = true;
            quickTimeEventDuration = 0.8f;
            quickTimeEventCountdown = quickTimeEventDuration;
        }
        if (quickTimeEventsOngoing)
        {
            if (!attackIsOver)
                Scene.SoulLayer.PostDrawActions.Enqueue(RenderQuickTimeEventUI);
            quickTimeEventCountdown -= Scene.Time.DeltaTime;
            bool readyForInputs = quickTimeEventCountdown <= quickTimeEventDuration * 0.5f;
            bool correctInputTiming = readyForInputs && ActiveKeymap.Confirm.WasJustPressed;
            bool nahYouWinAnyway = quickTimeEventCountdown <= -0.5f;
            if ((correctInputTiming || nahYouWinAnyway) && !attackIsOver)
            {
                staticSound?.Stop();
                staticSound = null;

                attackIsOver = true;
                cameraZoom = 0f;
                attackEndTimer = Scene.Time.Rent();
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("ReverseCymbal.wav"), 1.5f));
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MetalHit.wav"), 1.3f));
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightRadialSplit.wav")));
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), 1.1f));

                int variant = Scene.Rng.Next(2);
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav($"KnightHurt{variant + 1}.wav"), 0.9f));

                quickTimeEventImpactVisualsTimer = 0.001f;
                Scene.AddScreenShake(56f);

                Scene.Music.Stop();
            }
        }

        if (attackIsOver && attackEndTimer.Cycle(5f))
        {
            quickTimeEventsOngoing = false;
            playedFlyDashSound = false;
            createdSoulDashVisuals = false;
            createdSoulImpactVisuals = false;
            AttackTimer.Reset();

            staticSound?.Stop();
            staticSound = null;

            Battle.Soul.Rotation = 0f;
            cameraZoom = 0f;

            Battle.Soul.Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, Battle.Soul.Position.Z);

            Scene.BattleBackBackdropOpacity = 0f;
            End();
        }
    }

    private void UpdateStaticSound()
    {
        if (staticSound is null)
        {
            AudioBuffer buffer = AudioBuffer.FromWav("Static.wav");
            staticSound = new Sound(buffer)
            {
                Loop = true,
            };
            staticSound.Stream.FrequencyRatio = 1.5f;
            Scene.Audio.Play(staticSound);
        }

        if (Scene.Rng.NextBool(10))
            staticSound.Stream.FrequencyRatio = Scene.Rng.NextFloat(0.7f, 1.6f);
    }

    private void SummonSwordWaveInstance(Vector2 origin, Vector2 swordMoveDirection)
    {
        int damage = stats.AscentSwordDamage;
        float swordHeight = Atlases.KnightScene.BlackSword.Area.Height * BlackSword.Scale;

        for (int i = 5; i >= 0; i--)
        {
            Vector2 topOffset = swordMoveDirection.RotatedBy(Angle.PiOver2) * (stats.AscentSwordGapSize + swordHeight) * (1f + i * 0.7f);
            if (i >= 1)
                topOffset += Scene.Rng.NextCircular(10f);

            Bullets.Add(new BlackSword(Scene, this, damage, damage, stats.AscentSwordGrazePoints, 999f, true, origin + topOffset, Vector2.Zero, -topOffset));
            Bullets.Add(new BlackSword(Scene, this, damage, damage, stats.AscentSwordGrazePoints, 999f, false, origin - topOffset, Vector2.Zero, -topOffset));
        }
    }

    private void LimitBulletQuantity(int maxBullets)
    {
        if (Bullets.Count > maxBullets)
        {
            for (int i = 0; i < Bullets.Count - maxBullets; i++)
                Bullets[i].Kill();
        }
    }

    private void RenderSpecialLayer(FrameContext context, RenderLayer layer)
    {
        if (shatteredScreen)
            return;

        int totalFrames = 30;
        int totalFramesX = 5;
        int totalFramesY = 6;
        float animationInterpolant = float.InverseLerp(0f, 0.8f, specialAnimationProgress);
        int frame = (int)float.Round(animationInterpolant * (totalFrames - 1f));
        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        AtlasEntry image = Atlases.___.IMAGE_COVENANT;

        Vector2 baseSize = new Vector2(image.Area.Width / totalFramesX, image.Area.Height / totalFramesY);
        Vector2 scale = NorthernlightGame.KnightFightResolution / Scene.MainCamera.Zoom / baseSize * 1.01f;
        scale.X *= -1f;

        Vector3 drawPosition = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, 400f);

        Renderer renderer = layer.OutputRenderer;
        renderer.Get<SpriteRenderer>().Enqueue(pixel, new SpriteData(pixel)
        {
            Position = drawPosition - Vector3.UnitZ * 100f,
            Tint = Color.Black,
            Scale = Vector2.One * 99999f
        });
        renderer.Get<SpriteRenderer>().Enqueue(image, new SpriteData(image, frame % totalFramesX, frame / totalFramesX, totalFramesX, totalFramesY)
        {
            Position = drawPosition,
            Tint = Color.White,
            Scale = scale
        });
    }

    private void RenderWind(SpriteRenderer sprites)
    {
        Texture texture = Atlases.LargeTextures.CloudyNoise.BackingTexture;

        SpriteData windData = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, BattleBox.BaseZ + 30f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 2f,
            Tint = Color.White * float.Pow(ascentWindIntensity, 0.75f),
            Rotation = windDirection
        };

        sprites.Enqueue(texture, windData, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.WindLinesShader, PrepareWindParameters));
    }

    private void PrepareWindParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new FallingStars.Uniforms()
        {
            Time = new Vector4(ascentWindTimer * -1.85f, 0f, 0f, 0f),
            OverlayColor = Vector4.One * ascentWindIntensity * 0.02f,
            WindTextureBounds = Atlases.LargeTextures.WindLines.VectorBounds,
        });
    }

    private void RenderVignette(SpriteRenderer sprites)
    {
        Texture texture = Atlases.KnightScene.Atlas.Value.BackingTexture;
        float baseZ = BattleBox.BaseZ;
        float pulse = 1f + float.Cos(float.Tau * AttackTimer * 0.9f) * vignetteIntensity * 0.16f;
        SpriteData data = new SpriteData(texture)
        {
            Position = Scene.CameraPanPosition + Vector3.UnitZ * (baseZ + 150f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 1.1f,
            Tint = new Color(74, 0, 120) * vignetteIntensity * pulse
        };
        sprites.Enqueue(texture, data, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.RedVignetteShader, DoAWholeBunchOfNothing));
    }

    private static void DoAWholeBunchOfNothing(FrameContext frame, RenderPass pass) { }

    public override void ExtraPrepareRender(SpriteRenderer sprites)
    {
        if (ascentWindIntensity > 0f)
            RenderWind(sprites);
        if (vignetteIntensity > 0f)
            RenderVignette(sprites);
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

    private void RenderQuickTimeEventUI(Renderer renderer)
    {
        float opacity = 1f;
        float normalizedTime = float.InverseLerp(quickTimeEventDuration, 0f, quickTimeEventCountdown);
        normalizedTime *= float.Smoothstep(float.InverseLerp(0.85f, 0f, quickTimeEventImpactVisualsTimer));

        float scale = 1.5f;
        AtlasEntry box = Atlases.KnightScene.AttackBoxInner;
        AtlasEntry boxOuter = Atlases.KnightScene.AttackBoxOuter;
        AtlasEntry pressIndicator = Atlases.KnightScene.AttackPressIndicator;
        AtlasEntry reticleCurrent = Atlases.KnightScene.AttackBarReticleThingGlow;
        AtlasEntry reticleIdeal = Atlases.KnightScene.AttackBarReticleThing;

        Vector2 shake = Scene.Rng.NextCircular(float.Bump(quickTimeEventImpactVisualsTimer.Squared()).Squared() * 20.5f);
        Vector3 drawPosition = new Vector3(Scene.MainCamera.Position.AsVector2() + shake + Vector2.UnitY * 270f, 400f);
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
            Tint = new Color(255, 0, 0) * opacity * 0.99f, // This time to the indicator is red instead of blue, indicating your power instead of Kris' power...
            Scale = Vector2.One * scale * new Vector2(1f, 1.57f),
        });
        sprites.Enqueue(reticleCurrent, new SpriteData(reticleCurrent)
        {
            Position = endingReticlePosition + new Vector3((1f - normalizedTime) * scale * -150f, 0f, 50f),
            Tint = Color.White * opacity * 0.99f,
            Scale = Vector2.One * scale * new Vector2(1f, 1.56f),
        }, BlendState.Additive);
    }

    public override void ExtraPrepareRender_SoulLayer(SpriteRenderer sprites, FrameContext context)
    {
        if (Scene.DeathScreen.PlayerDied)
            return;

        if (specialAnimationProgress >= 1f && !shatteredScreen)
        {
            Sound shatterSound = new Sound(AudioBuffer.FromWav("GlassShatter.wav"));
            shatterSound.Stream.FrequencyRatio = 1.333f;
            shatterSound.Stream.Volume *= 1.65f;

            Scene.Audio.Play(shatterSound);

            shatteredScreen = true;
            Scene.ShatterSystem.Shatter(Scene.SpecialImageLayer.ScreenShaders.ScreenTarget.Texture, context.CommandBuffer, 200);
        }

        // Ensure that the Knight renders in front
        // of the darkening visual once they're
        // ready.
        if (Scene.BattleBackBackdropOpacity > 0f)
        {
            Attacker.FireUpGlow = 0f;
            Attacker.PrepareRender(sprites);
        }

        foreach (Bullet bullet in Bullets)
        {
            if (bullet is RedFlurryTelegraph telegraph)
                telegraph.PrepareRender(sprites, context);
        }

        if (soulShineIntensity > 0f)
            Scene.SoulLayer.PostDrawActions.Enqueue(r => RenderSoulGlowVisuals(r, 10, soulShineIntensity));

        if (attackIsOver)
        {
            AtlasEntry pixel = Atlases.KnightScene.Pixel;
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = Vector3.UnitZ * 1000f,
                Tint = Color.Black,
                Scale = Vector2.One * 99999f
            });
        }
    }
}
