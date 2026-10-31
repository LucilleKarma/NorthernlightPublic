using System.Numerics;
using Northernlight.Content.Credits;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Content.Knight.Particles;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     The handler responsible for this Side A Knight
///     clash animation.
/// </summary>
public sealed class KnightClashAnimationHandler
{
    private int susieClashImpactCounter;

    private bool playedSusieClashJumpSound;

    private float screenWhiteningDestination;

    private float screenWhiteningApproachSpeed;

    private float noelleReactionTimer;

    private readonly OwnedTimer susieClashImpactTimer;

    /// <summary>
    ///     The scene responsible for this.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
    }

    /// <summary>
    ///     The battle handler responsible for this.
    /// </summary>
    public BattleHandler Battle
    {
        get;
    }

    public KnightClashAnimationHandler(BattleHandler battle)
    {
        Battle = battle;
        Scene = battle.Scene;
        susieClashImpactTimer = Scene.Time.Rent();
    }

    /// <summary>
    ///     Resets the state of this handler back to its
    ///     default.
    /// </summary>
    public void Reset()
    {
        susieClashImpactCounter = 0;
        playedSusieClashJumpSound = false;
        screenWhiteningDestination = 0f;
        screenWhiteningApproachSpeed = 0f;
        noelleReactionTimer = 0f;
        susieClashImpactTimer.Reset();
    }

    /// <summary>
    ///     Updates this handler.
    /// </summary>
    public void Update(OwnedTimer stateTimer)
    {
        if (Scene.Heroes.Susie.ActiveAnimation.Texture != Atlases.KnightScene.SusieKnightClash)
            Scene.Heroes.Susie.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.SusieKnightClash, 4, Scene.Game, loops: true);
        if (Scene.Heroes.Noelle.ActiveAnimation != Scene.Heroes.Noelle.DefendAnimation)
            Scene.Heroes.Noelle.ActiveAnimation = Scene.Heroes.Noelle.DefendAnimation;

        Scene.RoaringKnight.SetTexture(Atlases.KnightScene.KnightWeak, 1);

        Scene.Music.Stop();

        if (!playedSusieClashJumpSound)
        {
            playedSusieClashJumpSound = true;
            susieClashImpactTimer.Reset();
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("Jump.wav")));
        }

        int maxClashCount = 13;
        bool done = susieClashImpactCounter >= maxClashCount;

        // Make screen-whitening effects created by this method dissipate
        // over time.
        float whiteningDirection = float.Sign(screenWhiteningDestination - Battle.screenWhitening);

        Battle.screenWhitening += whiteningDirection * screenWhiteningApproachSpeed * Scene.Time.DeltaTime;
        bool reachedDestination =
            (whiteningDirection == 1 && Battle.screenWhitening >= screenWhiteningDestination) ||
            (whiteningDirection == -1 && Battle.screenWhitening <= screenWhiteningDestination) ||
            whiteningDirection == 0;
        if (reachedDestination)
        {
            Battle.screenWhitening = screenWhiteningDestination;
            screenWhiteningApproachSpeed = 0.7f;
            screenWhiteningDestination = 0f;
        }

        float jumpProgress = float.Pow(float.InverseLerp(0.2f, 0.75f, stateTimer), 1.3f);
        if (jumpProgress <= 0.8f)
            Scene.Heroes.Susie.ActiveAnimation.ResetFrame();
        else if (Scene.Heroes.Susie.ActiveAnimation.Frame < 1)
            Scene.Heroes.Susie.ActiveAnimation.Frame = 1;

        float delayToNextClash = float.Max(0.25f, 2f / (susieClashImpactCounter * 0.8f + 1f));
        if (susieClashImpactCounter >= maxClashCount - 1)
            delayToNextClash = 1.7f;

        bool performClashEffects = susieClashImpactTimer.Cycle(delayToNextClash) || (jumpProgress >= 1f && susieClashImpactCounter == 0);
        if (jumpProgress >= 1f && !done && performClashEffects)
        {
            Scene.AddScreenShake(9.5f);
            screenWhiteningApproachSpeed = 3f / (susieClashImpactCounter * 0.16f + 1f);
            screenWhiteningDestination = 0.1f + float.InverseLerp(0f, 11f, susieClashImpactCounter) * 0.5f;

            Vector3 clashSpawnPosition = Scene.Heroes.Susie.Position - Vector3.UnitY * 120f;
            Vector2 direction = new Vector2(0.9f, 0.6f).SafeNormalize(Vector2.Zero);
            Scene.ClashParticles.Add(new ClashParticle()
            {
                Texture = Atlases.KnightScene.SusieClashParticle,
                Velocity = Vector3.Zero,
                Position = clashSpawnPosition,
                TotalFrames = 4,
                Animates = true,
            });
            Scene.ClashParticles.Add(new ClashParticle()
            {
                Texture = Atlases.KnightScene.SusieClashParticle,
                Velocity = direction.AsVector3() * 600f,
                Position = clashSpawnPosition,
                TotalFrames = 4,
                Animates = false,
            });

            for (int i = 0; i < 5; i++)
            {
                float afterimageLifetime = 1.1f - i * 0.125f;
                float afterimageTravelDistance = 75f + i * 25f;
                float scaleFactor = 1f - i * 0.1f;
                Scene.RoaringKnight.Afterimages.Add(new KnightAfterimage()
                {
                    RenderState = Scene.RoaringKnight.RenderState,
                    Lifetime = 2f,
                    Position = Scene.RoaringKnight.Position - Vector3.UnitZ * (12f + i),
                    HorizontallyFlip = Scene.RoaringKnight.HorizontallyFlip,
                    Scale = Vector2.One * scaleFactor * Scene.RoaringKnight.Scale,
                    Velocity = direction.AsVector3() * (Scene.RoaringKnight.HorizontallyFlip ? -1f : 1f) * Scene.RoaringKnight.Scale * afterimageTravelDistance / afterimageLifetime
                });
            }

            Scene.RoaringKnight.Rotation = float.Clamp(Scene.RoaringKnight.Rotation + 0.25f, 0f, 0.4f);

            susieClashImpactTimer.Reset();
            susieClashImpactCounter++;
            if (susieClashImpactCounter == maxClashCount)
            {
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("BasicKnightHit.wav"), 1.6f));
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MetalHit.wav"), 1.7f));
                noelleReactionTimer = 0f;
                screenWhiteningApproachSpeed = 6f;
            }
            else
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SusieKnightClash.wav"), 2f));
        }

        if (done)
        {
            float previousReactionTimer = noelleReactionTimer;
            noelleReactionTimer += Scene.Time.DeltaTime;

            if (previousReactionTimer < 1.2f && noelleReactionTimer >= 1.2f)
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("NoelleScaredNew.wav"), 1.5f));

            screenWhiteningDestination = 1f;
        }

        if (jumpProgress < 1f)
            Scene.RoaringKnight.Rotation *= 0.75f;

        if (stateTimer >= 14.05f)
        {
            CreditsScene credits = new CreditsScene(Scene.Game, Scene.SaveData.Slot);
            credits.Initialize();
            Scene.Game.PresentScene(credits);
            return;
        }

        float jumpHeight = 212f;
        Vector2 start = Scene.Heroes.Susie.StartingPosition.AsVector2();
        Vector2 end = Scene.RoaringKnight.Position.AsVector2() + new Vector2(-100f, 48f) + Scene.Rng.NextCircular(2.4f);

        Vector2 midpoint = (start + end) * 0.5f - Vector2.UnitY * jumpHeight;

        float x = float.Lerp(start.X, end.X, jumpProgress);
        float y = MathExtensions.QuadraticInterpolation(start, midpoint, end, x);
        Scene.Heroes.Susie.Position = new Vector3(x, y, Scene.Heroes.Susie.Position.Z);

        Scene.RoaringKnight.Velocity = Scene.Rng.NextCircular(200f).AsVector3();
        Scene.RoaringKnight.Position += Vector3.UnitX * float.Sin(stateTimer * 40f) * jumpProgress.Squared() * 10f;
    }
}
