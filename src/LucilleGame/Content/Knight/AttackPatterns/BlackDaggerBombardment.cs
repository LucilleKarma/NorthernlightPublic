using System.Numerics;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class BlackDaggerBombardment : Attack<RoaringKnight>
{
    public readonly record struct Stats(float Duration, float GrazePoints, float StartingDaggerSpawnRate, float EndingDaggerSpawnRate,
        float DaggerTelegraphDuration, float DiagonalSpawnProbability, int DaggerDamage, Vector2 StartingBoxSize, Vector2 EndingBoxSize, Vector2 BoxTravelOffset);

    private Vector2 previousAimDirection;

    private readonly Stats stats;

    private readonly OwnedTimer spawnTimer;

    public override Vector2 StartingBoxSize => stats.StartingBoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextDefault;

    public BlackDaggerBombardment(BattleHandler battle, Stats stats) : base(battle)
    {
        spawnTimer = Scene.Time.Rent();
        this.stats = stats;
    }

    protected override void UpdateInternal()
    {
        float duration = stats.Duration;
        float attackProgress = AttackTimer / duration;
        float easedSpawnRateProgress = float.Pow(float.InverseLerp(0f, 0.8f, attackProgress), 1.25f);
        float daggerSpawnRate = float.Lerp(stats.StartingDaggerSpawnRate, stats.EndingDaggerSpawnRate, easedSpawnRateProgress);

        if (spawnTimer.Cycle(daggerSpawnRate) && attackProgress <= 0.9f)
        {
            int daggerDamage = stats.DaggerDamage;
            float spawnOffset = 120f;

            // This is mostly just done to relax the RNG and
            // prevent cases where entire wall sections of
            // the bullet box get covered in daggers.
            Vector2 spawnPosition;
            Vector2 aimDirection;
            do
            {
                Vector2 offsetDirection = (float.Tau * Scene.Rng.Next(4) / 4).ToUnit();
                if (Scene.Rng.NextBool(stats.DiagonalSpawnProbability))
                    offsetDirection = offsetDirection.RotatedBy(Angle.PiOver4);
                spawnPosition = TargetPosition + offsetDirection * spawnOffset;

                aimDirection = (TargetPosition - spawnPosition).SafeNormalize(Vector2.UnitY);
            }
            while (Vector2.Dot(aimDirection, previousAimDirection) >= 0.99f);

            Bullets.Add(new BlackDagger(this, Scene, daggerDamage, stats.GrazePoints, spawnPosition, aimDirection, 50f, telegraphDuration: stats.DaggerTelegraphDuration));

            previousAimDirection = aimDirection;
        }

        Vector2 boxVelocity = stats.BoxTravelOffset / stats.Duration * Scene.Time.DeltaTime;
        Battle.Box.Position += boxVelocity;

        Battle.Box.Size = Vector2.Lerp(stats.StartingBoxSize, stats.EndingBoxSize, attackProgress);

        if (attackProgress >= 1f)
            End();
    }
}
