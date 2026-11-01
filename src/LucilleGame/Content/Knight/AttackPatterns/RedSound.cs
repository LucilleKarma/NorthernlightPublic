using System.Numerics;
using Northernlight.Content.Borders;
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

public sealed class RedSound : Attack<RoaringKnight>
{
    public readonly record struct Stats(float Duration, int NoteDamage, float NoteGrazePoints, float MaxGameSpeedup, float ShockwaveReleaseRate, float RadialSpreadUniformity, Vector2 BoxSize);

    public record Note(WeakReference<Sound> Sound, OwnedTimer Timer, float Duration, float StartingVolume);

    private int shockwaveCounter;

    private int auraFarmPoseFrame;

    private bool startedTeleport;

    private bool readyToAttack;

    private bool attackingThisFrame;

    private bool auraFarmPoseOngoing;

    private float auraFarmPoseFrameUpdateRate;

    private float doneAttackingTimer;

    private float redirectCooldown = 4f;

    private float horizontalGuitarSpeed;

    private float vignetteIntensity;

    private float auraFarmPoseFrameTimer;

    private float previousMusicPlaybackTime;

    private float auraFarmPosePreparationCountdown;

    private KnightArm leftKnightArm;

    private KnightArm rightKnightArm;

    private Vector2 guitarScale;

    private Vector3 knightFlyDestination;

    private Vector3 guitarPosition;

    private Vector2 battleBoxVelocity;

    private Vector2 KnightScale => Vector2.One * Attacker.Scale * new Vector2(Attacker.HorizontallyFlip ? -1f : 1f, 1f);

    private Vector3 LeftArmOrigin => Attacker.Position + new Vector3(-28f, -32f, -1f) * new Vector3(KnightScale, 1f);

    private Vector3 RightArmOrigin => Attacker.Position + new Vector3(18f, -34f, -1f) * new Vector3(KnightScale, 1f);

    private readonly MidiSequencer musicSequencer;

    private readonly OwnedTimer shockwaveTimer;

    private readonly List<Note> activeNotes = [];

    private static Angle GuitarRotation => -0.7f;

    internal readonly Stats stats;

    internal static readonly string[] pitchNames =
    [
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    ];

    /// <summary>
    ///     Whether it'd be a bad time to play the
    ///     guitar from the MIDI playback.
    /// </summary>
    public bool BadTimeForGuitar
    {
        get
        {
            if (Scene.Music.Tracks.Length <= 0)
                return true;

            Sound blackKnifeMusic = Scene.Music.Tracks[0];
            return blackKnifeMusic.PlayPositionSeconds < 80f;
        }
    }

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText =>
        Scene.SideB ? LocalizationEntries.BattleFlavorTextRedSoundSideB : LocalizationEntries.BattleFlavorTextRedSound;

    public RedSound(BattleHandler battle, Stats stats) : base(battle)
    {
        this.stats = stats;
        shockwaveTimer = Scene.Time.Rent();

        using FileStream midiFileStream = File.OpenRead(ResourceMonitor.CheckForSubdirectoryEquivalent("BlackKnifeGuitarMidi.mid"));
        musicSequencer = new MidiSequencer(MidiParser.Parse(midiFileStream), startPaused: true);
        musicSequencer.OnPlaybackActionFire += ProcessSequencerNotes;
    }

