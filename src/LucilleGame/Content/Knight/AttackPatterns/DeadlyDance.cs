using System.Numerics;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class DeadlyDance : Attack<RoaringKnight>
{
    public readonly record struct Stats(float Duration, Vector2 BoxSize);

    private int slashCounter;

    private float sliceProgress;

    private readonly Stats stats;

    private readonly OwnedTimer spawnTimer;

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextDefault;

    public DeadlyDance(BattleHandler battle, Stats stats) : base(battle)
    {
        spawnTimer = Scene.Time.Rent();
        this.stats = stats;
    }

    protected override void UpdateInternal()
    {
        float duration = stats.Duration;
        float attackProgress = float.InverseLerp(0f, 0.75f, AttackTimer / duration);

        Attacker.SlashAnimationSpeedup = 1.5f + attackProgress.Squared() * 9f;

        if (sliceProgress < 0.4f)
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightSlashAnimation, 3, true);
            Attacker.LimitTextureFrame(1);
        }
        else
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightSlashAnimation, 3, true);
            Attacker.LimitTextureFrame(2);
        }

        float sliceTimerIncrement = Scene.Time.DeltaTime * Attacker.SlashAnimationSpeedup / 1.1f;
        sliceProgress = (sliceProgress + sliceTimerIncrement) % 1f;

        Scene.ScreenDarkening = attackProgress.Squared() * 0.95f;

        if (Attacker.JustSlashed)
        {
            slashCounter++;
            Attacker.HorizontallyFlip = slashCounter % 2 == 0;

            float direction = Attacker.HorizontallyFlip ? 1f : -1f;
            Vector2 teleportPosition = Battle.Box.Position + Vector2.UnitX * (Battle.Box.Size.X * 0.5f + 180f) * direction;
            Attacker.Position = new Vector3(teleportPosition, Attacker.Position.Z);
            Attacker.Velocity = Vector3.UnitX * Attacker.Velocity.Length() * direction;

            Battle.Box.CutDirection = Vector2.UnitX;
            Battle.Box.CutDistance += Scene.Rng.NextFloat(40f, 70f);

            bool okStopThatsHurtingMyEyes = Attacker.SlashAnimationSpeedup >= 4.1f;
            Bullets.Add(new BoxCutTelegraph(this, Scene, Battle.Box.Position, Battle.Box.CutDirection, 0.25f, okStopThatsHurtingMyEyes ? 0f : 0.3f));

            int bulletCount = 6;
            if (slashCounter % 2 == 0)
                bulletCount--;

            int bulletDamage = 100;
            float bulletGrazePoints = 2.5f;

            if (spawnTimer.Cycle(0.2f))
            {
                Vector2 origin = Battle.Box.Position + Battle.Box.CutDirection * Scene.Rng.NextFloat(-20f, 20f);
                Vector2 directionToTarget = (TargetPosition - origin).SafeNormalize(Vector2.UnitY);
                for (int i = 0; i < bulletCount; i++)
                {
                    Vector2 bulletDirection = directionToTarget.RotatedBy(float.Pi * i / bulletCount + Scene.Rng.NextFloat(-0.32f, 0.32f));
                    Vector2 bulletVelocity = bulletDirection * Scene.Rng.NextFloat(100f, 500f) * float.Clamp(slashCounter * 0.33f, 0.2f, 1f);
                    Bullets.Add(new Star(Scene, origin, bulletVelocity, bulletDamage, bulletGrazePoints));
                }
            }
        }

        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, Vector3.Zero, 10f, Scene.Time);
        Attacker.RepositionDelay = 0.25f;

        float cutDecrementPerSecond = float.Pow(Attacker.SlashAnimationSpeedup, 0.75f) * 120f;
        Battle.Box.CutDistance = float.Max(0f, Battle.Box.CutDistance - Scene.Time.DeltaTime * cutDecrementPerSecond);

        if (AttackTimer >= duration)
        {
            Attacker.HorizontallyFlip = false;
            Attacker.Position = Attacker.RestingPosition;
            Attacker.Velocity = Vector3.Zero;

            Battle.Box.CutDistance = 0f;
            Battle.Box.CutDirection = Vector2.UnitX;
            End();
        }
    }
}
