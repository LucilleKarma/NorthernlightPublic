using System.Numerics;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Content.Knight.Particles;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class SideFlurry : Attack<RoaringKnight>
{
    public enum Substate
    {
        Taunt,
        FlyAndReleaseBolts,
        HoverAboveBattleBox,
        StrikeBattleBox,
        ReturnToRestingPosition
    }

    public readonly record struct Stats(int RedBoltDamage, int UpwardFlyingBoltCount, int BoltBarrageCount, bool SkipTauntAnimation, float UpwardFlyingBoltAcceleration, float FlySpeedFactor, Vector2 BoxSize);

    private int teleportCounter;

    private float animationTransitionTimer;

    private float boxAngularVelocity;

    private bool beganTauntFraming;

    private bool createdBoxImpactEffects;

    private Vector2 boxVelocity;

    private Substate currentSubstate;

    private readonly Stats stats;

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextSideFlurry;

    public SideFlurry(BattleHandler battle, Stats stats) : base(battle)
    {
        this.stats = stats;
    }

    protected override void UpdateInternal()
    {
        Attacker.RepositionDelay = 0.2f;

        switch (currentSubstate)
        {
            case Substate.Taunt:
                DoBehavior_Taunt();
                break;
            case Substate.FlyAndReleaseBolts:
                DoBehavior_FlyAndReleaseBolts();
                break;
            case Substate.HoverAboveBattleBox:
                DoBehavior_HoverAboveBattleBox();
                break;
            case Substate.StrikeBattleBox:
                DoBehavior_StrikeBattleBox();
                break;
            case Substate.ReturnToRestingPosition:
                DoBehavior_ReturnToRestingPosition();
                break;
        }

        boxAngularVelocity = float.LerpDt(boxAngularVelocity, 0f, 40f, Scene.Time);
        boxVelocity = Vector2.LerpDt(boxVelocity, Vector2.Zero, 12f, Scene.Time);
        Battle.Box.Rotation += boxAngularVelocity * Scene.Time.DeltaTime;
        Battle.Box.Position += boxVelocity * Scene.Time.DeltaTime;

        if (Battle.Box.ShatterDistance > 0f)
        {
            Vector3 soulPosition = Battle.Soul.Position;
            Vector2 restrictedSoulPosition = FallingStars.KeepPlayerInsideGame(Scene, soulPosition.AsVector2(), Vector2.One * Soul.Scale * 20f);
            Battle.Soul.Position = new Vector3(restrictedSoulPosition, Battle.Soul.Position.Z);
        }
    }

    private void DoBehavior_Taunt()
    {
        Attacker.HorizontallyFlip = true;

        AtlasEntry ballTransition = Atlases.KnightScene.KnightBallFormTransition;
        AtlasEntry ballTransitionForward = Atlases.KnightScene.KnightBallFormTransitionForward;
        AtlasEntry ball = Atlases.KnightScene.KnightBallFormAnimation;
        AtlasEntry taunt = Atlases.KnightScene.KnightTaunt;

        if (!beganTauntFraming)
        {
            beganTauntFraming = true;
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightTeleport.wav"), 0.7f));
            Attacker.SetTexture(ballTransition, 4, loops: false, animatesInReverse: true);
        }

        bool endOfFirstBallAnimation = Attacker.RenderState.Texture == ballTransition && Attacker.RenderState.CurrentFrame <= 0 && Attacker.RenderState.AnimatesInReverse;
        bool endOfSecondBallAnimation = Attacker.RenderState.Texture == ballTransitionForward && Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1 && !Attacker.RenderState.AnimatesInReverse;
        if (endOfFirstBallAnimation)
        {
            animationTransitionTimer += Scene.Time.DeltaTime;
            if (animationTransitionTimer >= 0.1f)
            {
                Attacker.SetTexture(ball, 5);
                animationTransitionTimer = 0f;
            }
        }
        if (endOfSecondBallAnimation)
        {
            animationTransitionTimer += Scene.Time.DeltaTime;
            if (animationTransitionTimer >= 0.1f)
            {
                animationTransitionTimer = 0f;
                if (stats.SkipTauntAnimation)
                {
                    currentSubstate = Substate.FlyAndReleaseBolts;
                    AttackTimer.Reset();
                    return;
                }

                Attacker.SetTexture(taunt, 5);
            }
        }

        if (Attacker.RenderState.Texture == ball)
        {
            float hoverHeightAboveBox = 136f;
            Vector2 hoverDestination = Battle.Box.Position - Vector2.UnitY * (Battle.Box.Size.Y * 0.5f + hoverHeightAboveBox);
            Vector3 idealVelocity = (hoverDestination - Attacker.Position.AsVector2()).AsVector3() * 4f;
            Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealVelocity, 30f, Scene.Time);

            if (Vector2.Distance(Attacker.Position.AsVector2(), hoverDestination) <= 40f)
            {
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightTeleport.wav"), 0.7f));
                Attacker.SetTexture(ballTransitionForward, 5, loops: false, animatesInReverse: false);
            }
        }

        if (Attacker.RenderState.Texture == taunt)
        {
            Vector3 idealVelocity = Vector3.UnitY * float.Sin(AttackTimer * 2f) * 75f;
            Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealVelocity, 30f, Scene.Time);

            if (AttackTimer >= 1.4f)
            {
                currentSubstate = Substate.FlyAndReleaseBolts;
                AttackTimer.Reset();
            }
        }
        else
            AttackTimer.Reset();
    }

    private void DoBehavior_FlyAndReleaseBolts()
    {
        float boxDescent = float.Smoothstep(float.InverseLerp(0f, 4f, AttackTimer)) * 60f;
        Vector2 idealBoxPosition = BattleHandler.StartingBoxCenter + Vector2.UnitY * boxDescent;
        boxVelocity = (idealBoxPosition - Battle.Box.Position) * 5.1f;

        AtlasEntry forwardPoseWithOpenEye = Atlases.KnightScene.KnightOpenEyeForward;
        AtlasEntry forwardPoseWithOpenEyeSlash = Atlases.KnightScene.KnightOpenEyeForwardSlash;
        if (Attacker.RenderState.Texture != forwardPoseWithOpenEye && Attacker.RenderState.Texture != forwardPoseWithOpenEyeSlash)
            Attacker.SetTexture(forwardPoseWithOpenEye, 6, loops: false);
        if (Attacker.RenderState.Texture == forwardPoseWithOpenEye && Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
            Attacker.SetTexture(forwardPoseWithOpenEyeSlash, 6, loops: true);

        int direction = 1;
        if (teleportCounter % 2 == 1)
            direction *= -1;

        float xSpeed = 1850f + float.Clamp(AttackTimer * 500f, 0f, 1200f);
        xSpeed *= float.InverseLerp(0f, 2.1f, AttackTimer);
        xSpeed *= stats.FlySpeedFactor;

        Vector3 idealVelocity = new Vector3(xSpeed * direction, xSpeed * -0.184f, 0f);
        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealVelocity, 16f, Scene.Time);
        Attacker.AfterimageCountPerSecondOverride = 100;
        Attacker.Rotation = Attacker.Velocity.X / 19000f;
        Attacker.SlashAnimationSpeedup = 1.9f + xSpeed / 1100f;
        Attacker.FrameUpdateRateSpeedup = 1.65f;
        Attacker.HorizontallyFlip = direction == 1;
        if (Attacker.JustSlashed && teleportCounter < stats.BoltBarrageCount - 1)
        {
            Scene.AddScreenShake(7f);
            for (int i = 0; i < 5; i++)
            {
                Vector2 bulletVelocity = Scene.Rng.NextCircular(400f);
                bulletVelocity.Y += Scene.Rng.NextFloat(380f);

                Angle bulletRotation = Scene.Rng.NextFloat(Angle.Tau);
                Bullets.Add(new RedBolt(Scene, Attacker.Position.AsVector2(), bulletVelocity, bulletRotation, stats.RedBoltDamage, 5f, 0f));
            }

            // Apply sufficient wind forces to affect the
            // battle box slightly.
            boxAngularVelocity += direction * 2.4f;
            boxVelocity.X += direction * 300f;
        }

        if (Attacker.Position.X <= -350f ||
            Attacker.Position.X >= NorthernlightGame.KnightFightResolution.X + 350f)
        {
            float x = -120f;
            if (direction == 1)
                x = NorthernlightGame.KnightFightResolution.X - x;

            Attacker.Position = new Vector3(x, NorthernlightGame.KnightFightResolution.Y * 0.3f, Attacker.Position.Z);
            Attacker.Velocity = Vector3.Zero;

            teleportCounter++;
            if (teleportCounter >= stats.BoltBarrageCount)
            {
                currentSubstate = Substate.HoverAboveBattleBox;
                AttackTimer.Reset();
            }
        }
    }

    private void DoBehavior_HoverAboveBattleBox()
    {
        float riseProgress = float.InverseLerp(0.3f, 1f, AttackTimer);
        float hoverHeightAboveBox = 124f + riseProgress.Squared() * 205f;
        Vector2 hoverDestination = Battle.Box.Position - Vector2.UnitY * (Battle.Box.Size.Y * 0.5f + hoverHeightAboveBox);
        Vector3 idealVelocity = (hoverDestination - Attacker.Position.AsVector2()).AsVector3() * 4f;
        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealVelocity, 30f, Scene.Time);
        Attacker.Rotation = Attacker.Rotation.StepTowards(0f, Scene.Time.DeltaTime * 2f);
        Attacker.HorizontallyFlip = false;
        Attacker.FrameUpdateRateSpeedup = 0.8f;

        if (riseProgress >= 0.8f)
        {
            AtlasEntry anticipation = Atlases.KnightScene.KnightSwordThrowAnticipation;
            Attacker.SetTexture(anticipation, 5, loops: false);
        }
        else
        {
            AtlasEntry forwardPoseWithOpenEye = Atlases.KnightScene.KnightOpenEyeForward;
            Attacker.SetTexture(forwardPoseWithOpenEye, 6, loops: false, animatesInReverse: true);
        }

        if (AttackTimer >= 1.4f)
        {
            AttackTimer.Reset();
            currentSubstate = Substate.StrikeBattleBox;

            boxAngularVelocity = (Scene.Rng.NextBool() ? -1f : 1f) * 240f;

            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), 0.75f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightDaggerDash.wav"), 0.8f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightAppearanceDash.wav"), 1.2f));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightDaggerDash.wav")));

            Attacker.Velocity = Vector3.UnitY * 5000f;
            boxVelocity += Vector2.UnitY * 750f;
        }
    }

    private void DoBehavior_StrikeBattleBox()
    {
        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, Vector3.Zero, 40f, Scene.Time);

        if (AttackTimer >= 0.1f && !createdBoxImpactEffects)
        {
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightDaggerDash.wav")));
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("BattleBoxBreakBurst.wav")));

            Scene.AddScreenShake(32f);

            createdBoxImpactEffects = true;
            Vector2 shockwaveSpawnPosition = Attacker.Position.AsVector2() + Vector2.UnitY * 120f;

            for (int i = 0; i < 4; i++)
                Bullets.Add(new ShockwaveParticle(Scene, 0.5f - i * 0.12f, 600f + i * 95f, shockwaveSpawnPosition));

            Vector2 bulletSpawnPosition = shockwaveSpawnPosition + Vector2.UnitY * 100f;
            for (int i = 0; i < stats.UpwardFlyingBoltCount; i++)
            {
                Vector2 bulletVelocity = new Vector2(Scene.Rng.NextFloat(-600f, 600f), Scene.Rng.NextFloat(450f, 850f));

                Angle bulletRotation = Scene.Rng.NextFloat(Angle.Tau);
                Bullets.Add(new RedBolt(Scene, bulletSpawnPosition, bulletVelocity, bulletRotation, stats.RedBoltDamage, 5f, stats.UpwardFlyingBoltAcceleration));
            }

            Angle rotation = -0.41f + Angle.PiOver2;
            Scene.ClashParticles.Add(new ClashParticle()
            {
                Texture = Atlases.KnightScene.SusieClashParticle,
                Velocity = Vector3.Zero,
                Position = shockwaveSpawnPosition.AsVector3(),
                TotalFrames = 4,
                Animates = true,
                Rotation = rotation
            });
        }

        if (AttackTimer >= 0.3f)
        {
            AtlasEntry forwardPoseWithOpenEyeSlash = Atlases.KnightScene.KnightOpenEyeForwardSlash;
            Attacker.SetTexture(forwardPoseWithOpenEyeSlash, 6, loops: false, animatesInReverse: true);
            Attacker.FrameUpdateRateSpeedup = 1.67f;

            if (Battle.Box.CutAngles.Count <= 0)
            {
                Battle.Box.ShatterDistance = 0.001f;
                Battle.Box.Size = Vector2.One * 3000f;
                for (int i = 0; i < 6; i++)
                    Battle.Box.CutAngles.Add(float.Tau * (i + 0.5f) / 6f);
            }
        }
        if (Battle.Box.ShatterDistance > 0f)
            Battle.Box.ShatterDistance += (Battle.Box.ShatterDistance * 14.93f + 9.3f) * Scene.Time.DeltaTime;

        Attacker.Position += Scene.Rng.NextCircular(75f).AsVector3() * Scene.Time.DeltaTime;

        if (AttackTimer >= 2.6f)
        {
            currentSubstate = Substate.ReturnToRestingPosition;
            AttackTimer.Reset();
        }
    }

    private void DoBehavior_ReturnToRestingPosition()
    {
        AtlasEntry forwardPoseWithOpenEyeSlash = Atlases.KnightScene.KnightOpenEyeForwardSlash;
        AtlasEntry ballTransitionForward = Atlases.KnightScene.KnightBallFormTransitionForward;
        AtlasEntry ball = Atlases.KnightScene.KnightBallFormAnimation;
        AtlasEntry ballTransition = Atlases.KnightScene.KnightBallFormTransition;

        Attacker.EmitsAfterimages = false;
        Attacker.FrameUpdateRateSpeedup = 1f;

        if (Attacker.RenderState.Texture == forwardPoseWithOpenEyeSlash)
        {
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightTeleport.wav"), 0.7f));
            Attacker.SetTexture(ballTransitionForward, 5, loops: false, animatesInReverse: true);
        }
        if (Attacker.RenderState.Texture == ballTransitionForward && Attacker.RenderState.CurrentFrame <= 0)
            Attacker.SetTexture(ball, 5);
        if (Attacker.RenderState.Texture == ball)
        {
            Attacker.RepositionDelay = 0f;
            Attacker.Position += (Attacker.RestingPosition - Attacker.Position).SafeNormalize(Vector3.Zero) * Scene.Time.DeltaTime * 400f;

            if (Vector3.Distance(Attacker.Position, Attacker.RestingPosition) <= 70f)
            {
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightTeleport.wav"), 0.7f));
                Attacker.SetTexture(ballTransition, 4, loops: false);
            }
        }
        if (Attacker.RenderState.Texture == ballTransition && Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
        {
            animationTransitionTimer += Scene.Time.DeltaTime;
            if (animationTransitionTimer >= 0.1f)
            {
                Battle.Box.CutAngles.Clear();
                Battle.Box.ClearAfterimages(false);
                Battle.Box.Size = Vector2.Zero;
                End();
            }
        }
    }
}