    private void ProcessSequencerNotes(MidiSequencer.PlaybackAction action)
    {
        if (musicSequencer.Paused)
            return;

        if (auraFarmPoseFrame >= 4)
            return;

        if (BadTimeForGuitar)
            return;

        static string CalculateNoteName(int pitch)
        {
            int octave = pitch / 12;
            string name = pitchNames[pitch % 12];
            return $"{name}{octave}";
        }

        float FindDurationToNextNoteOffCommand()
        {
            int index = musicSequencer.ActionIndex;
            double duration = 0D;
            while ((musicSequencer.Actions[index].Status & 0xF0) != 0x80)
            {
                index++;
                if (index >= musicSequencer.Actions.Count)
                    return (float)duration;

                duration += musicSequencer.Actions[index].Duration;
            }

            return (float)duration;
        }

        int status = action.Status & 0xF0;
        bool isNoteOn = status == 0x90;
        if (isNoteOn)
        {
            float velocity = action.Data2 / 127f;
            if (velocity > 0f)
            {
                int pitch = action.Data1 - 14;
                string noteName = CalculateNoteName(pitch);
                float pitchFactor = 1f;

                string? noteFilePath;
                switch (noteName)
                {
                    case "A4":
                        noteFilePath = "KnightGuitarA4.wav";
                        break;
                    case "A5":
                        noteFilePath = "KnightGuitarA5.wav";
                        break;
                    case "A6":
                        noteFilePath = "KnightGuitarA6.wav";
                        break;
                    case "A7":
                        noteFilePath = "KnightGuitarA7.wav";
                        break;

                    case "A#4":
                        noteFilePath = "KnightGuitarASharp4.wav";
                        break;
                    case "A#5":
                        noteFilePath = "KnightGuitarASharp5.wav";
                        break;
                    case "A#6":
                        noteFilePath = "KnightGuitarASharp6.wav";
                        break;
                    case "A#7":
                        noteFilePath = "KnightGuitarASharp7.wav";
                        break;

                    case "B4":
                        noteFilePath = "KnightGuitarB4.wav";
                        break;
                    case "B5":
                        noteFilePath = "KnightGuitarB5.wav";
                        break;
                    case "B6":
                        noteFilePath = "KnightGuitarB6.wav";
                        break;
                    case "B7":
                        noteFilePath = "KnightGuitarB7.wav";
                        break;

                    case "C4":
                        noteFilePath = "KnightGuitarC4.wav";
                        break;
                    case "C5":
                        noteFilePath = "KnightGuitarC5.wav";
                        break;
                    case "C6":
                        noteFilePath = "KnightGuitarC6.wav";
                        break;
                    case "C7":
                        noteFilePath = "KnightGuitarC7.wav";
                        break;
                    case "C8":
                        noteFilePath = "KnightGuitarC8.wav";
                        break;

                    case "C#5":
                        noteFilePath = "KnightGuitarCSharp5.wav";
                        break;
                    case "C#6":
                        noteFilePath = "KnightGuitarCSharp6.wav";
                        break;
                    case "C#7":
                        noteFilePath = "KnightGuitarCSharp7.wav";
                        break;

                    case "D5":
                        noteFilePath = "KnightGuitarD5.wav";
                        break;
                    case "D6":
                        noteFilePath = "KnightGuitarD6.wav";
                        break;
                    case "D7":
                        noteFilePath = "KnightGuitarD7.wav";
                        break;

                    case "D#5":
                        noteFilePath = "KnightGuitarDSharp5.wav";
                        break;
                    case "D#6":
                        noteFilePath = "KnightGuitarDSharp6.wav";
                        break;
                    case "D#7":
                        noteFilePath = "KnightGuitarDSharp7.wav";
                        break;

                    case "E4":
                        noteFilePath = "KnightGuitarE4.wav";
                        break;
                    case "E5":
                        noteFilePath = "KnightGuitarE5.wav";
                        break;
                    case "E6":
                        noteFilePath = "KnightGuitarE6.wav";
                        break;
                    case "E7":
                        noteFilePath = "KnightGuitarE7.wav";
                        break;

                    case "F4":
                        noteFilePath = "KnightGuitarF4.wav";
                        break;
                    case "F5":
                        noteFilePath = "KnightGuitarF5.wav";
                        break;
                    case "F6":
                        noteFilePath = "KnightGuitarF6.wav";
                        break;
                    case "F7":
                        noteFilePath = "KnightGuitarF7.wav";
                        break;

                    case "F#4":
                        noteFilePath = "KnightGuitarFSharp7.wav";
                        break;
                    case "F#5":
                        noteFilePath = "KnightGuitarFSharp5.wav";
                        break;
                    case "F#6":
                        noteFilePath = "KnightGuitarFSharp6.wav";
                        break;
                    case "F#7":
                        noteFilePath = "KnightGuitarFSharp7.wav";
                        break;

                    case "G4":
                        noteFilePath = "KnightGuitarG4.wav";
                        break;
                    case "G5":
                        noteFilePath = "KnightGuitarG5.wav";
                        break;
                    case "G6":
                        noteFilePath = "KnightGuitarG6.wav";
                        break;
                    case "G7":
                        noteFilePath = "KnightGuitarG7.wav";
                        break;

                    case "G#4":
                        noteFilePath = "KnightGuitarGSharp7.wav";
                        break;
                    case "G#5":
                        noteFilePath = "KnightGuitarGSharp5.wav";
                        break;
                    case "G#6":
                        noteFilePath = "KnightGuitarGSharp6.wav";
                        break;
                    case "G#7":
                        noteFilePath = "KnightGuitarGSharp7.wav";
                        break;

                    default:
                        noteFilePath = "KnightGuitarC5.wav";
                        pitchFactor = 1f / float.Pow(2f, pitch / 12f - 5f);
                        break;
                }

                if (noteFilePath is not null)
                {
                    float volume = Scene.Rng.NextFloat(0.9f, 1f) * velocity * 8.5f;

                    Sound sound = new Sound(AudioBuffer.FromWav(noteFilePath), volume);
                    sound.Stream.FrequencyRatio = pitchFactor;
                    float durationToStop = FindDurationToNextNoteOffCommand() * 1.3f;
                    if (auraFarmPoseOngoing)
                    {
                        volume *= 1.75f;
                        durationToStop *= 1.45f;
                    }

                    activeNotes.Add(new Note(new WeakReference<Sound>(sound), Scene.Time.Rent(ignoreGlobalSpeed: true), durationToStop, volume));
                    Scene.Audio.Play(sound);
                }

                // Look for the next note. If it's gonna be a long
                // one, queue an aura farm pose to go with it.
                // It'll be awesome...
                int index = musicSequencer.ActionIndex;
                float frameWait = auraFarmPoseFrameUpdateRate * 3f;
                double delayToNextNote = 0D;
                while (true)
                {
                    index++;
                    if (index >= musicSequencer.Actions.Count - 1)
                        break;

                    float timeToAdd = (float)musicSequencer.Actions[index].Duration;
                    bool validNextNote = true;
                    if ((musicSequencer.Actions[index].Status & 0xF0) != 0x90)
                    {
                        validNextNote = false;
                    }
                    if (musicSequencer.Actions[index].Duration <= 0f)
                        validNextNote = false;
                    if (delayToNextNote - frameWait <= 0f)
                        validNextNote = false;

                    if (validNextNote)
                        break;

                    delayToNextNote += timeToAdd;
                }
                if (auraFarmPosePreparationCountdown <= 0f && !auraFarmPoseOngoing)
                {
                    auraFarmPoseFrameUpdateRate = 0.15f;

                    MidiSequencer.PlaybackAction nextNotePlay = musicSequencer.Actions[index];
                    if (nextNotePlay.Duration >= 1.25f)
                        auraFarmPosePreparationCountdown = (float)delayToNextNote - 0.12f;
                }

                if (auraFarmPoseOngoing)
                {
                    CreateWave(true);
                    KnightBorder.Instance.BiasToRedInterpolant = 0.9f;
                    KnightBorder.Instance.ShineIntensityFactor = 6.1f;
                }
                else
                    GiftTheSoulWithMusic(delayToNextNote);
            }
        }
    }

