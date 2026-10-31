using System.Numerics;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class BladeLineup : Attack<RoaringKnight>
{
    public enum State
    {
        SummonDaggers,
        PointForward,
        Fire,
        End
    }

    private int shotCounter;

    private int waveCounter;

    private int daggerCountAtStartOfWave;

    private Vector2 knightHoverDestination;

    private readonly Queue<AimedBlackDagger> daggers = [];

    /// <summary>
    ///     This attack's current substate.
    /// </summary>
    public State CurrentState
    {
        get;
        set;
    } = State.SummonDaggers;

    public readonly record struct Stats(float DaggerGrazePoints, float DaggerSpread, float TelegraphDuration, int DaggerDamage, Vector2 BoxSize);

    private readonly Stats stats;

    private readonly OwnedTimer timer;

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextDefault;

    public BladeLineup(BattleHandler battle, Stats stats) : base(battle)
    {
        timer = Scene.Time.Rent();
        this.stats = stats;
    }

    protected override void UpdateInternal()
    {
        // Keep the Knight in place during this attack, so
        // that the vertical wave motion doesn't interfere
        // with the spawn positions or anything.
        Attacker.RepositionDelay = 0.24f;

        float intensity = float.InverseLerp(0f, 3f, shotCounter);

        switch (CurrentState)
        {
            case State.SummonDaggers:
                int daggerDamage = stats.DaggerDamage;
                int waveCount = int.Clamp(shotCounter + 1, 1, 3);
                int daggerSummonCount = (int)float.Lerp(4f, 5.4f, intensity) * 2;
                if (waveCounter >= 1)
                    daggerSummonCount--;
                if (waveCounter >= 2)
                    daggerSummonCount -= 2;

                int daggersAddedByWave = daggers.Count - daggerCountAtStartOfWave;
                float daggerGrazePoints = stats.DaggerGrazePoints;
                float daggerSummonRate = float.Lerp(0.046f, 0.024f, intensity);
                float daggerSpread = stats.DaggerSpread;
                float daggerSpawnProgress = daggersAddedByWave / (float)(daggerSummonCount - 1f);
                float pointDelay = 0.06f;
                if (daggersAddedByWave < daggerSummonCount && timer.Cycle(daggerSummonRate))
                {
                    float theta = daggerSpread * float.Lerp(-0.5f, 0.5f, daggerSpawnProgress);
                    Vector2 knightOrigin = Attacker.Position.AsVector2();
                    Vector2 forward = (Battle.Box.Position - knightOrigin).SafeNormalize(-Vector2.UnitX);
                    Vector2 daggerDirection = forward.RotatedBy(theta);
                    Vector2 daggerSpawnPosition = knightOrigin + daggerDirection * 32f;

                    AimedBlackDagger dagger = new AimedBlackDagger(Scene, daggerDamage, daggerGrazePoints, daggerSpawnPosition, daggerDirection)
                    {
                        Velocity = daggerDirection / (waveCounter * 1.5f + 1f) * 1500f,
                        ScaleFactor = 0.7f
                    };
                    Bullets.Add(dagger);
                    daggers.Enqueue(dagger);
                }

                if (daggersAddedByWave >= daggerSummonCount && (timer.Cycle(pointDelay) || waveCounter < waveCount - 2))
                {
                    if (waveCounter < waveCount - 1)
                    {
                        daggerCountAtStartOfWave = daggers.Count;
                        waveCounter++;
                    }
                    else
                    {
                        waveCounter = 0;
                        CurrentState = State.PointForward;
                    }
                }

                Attacker.Velocity = Vector3.Zero;
                knightHoverDestination = Attacker.SpawnPosition.AsVector2() + Scene.Rng.NextCircular(50f) * new Vector2(2f, 1f);
                Attacker.SetTexture(Atlases.KnightScene.KnightPointAnimation, 5, loops: false, animatesInReverse: true);

                break;
            case State.PointForward:
                Attacker.SetTexture(Atlases.KnightScene.KnightPointAnimation, 5, loops: false, animatesInReverse: false);
                if (Attacker.RenderState.CurrentFrame < 4)
                    timer.Reset();
                else
                    CurrentState = State.Fire;

                break;
            case State.Fire:
                float fireRate = float.Lerp(0.125f, 0.07f, intensity);
                float telegraphDuration = stats.TelegraphDuration;
                if (timer.Cycle(fireRate) && daggers.TryDequeue(out AimedBlackDagger? d))
                    d.PrepareToFire(telegraphDuration);

                Vector3 idealKnightVelocity = (new Vector3(knightHoverDestination, Attacker.Position.Z) - Attacker.Position) * 10f;
                Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, idealKnightVelocity, 12.5f, Scene.Time);

                if (Bullets.Count <= 0)
                {
                    shotCounter++;
                    daggerCountAtStartOfWave = 0;
                    CurrentState = State.SummonDaggers;
                    if (intensity >= 1f)
                        CurrentState = State.End;
                }

                break;
            case State.End:
                Attacker.SetTexture(Atlases.KnightScene.KnightPointAnimation, 5, loops: false, animatesInReverse: true);
                if (Attacker.RenderState.CurrentFrame <= 0)
                    End();

                break;
        }
    }
}
