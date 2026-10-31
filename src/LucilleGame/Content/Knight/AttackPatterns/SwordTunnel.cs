using System.Numerics;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class SwordTunnel : Attack<RoaringKnight>
{
    public readonly record struct Stats(int SwordStandardDamage, int SwordDashDamage, float GrazePoints, float OrbitRadius, float SwordGapSize, float SpinDuration, float SwordDashAnimationDuration, Vector2 BoxSize);

    private bool startedDashAnimations;

    private bool playedDashSound;

    private float spinAngle;

    private float reformTimer;

    private bool teleportedBack;

    private readonly Stats stats;

    private readonly OwnedTimer spawnTimer;

    private readonly OwnedTimer afterimageTimer;

    private readonly OwnedTimer vanishSoundTimer;

    private float Windup => float.InverseLerp(0f, 1.75f, AttackTimer).Squared();

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextSwordTunnel;

    public SwordTunnel(BattleHandler battle, Stats stats) : base(battle)
    {
        spawnTimer = Scene.Time.Rent();
        afterimageTimer = Scene.Time.Rent();
        vanishSoundTimer = Scene.Time.Rent();
        spinAngle = Scene.Rng.NextFloat(float.Tau);

        float bendAccountance = 0.75f;
        spinAngle = -bendAccountance;

        this.stats = stats;
    }

    protected override void UpdateInternal()
    {
        Scene.ScreenDarkening = float.InverseLerp(0f, 1.5f, AttackTimer) * 0.8f;

        // SPIN 2 WIN!!!!
        spinAngle += Scene.Time.DeltaTime * 0.4f;

        // OK so this basically works in such a way
        // that swords summoned by the Knight spin around circularly, with
        // one of the circle's edge points rotating and
        // staying (relatively) attached to the center of
        // the battle box.
        Vector2 origin = Battle.Box.Position - Vector2.UnitY.RotatedBy(spinAngle) * stats.OrbitRadius;

        float afterimagesDuration = 1.2f;
        if (AttackTimer < afterimagesDuration)
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightPointAnimation, 5, loops: false, animatesInReverse: false);
            Attacker.EmitsAfterimages = false;
            EmitDisappearanceAfterimages(afterimagesDuration);

            if (vanishSoundTimer.Cycle(0.1f) && AttackTimer < afterimagesDuration * 0.8f)
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightVanish.wav"), 0.4f));
        }

        if (AttackTimer >= afterimagesDuration * 0.7f)
        {
            if (startedDashAnimations)
            {
                Attacker.Opacity = 1f;
                Attacker.Position = Attacker.SpawnPosition;
                if (!teleportedBack)
                {
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightTeleport.wav"), 0.5f));
                    teleportedBack = true;
                }
            }
            else
                Attacker.Position = Attacker.SpawnPosition + Vector3.UnitY * 100000f;
            Attacker.Velocity = Vector3.Zero;
        }

        if (!startedDashAnimations)
        {
            if (spawnTimer.Cycle(0.11f))
                SummonSwordRingInstance(origin);

            EnforceSpin(origin, stats.OrbitRadius);
            if (AttackTimer >= stats.SpinDuration)
            {
                startedDashAnimations = true;
                foreach (Bullet b in Bullets)
                {
                    if (b is BlackSword sword)
                        sword.StartDashAnimation();
                }
            }
        }
        else
        {
            if (reformTimer >= 0.1f)
                Attacker.RenderState = Attacker.StandardRenderState;
            else
            {
                Attacker.EmitsAfterimages = false;
                Attacker.SetTexture(Atlases.KnightScene.KnightBallFormTransition, 4, loops: false);
                if (Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
                    reformTimer += Scene.Time.DeltaTime;
            }
        }

        if (AttackTimer >= stats.SpinDuration + stats.SwordDashAnimationDuration && !playedDashSound)
        {
            playedDashSound = true;
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SwordDash.wav")));
        }

        if (startedDashAnimations && Bullets.Count <= 0)
            End();
    }

    private void EmitDisappearanceAfterimages(float afterimagesDuration)
    {
        int afterimageCountPerSecond = 20;
        float afterimageProgress = float.InverseLerp(0f, afterimagesDuration, AttackTimer);
        float afterimageLifetime = 2f;
        float afterimageTravelDistance = float.Lerp(132f, 550f, float.Sqrt(afterimageProgress));

        Attacker.Opacity = (1f - afterimageProgress).Cubed();
        Attacker.RepositionDelay = 0.1f;
        Attacker.Velocity = Vector3.Zero;

        if (afterimageTimer.Cycle(afterimageLifetime / afterimageCountPerSecond))
        {
            Attacker.Afterimages.Add(new KnightAfterimage()
            {
                RenderState = Attacker.RenderState,
                Lifetime = afterimageLifetime,
                Position = Attacker.Position + Vector3.UnitZ * (Attacker.Afterimages.Count * 2f + 10f),
                HorizontallyFlip = Attacker.HorizontallyFlip,
                Scale = Vector2.One * Attacker.Scale,
                Velocity = Vector3.UnitX * (Attacker.HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / afterimageLifetime * (1f - afterimageProgress)
            });

            Attacker.Afterimages.Add(new KnightAfterimage()
            {
                RenderState = Attacker.RenderState,
                Lifetime = afterimageLifetime,
                Position = Attacker.Position + Vector3.UnitZ * (Attacker.Afterimages.Count * 2f + 10f),
                HorizontallyFlip = Attacker.HorizontallyFlip,
                Scale = Vector2.One * Attacker.Scale,
                Velocity = Vector3.UnitX * (Attacker.HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / afterimageLifetime * -afterimageProgress * 0.4f
            });
        }
    }

    private void EnforceSpin(Vector2 origin, float r)
    {
        float spinSpeed = 0.25f;
        float swordHeight = Atlases.KnightScene.BlackSword.Area.Height * BlackSword.Scale;
        float wavyVarianceInfluence = float.InverseLerp(1f, 3f, AttackTimer);
        foreach (Bullet b in Bullets)
        {
            if (b is BlackSword sword)
            {
                // If the sword isn't on-screen, nullify wind-up effects
                // until it is, so that the player can see it coming.
                float windUp = Windup;
                bool onscreen = sword.Position.X >= -50f && sword.Position.X <= NorthernlightGame.KnightFightResolution.X + 50f &&
                                sword.Position.Y >= -50f && sword.Position.Y <= NorthernlightGame.KnightFightResolution.Y + 50f;
                if (!onscreen)
                    windUp = 1f;

                float relativeRadius = r;

                // Modulate the effective radius a tiny bit over
                // time. This is done to ensure that the orbiting tunnel
                // doesn't stay EXACTLY on top of the battle box
                // center, since that would let the player just Luigi%
                // and overcome the attack until they start aiming
                // and firing at the end.
                relativeRadius += float.Cos(AttackTimer * 0.45f + sword.Rotation * 3f) * stats.SwordGapSize * wavyVarianceInfluence;

                if (sword.top)
                    relativeRadius -= swordHeight + stats.SwordGapSize;
                else
                    relativeRadius += swordHeight + stats.SwordGapSize;

                sword.PreviousPosition = sword.Position;
                sword.Position = origin + sword.Rotation.Unit * relativeRadius;
                sword.Rotation += float.Tau * Scene.Time.DeltaTime * windUp * spinSpeed;
            }
        }
    }

    private void SummonSwordRingInstance(Vector2 origin)
    {
        Bullets.Add(new BlackSword(Scene, this, stats.SwordStandardDamage, stats.SwordDashDamage, stats.GrazePoints, stats.SwordDashAnimationDuration, true, origin, Vector2.Zero, spinAngle));
        Bullets.Add(new BlackSword(Scene, this, stats.SwordStandardDamage, stats.SwordDashDamage, stats.GrazePoints, stats.SwordDashAnimationDuration, false, origin, Vector2.Zero, spinAngle));
    }
}