    protected override void UpdateInternal()
    {
        attackingThisFrame = false;
        if (Scene.Music.Tracks.Length >= 1)
        {
            Sound blackKnifeMusic = Scene.Music.Tracks[0]; // Should be the only thing playing.
            musicSequencer.Update(blackKnifeMusic.PlayPositionSeconds);
        }
        CloselyMonitorNotesForDurationStop();

        bool nopeWereDoneYouDied = Scene.Time.GlobalSpeed <= 0f;
        if (nopeWereDoneYouDied)
        {
            musicSequencer.Paused = true;
            return;
        }

        AtlasEntry startingTeleport = Atlases.KnightScene.KnightBallFormTransition;
        AtlasEntry returningTeleport = Atlases.KnightScene.KnightBallFormTransitionForwardBladeless;

        // Start in the foreground.
        if (!startedTeleport)
        {
            startedTeleport = true;
            Attacker.InBackground = false;
            Attacker.SetTexture(startingTeleport, 4, loops: false, animatesInReverse: true);
        }

        // Teleport above the battle box.
        if (!readyToAttack && Attacker.RenderState.Texture == startingTeleport && Attacker.RenderState.CurrentFrame == 0)
        {
            Attacker.Position = new Vector3(NorthernlightGame.KnightFightResolution * new Vector2(0.425f, 0.14f), Attacker.SpawnPosition.Z + 32f);
            Attacker.SetTexture(returningTeleport, 5, loops: false);
        }

        Attacker.RepositionDelay = 0.2f;
        Attacker.EmitsAfterimages = true;

        // Then START BLASTING!!!
        if (!readyToAttack && Attacker.RenderState.Texture == returningTeleport && Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
        {
            leftKnightArm = new KnightArm(LeftArmOrigin.AsVector2(), true);
            rightKnightArm = new KnightArm(RightArmOrigin.AsVector2(), false);
            knightFlyDestination = Attacker.Position;
            guitarPosition = Attacker.Position;
            readyToAttack = true;
        }

        float guitarAppearance = float.InverseLerp(0f, 0.75f, AttackTimer);
        float idealVignetteIntensity = float.InverseLerpBump(-0.75f, 6.25f, stats.Duration - 4f, stats.Duration, AttackTimer);
        vignetteIntensity = float.LerpDt(vignetteIntensity, idealVignetteIntensity, 3f, Scene.Time);
        Scene.Time.GlobalSpeed = float.Lerp(1f, stats.MaxGameSpeedup, vignetteIntensity);

        if (doneAttackingTimer > 0f)
        {
            FinishAttack();
            guitarAppearance *= float.InverseLerp(0.5f, 0f, doneAttackingTimer);
            Scene.Time.GlobalSpeed = float.Lerp(Scene.Time.GlobalSpeed, 1f, float.InverseLerp(0f, 0.4f, doneAttackingTimer));
            doneAttackingTimer += Scene.Time.DeltaTime;
        }
        else if (readyToAttack)
            PerformAttack(ref guitarAppearance);
        else
        {
            AttackTimer.Reset();
            shockwaveTimer.Reset();

            Attacker.Velocity = Vector3.Zero;
            guitarAppearance = 0f;
        }

        float easedAppearance = float.Max(0f, float.EaseInOutBack(guitarAppearance));
        guitarScale = new Vector2(easedAppearance, float.Pow(easedAppearance, 2.2f));

        if (redirectCooldown > 0f)
            redirectCooldown -= Scene.Time.DeltaTime;

        KeepBattleBoxInSaneBounds();
        ReturnBattleBoxRotationToNormal();

        Battle.Box.Position += battleBoxVelocity * Scene.Time.DeltaTime;
        battleBoxVelocity = Vector2.LerpDt(battleBoxVelocity, Vector2.Zero, 11.5f, Scene.Time);

        bool wasPaused = musicSequencer.Paused;
        if (wasPaused && attackingThisFrame)
            musicSequencer.Paused = false;

        if (!wasPaused && !attackingThisFrame)
            musicSequencer.Paused = true;
    }

    private void PerformAttack(ref float guitarAppearance)
    {
        attackingThisFrame = true;
        Attacker.EmitsAfterimages = false;

        float horizontalUndulation = float.Sin(float.Tau * AttackTimer * 0.15f) * 600f;
        float verticalUndulation = float.Sin(float.Tau * AttackTimer * 0.4f) * 300f;
        Vector3 idleMovement = new Vector3(horizontalUndulation, verticalUndulation, 0f);

        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, (knightFlyDestination - Attacker.Position) * 12f + idleMovement, 30f, Scene.Time);

        float distanceFromBox = Vector2.Distance(knightFlyDestination.AsVector2(), Battle.Box.Position);
        bool needsToReposition = distanceFromBox >= 250f;

        // If the Knight needs to reposition due to being
        // too close to the battle box, make them move
        // back a bit until everything's OK.
        if (!needsToReposition)
        {
            Vector3 directionToBox = new Vector3((Battle.Box.Position - knightFlyDestination.AsVector2()).SafeNormalize(Vector2.Zero), 0f);
            knightFlyDestination -= directionToBox * Scene.Time.DeltaTime * 120f;
        }

        // Randomly choose a new point to fly to.
        float redirectDelay = 12.5f;
        float distanceToDestination = Vector3.Distance(Attacker.Position, knightFlyDestination);
        if (redirectCooldown <= 0f && distanceToDestination <= 50f && Scene.Rng.NextBool(30) && auraFarmPosePreparationCountdown <= 0f && !auraFarmPoseOngoing)
        {
            int tries = 0;
            Vector3 previousFlyDestination = knightFlyDestination;
            do
            {
                Vector2 flyOffset = Scene.Rng.NextCircular(540f) * new Vector2(1f, 0.125f) - Vector2.UnitY * 100f;
                knightFlyDestination = new Vector3(Battle.Box.Position + flyOffset, Attacker.Position.Z);
                tries++;
            }
            while (ShouldTryDifferentHoverSpot(previousFlyDestination, knightFlyDestination) && tries < 100);

            Scene.AddScreenShake(4f);
            redirectCooldown = redirectDelay;
        }

        Vector2 guitarOrigin = (Attacker.Position - Attacker.Velocity * Scene.Time.DeltaTime).AsVector2() - Vector2.UnitY * 28f;
        Vector2 forward = GuitarRotation.Unit;
        Vector2 orthogonal = forward.RotatedBy(Angle.PiOver2);
        Vector2 leftArmDestination = guitarOrigin - forward * 25f;
        Vector2 rightArmDestination = guitarOrigin + forward * 20f;

        float armSpeedFactor = 1.95f;
        float knightSpeed = Attacker.Velocity.Length();
        bool flying = knightSpeed >= 220f;
        if (flying || guitarAppearance < 0.7f)
        {
            leftArmDestination = guitarOrigin + new Vector2(-120f, 32f);
            rightArmDestination = guitarOrigin + new Vector2(120f, 32f);
            armSpeedFactor = 2f;
            attackingThisFrame = false;
        }

        leftKnightArm.MoveTowards(Scene, leftArmDestination, armSpeedFactor * 4f, armSpeedFactor * 300f);
        rightKnightArm.MoveTowards(Scene, rightArmDestination, armSpeedFactor * 4f, armSpeedFactor * 300f);

        Vector2 knightVelocity2D = Attacker.Velocity.AsVector2();
        leftKnightArm.Update(Scene, knightVelocity2D, LeftArmOrigin.AsVector2());
        rightKnightArm.Update(Scene, knightVelocity2D, RightArmOrigin.AsVector2());

        // Make the Knight move their arms in such a way
        // as to suggest plucks on the guitar.
        // I, unfortunately, do not play the guitar, so this is
        // probably gonna look a bit weird to someone who does.

        // Might be a good idea to do more research into
        // proper guitar form...
        if (!flying)
        {
            float pluckSpeed = 450f;
            if (Scene.Rng.NextBool(5))
                pluckSpeed *= 2f;

            float minHandSpeedThreshold = Scene.Time.GlobalSpeed * 15f;
            float pluckChance = float.Clamp(Scene.Time.GlobalSpeed * 0.5f, 0f, 1f);
            if (Scene.Rng.NextBool(pluckChance) && leftKnightArm.Velocity.Length() <= minHandSpeedThreshold)
                leftKnightArm.Velocity -= orthogonal * pluckSpeed + forward * Scene.Rng.NextFloat(-10f, 10f);
            if (Scene.Rng.NextBool(pluckChance) && rightKnightArm.Velocity.Length() <= minHandSpeedThreshold)
                rightKnightArm.Velocity += orthogonal * pluckSpeed + forward * Scene.Rng.NextFloat(-10f, 10f);
        }

        Vector3 newGuitarPosition = Vector3.LerpDt(guitarPosition, Attacker.Position + Attacker.Velocity * Scene.Time.DeltaTime, 102f, Scene.Time);

        horizontalGuitarSpeed = newGuitarPosition.X - guitarPosition.X;
        guitarPosition = newGuitarPosition;

        AtlasEntry teleport = Atlases.KnightScene.KnightBallFormTransitionForwardBladeless;

        // Handle animation work.
        // When the Knight is flying, they enter their ball form
        // animation until reaching their destination.
        // After that, they do a reformation animation.

        // Once that's complete, it's back to having fun with
        // the guitar!
        if (flying)
            Attacker.SetTexture(Atlases.KnightScene.KnightBallFormAnimation, 5);
        else
        {
            if (Attacker.RenderState.Texture == Atlases.KnightScene.KnightBallFormAnimation)
                Attacker.SetTexture(teleport, 5, loops: false);
            else if (Attacker.RenderState.Texture == teleport)
            {
                if (Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
                    Attacker.SetTexture(Atlases.KnightScene.KnightGuitarHoldBody, 1);
            }
            else
                Attacker.SetTexture(Atlases.KnightScene.KnightGuitarHoldBody, 1);
        }
        if (auraFarmPoseOngoing)
        {
            Attacker.RenderOverrideAction = RenderKnightDoingGuitarPose;
            auraFarmPoseFrameTimer += Scene.Time.DeltaTime;
            if (auraFarmPoseFrameTimer >= auraFarmPoseFrameUpdateRate * (auraFarmPoseFrame == 6 ? 24f : 1f))
            {
                auraFarmPoseFrameTimer = 0f;
                auraFarmPoseFrame++;
                if (auraFarmPoseFrame == 3)
                {
                    Scene.AddScreenShake(18.5f);
                    Scene.ScreenDarkening = -1.15f;
                    vignetteIntensity += 0.65f;
                }
                if (auraFarmPoseFrame >= 7)
                {
                    auraFarmPoseFrame = 6;
                    auraFarmPoseOngoing = false;
                }
            }
        }
        else
        {
            Attacker.RenderOverrideAction = RenderKnightPlayingGuitar;

            auraFarmPoseFrameTimer = 0f;
            auraFarmPoseFrame = 0;
            if (auraFarmPosePreparationCountdown > 0f && Scene.Music.Tracks.Length >= 1)
            {
                float playbackChange = float.Max(0f, (float)Scene.Music.Tracks[0].PlayPositionSeconds - previousMusicPlaybackTime);
                auraFarmPosePreparationCountdown -= playbackChange;
                if (auraFarmPosePreparationCountdown <= 0f)
                    auraFarmPoseOngoing = true;
            }
        }

        if (Scene.Music.Tracks.Length >= 1)
            previousMusicPlaybackTime = (float)Scene.Music.Tracks[0].PlayPositionSeconds;
        Attacker.RenderOverrideResetCountdown = 0.2f;

        // Handle projectile shot stuff.
        if (!flying && needsToReposition && !auraFarmPoseOngoing)
        {
            if (shockwaveTimer.Cycle(stats.ShockwaveReleaseRate))
                CreateWave(false);
        }

        bool imTiredCanWePleaseDoADifferentAttack = AttackTimer >= stats.Duration && doneAttackingTimer <= 0f;
        if (imTiredCanWePleaseDoADifferentAttack)
            doneAttackingTimer = 0.0001f;
    }

    private bool ShouldTryDifferentHoverSpot(Vector3 previousFlyDestination, Vector3 p)
    {
        if (Vector3.Distance(previousFlyDestination, knightFlyDestination) < 348.5f)
            return true;

        if (Vector2.Distance(previousFlyDestination.AsVector2(), Battle.Box.Position) < 270f)
            return true;

        if (p.X <= 120f || p.X >= NorthernlightGame.KnightFightResolution.X - 120f)
            return true;

        return false;
    }

    private void CreateWave(bool forceBig)
    {
        shockwaveCounter++;

        bool harder = BadTimeForGuitar;
        int bigWaveRate = harder ? 3 : 7;
        bool bigWave = shockwaveCounter % bigWaveRate == bigWaveRate - 1 || forceBig;

        Scene.AddScreenShake(bigWave ? 16.5f : 7.25f);
        if (!forceBig)
            Bullets.Add(new RadialWaveParticle(Scene, Battle.Box, bigWave ? 0.8f : 0.6f, bigWave ? 2000f : 1500f, Attacker.Position.AsVector2(), Color.Black));

        Vector2 boxToKnight = (Attacker.Position.AsVector2() - Battle.Box.Position).SafeNormalize(Vector2.Zero);

        if (bigWave)
        {
            float randomAngle = Scene.Rng.NextFloat(float.Tau);
            for (int i = 0; i < 23; i++)
            {
                int damage = stats.NoteDamage;
                float grazePoints = stats.NoteGrazePoints;
                Vector2 spawnPosition = guitarPosition.AsVector2();
                Vector2 radial = (float.Tau * i / 25f + randomAngle).ToUnit();
                Vector2 direction = Vector2.Lerp(Scene.Rng.NextCircularEdge(1f), radial, stats.RadialSpreadUniformity);
                Vector2 noteVelocity = direction * Scene.Rng.NextFloat(0.6f, 1f) * 345f;
                if (harder)
                    noteVelocity *= 1.15f;

                if (Vector2.Distance(spawnPosition, TargetPosition) <= 320f)
                    noteVelocity *= 0.7f;

                RedNote.NoteKind noteKind = (RedNote.NoteKind)Scene.Rng.Next((int)RedNote.NoteKind.Count);

                Bullets.Add(new RedNote(Scene, spawnPosition, noteVelocity, damage, grazePoints, noteKind)
                {
                    Scale = 1.6f
                });
            }

            battleBoxVelocity -= boxToKnight * (harder ? 312f : 650f);
            Battle.Box.Rotation -= boxToKnight.X * Scene.Rng.NextFloat(0.5f, 1f) * 0.3f;
        }
    }

    private void GiftTheSoulWithMusic(double delayToNextNote)
    {
        int damage = stats.NoteDamage;
        float grazePoints = 10f;
        float shootSpeed = 350f;
        Vector2 spawnPosition = guitarPosition.AsVector2();
        Vector2 noteVelocity = (TargetPosition - spawnPosition).SafeNormalize(Vector2.UnitY).RotatedBy(Scene.Rng.NextFloat(-0.02f, 0.02f)) * shootSpeed;

        RedNote.NoteKind noteKind = RedNote.NoteKind.Eighth;
        if (delayToNextNote >= 1.5)
            noteKind = RedNote.NoteKind.Quarter;
        if (delayToNextNote >= 3)
            noteKind = RedNote.NoteKind.Half;

        Bullets.Add(new RedNote(Scene, spawnPosition, noteVelocity, damage, grazePoints, noteKind)
        {
            Scale = 1.6f
        });
    }

    private void KeepBattleBoxInSaneBounds()
    {
        float battleBoxCenterX = Battle.Box.Position.X;
        float battleBoxCenterY = Battle.Box.Position.Y;
        float battleBoxTop = battleBoxCenterY - Battle.Box.Size.Y * 0.5f;
        float battleBoxBottom = battleBoxCenterY + Battle.Box.Size.Y * 0.5f;
        float verticalSpeed = 75f;
        float horizontalSpeed = 97.5f;

        if (battleBoxTop < NorthernlightGame.KnightFightResolution.Y * 0.25f)
            Battle.Box.Position += Vector2.UnitY * Scene.Time.DeltaTime * verticalSpeed;
        if (battleBoxBottom > NorthernlightGame.KnightFightResolution.Y * 0.625f)
            Battle.Box.Position -= Vector2.UnitY * Scene.Time.DeltaTime * verticalSpeed;

        float battleBoxLeft = battleBoxCenterX - Battle.Box.Size.X * 0.5f;
        float battleBoxRight = battleBoxCenterX + Battle.Box.Size.X * 0.5f;
        if (battleBoxLeft < NorthernlightGame.KnightFightResolution.X * 0.16f)
            Battle.Box.Position += Vector2.UnitX * Scene.Time.DeltaTime * horizontalSpeed;
        if (battleBoxRight > NorthernlightGame.KnightFightResolution.X * 0.84f)
            Battle.Box.Position -= Vector2.UnitX * Scene.Time.DeltaTime * horizontalSpeed;
    }

    private void ReturnBattleBoxRotationToNormal()
    {
        float theta = Battle.Box.Rotation;
        if (theta > float.Pi)
            theta -= float.Tau;

        Battle.Box.Rotation = float.LerpDt(theta, 0f, 1.04f, Scene.Time);
    }

    private void CloselyMonitorNotesForDurationStop()
    {
        activeNotes.RemoveAll(n => !n.Sound.TryGetTarget(out Sound? s) || s.PositionBytes >= s.LoopEndBytes || s.Disposed);
        for (int i = 0; i < activeNotes.Count; i++)
        {
            Note note = activeNotes[i];
            if (!note.Sound.TryGetTarget(out Sound? s))
                continue;

            float fadeoutProgress = note.Timer / note.Duration;
            s.Stream.Volume = note.StartingVolume * float.InverseLerp(1f, 0.5f, fadeoutProgress);

            if (note.Timer.Elapsed(note.Duration))
            {
                s.Stop();
                activeNotes.RemoveAt(i);
                i--;
            }
        }
    }

    private void FinishAttack()
    {
        Vector3 flyDestination = Attacker.SpawnPosition;
        bool flying = Vector3.Distance(Attacker.Position, flyDestination) >= 50f;

        if (flying)
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightBallFormAnimation, 5);

            Vector3 idealVelocity = (flyDestination - Attacker.Position).SafeNormalize(Vector3.Zero) * 1400f;
            Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealVelocity, 32f, Scene.Time);
        }
        else
        {
            Attacker.Velocity = Vector3.Zero;

            AtlasEntry teleport = Atlases.KnightScene.KnightBallFormTransition;
            if (Attacker.RenderState.Texture == Atlases.KnightScene.KnightBallFormAnimation ||
                Attacker.RenderState.Texture == Atlases.KnightScene.KnightGuitarHoldBody)
            {
                Attacker.SetTexture(teleport, 4, loops: false);
            }
            else if (Attacker.RenderState.Texture == teleport)
            {
                if (Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1 && doneAttackingTimer >= 0.55f)
                    End();
            }
        }
    }

    private void RenderKnightPlayingGuitar(SpriteRenderer sprites)
    {
        Vector3 knightPosition = Attacker.Position;

        AtlasEntry texture = Attacker.RenderState.Texture;
        int frame = Attacker.RenderState.CurrentFrame;
        int totalFrames = Attacker.RenderState.TotalFrames;

        sprites.Enqueue(texture, new SpriteData(texture, 0, frame, 1, totalFrames) with
        {
            Position = knightPosition,
            Scale = Vector2.One * Attacker.Scale * new Vector2(Attacker.HorizontallyFlip ? -1f : 1f, 1f) * 2f,
            Tint = Color.White * Attacker.Opacity,
        }, queuePriorityInfluence: 50);

        RenderGuitar(sprites);

        if (texture == Atlases.KnightScene.KnightGuitarHoldBody)
        {
            leftKnightArm.PrepareRender(sprites, LeftArmOrigin, KnightScale * 2f);
            rightKnightArm.PrepareRender(sprites, RightArmOrigin, KnightScale * 2f);
        }
    }

    private void RenderKnightDoingGuitarPose(SpriteRenderer sprites)
    {
        Vector3 knightPosition = Attacker.Position;
        AtlasEntry pose = Atlases.KnightScene.KnightGuitarStrikeAnimation;
        AtlasEntry poseArms = Atlases.KnightScene.KnightGuitarStrikeAnimationArms;
        sprites.Enqueue(pose, new SpriteData(pose, 0, auraFarmPoseFrame, 1, 7) with
        {
            Position = knightPosition,
            Scale = Vector2.One * Attacker.Scale * new Vector2(Attacker.HorizontallyFlip ? -1f : 1f, 1f) * 2f,
            Tint = Color.White * Attacker.Opacity,
        }, BlendState.NonPremultiplied, queuePriorityInfluence: 50);

        RenderGuitar(sprites);

        sprites.Enqueue(poseArms, new SpriteData(poseArms, 0, auraFarmPoseFrame, 1, 7) with
        {
            Position = knightPosition + Vector3.UnitZ * 5f,
            Scale = Vector2.One * Attacker.Scale * new Vector2(Attacker.HorizontallyFlip ? -1f : 1f, 1f) * 2f,
            Tint = Color.White * Attacker.Opacity,
        }, queuePriorityInfluence: 50);
    }

    private void RenderGuitar(SpriteRenderer sprites)
    {
        AtlasEntry guitar = Atlases.KnightScene.Guitar;
        int guitarFrame = (int)(AttackTimer * 7.3f) % 4 + 2;
        sprites.Enqueue(guitar, new SpriteData(guitar, 0, guitarFrame, 1, 6)
        {
            Position = new Vector3(guitarPosition.X, guitarPosition.Y + 20f, Attacker.Position.Z),
            Scale = KnightScale * guitarScale,
            Tint = Color.White,
            Rotation = GuitarRotation + float.Atan(75f / 129f) + horizontalGuitarSpeed * 0.0004f
        }, queuePriorityInfluence: 50);
    }

    public override void ExtraPrepareRender(SpriteRenderer sprites)
    {
        if (vignetteIntensity > 0f)
            RenderVignette(sprites);
    }

    private void RenderVignette(SpriteRenderer sprites)
    {
        Texture texture = Atlases.KnightScene.Atlas.Value.BackingTexture;

        float baseZ = BattleBox.BaseZ;
        float pulse = 1f + float.Cos(float.Tau * AttackTimer * 0.6f) * vignetteIntensity * 0.2f;
        float vignetteOpacity = vignetteIntensity;
        SpriteData data = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, baseZ + 150f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size,
            Tint = new Color(255, 0, 0) * vignetteIntensity * pulse
        };
        sprites.Enqueue(texture, data, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.RedVignetteShader, DoAWholeBunchOfNothing));
    }

    private static void DoAWholeBunchOfNothing(FrameContext frame, RenderPass pass) { }
}
