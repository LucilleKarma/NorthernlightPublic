using System.Numerics;
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

public sealed class RedFlurry : Attack<RoaringKnight>
{
    public readonly record struct Stats(int SlashDamage, int TotalTurnArounds, float TelegraphDuration, float SlashGrazePoints,
        float SpinTurnAroundDuration, float SpinSpeedFactor, Vector2 StartingBoxSize, Vector2 EndingBoxSize);

    private bool initializedTelegraphStuff;

    private bool initializedSpinStuff;

    private int initialSpinDirection;

    private int turnAroundCounter;

    private float movingSpinSpeed;

    private float idealSpinSpeed;

    private float boxShake;

    private Vector2 spinRotation;

    private Vector2 knightHoverDestination2D;

    private RedFlurryTelegraph telegraph;

    private readonly Stats stats;

    private readonly OwnedTimer sliceTimer;

    private readonly OwnedTimer switchSpinDirectionTimer;

    private Vector3 KnightHoverDestination => new Vector3(knightHoverDestination2D, Attacker.Position.Z);

    public override Vector2 StartingBoxSize => stats.StartingBoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextRedFlurry;

    public RedFlurry(BattleHandler battle, Stats stats) : base(battle)
    {
        sliceTimer = Scene.Time.Rent();
        switchSpinDirectionTimer = Scene.Time.Rent();
        this.stats = stats;
    }

    protected override void UpdateInternal()
    {
        Scene.UILayer.ForceDeferment();
        Scene.SoulLayer.ForceDeferment();
        Scene.BattleLayer.ForceDeferment();
        Scene.DefaultLayer.ForceDeferment();

        bool telegraphing = AttackTimer < stats.TelegraphDuration;
        float duration = stats.TelegraphDuration + stats.SpinTurnAroundDuration * (stats.TotalTurnArounds + 1);
        float overallAttackProgress = float.InverseLerp(stats.TelegraphDuration, duration, AttackTimer);
        Battle.Box.Size = Vector2.Lerp(stats.StartingBoxSize, stats.EndingBoxSize, overallAttackProgress);

        if (telegraphing)
            HandleTelegraphing();
        else
            Attack();

        if (boxShake > 0f)
        {
            Battle.Box.Position = BattleHandler.StartingBoxCenter + Scene.Rng.NextCircular(boxShake);
            if (boxShake > 25f)
                boxShake = 25f;

            boxShake -= Scene.Time.DeltaTime * 15.1f;
        }

        Vector3 idealKnightVelocity = (KnightHoverDestination - Attacker.Position) * 10f;
        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealKnightVelocity, 16f, Scene.Time);
        Attacker.RepositionDelay = 0.2f;
    }

    private void HandleTelegraphing()
    {
        if (!initializedTelegraphStuff)
        {
            initializedTelegraphStuff = true;
            knightHoverDestination2D = Attacker.SpawnPosition.AsVector2() + Scene.Rng.NextCircular(185f) * new Vector2(1f, 0.6f);

            initialSpinDirection = Scene.Rng.NextBool() ? -1 : 1;
            Vector2 startingDirection = Scene.Rng.NextCircularEdge(1f);
            Angle idleTelegraphAngularVelocity = Angle.Pi * 0.4f;
            telegraph = new RedFlurryTelegraph(this, Scene, Battle.Box.Position, startingDirection, initialSpinDirection, stats.TelegraphDuration, idleTelegraphAngularVelocity);
            Bullets.Add(telegraph);

            Scene.AddScreenShake(7.5f);
            boxShake += 5.6f;

            Bullets.Add(new RadialWaveParticle(Scene, Battle.Box, stats.TelegraphDuration * 0.85f, 600f, Battle.Box.Position, new Color(193, 0, 0) * 8.4f)
            {
                ConfinedToBattleBox = true,
                Additive = true
            });
        }

        Attacker.SetTexture(Atlases.KnightScene.KnightSlashFlurryAnticipation, 1);
    }

    private void Attack()
    {
        float timeSinceTelegraph = AttackTimer - stats.TelegraphDuration;
        Scene.ScreenDarkening = float.InverseLerp(0f, 0.5f, timeSinceTelegraph) * 0.85f;

        if (!initializedSpinStuff)
        {
            initializedSpinStuff = true;

            sliceTimer.Reset();
            switchSpinDirectionTimer.Reset();
            spinRotation = telegraph.AimDirection;

            movingSpinSpeed = Angle.Pi * initialSpinDirection * stats.SpinSpeedFactor * 1.2f;
            idealSpinSpeed = movingSpinSpeed;

            Scene.AddScreenShake(24f);
            boxShake += 25f;
        }

        knightHoverDestination2D = Attacker.SpawnPosition.AsVector2() + Scene.Rng.NextCircular(90f);
        Attacker.SetTexture(Atlases.KnightScene.KnightSlashFlurryAnimation, 3);

        if (switchSpinDirectionTimer.Cycle(stats.SpinTurnAroundDuration))
        {
            idealSpinSpeed *= -1f;
            turnAroundCounter++;

            if (turnAroundCounter >= stats.TotalTurnArounds + 1)
            {
                End();
                return;
            }
        }

        movingSpinSpeed = float.LerpDt(movingSpinSpeed, idealSpinSpeed, 2.4f, Scene.Time);
        spinRotation = spinRotation.RotatedBy(movingSpinSpeed * Scene.Time.DeltaTime);

        bool turningAround = float.Abs(idealSpinSpeed - movingSpinSpeed) >= 2f;
        float sliceRate = 0.1f;
        if (turningAround)
            sliceRate *= 2.4f;

        if (sliceTimer.Cycle(sliceRate))
        {
            boxShake += 10f;
            Scene.AddScreenShake(1.4f);

            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), 0.75f));
            Bullets.Add(new RedFlurrySlash(this, Scene, Battle.Box.Position, spinRotation, turningAround ? 0 : stats.SlashDamage, stats.SlashGrazePoints, sliceRate * 2f));

            Vector2 forward = spinRotation;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 spawnPosition = Battle.Box.KeepInside(Battle.Box.Position + forward * i * 400f, Vector2.One);
                for (int j = 0; j < 4; j++)
                {
                    Vector2 velocity = forward.RotatedBy(Scene.Rng.NextFloat(-0.7f, 0.7f)) * i * Scene.Rng.NextFloat(300f, 590f);
                    Bullets.Add(new RedFlurryImpactParticle(Scene, 0.45f, spawnPosition, velocity));
                }
            }
        }
    }
}
