using System.Numerics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Enemies;

/// <summary>
///     The mechanism responsible for the Knight's attack selections.
/// </summary>
public sealed class KnightAttackSelector
{
    private bool performedFirstAttack;

    private bool performedFirstPhase2Attack;

    private bool performedFinalAttack;

    /// <summary>
    ///     The owner of this selector.
    /// </summary>
    public RoaringKnight Owner
    {
        get;
    }

    /// <summary>
    ///     The list of all previous attack types
    ///     chosen by this selector.
    /// </summary>
    public readonly List<RuntimeTypeHandle> PreviousAttackTypes = [];

    public KnightAttackSelector(RoaringKnight owner) => Owner = owner;

    public Attack Choose(BattleHandler battle)
    {
        Attack result;
        KnightFightScene scene = battle.Scene;
        WeightedRandom<Attack> options = new WeightedRandom<Attack>();

        float knightLifeRatio = float.Clamp(Owner.HP / (float)Owner.MaxHP, 0f, 1f);
        float difficulty = 1f - knightLifeRatio;
        if (scene.SideB)
            difficulty += 0.4667f;

        bool phase2 = knightLifeRatio <= 0.6f || scene.BattleHandler.Turn >= 8;
        float daggerDiagonalProbability = 0f;
        BlackDaggerBombardment.Stats daggerStats = new BlackDaggerBombardment.Stats()
        {
            Duration = StatsRepository.BlackDaggerBombardment_Duration,
            GrazePoints = StatsRepository.BlackDaggerBombardment_GrazePoints,
            StartingDaggerSpawnRate =
            scene.SideB ?
                StatsRepository.BlackDaggerBombardment_StartingDaggerSpawnRateSideB :
                StatsRepository.BlackDaggerBombardment_StartingDaggerSpawnRate,

            EndingDaggerSpawnRate =
            scene.SideB ?
            StatsRepository.BlackDaggerBombardment_EndingDaggerSpawnRateSideB :
            StatsRepository.BlackDaggerBombardment_EndingDaggerSpawnRate,

            StartingBoxSize = Vector2.One * StatsRepository.BlackDaggerBombardment_DefaultBoxSize,
            EndingBoxSize = Vector2.One * StatsRepository.BlackDaggerBombardment_VariantAEndingBoxSize,
            DaggerTelegraphDuration = StatsRepository.BlackDaggerBombardment_DaggerTelegraphDuration,
            DaggerDamage = (int)StatsRepository.BlackDaggerBombardment_DaggerDamage,
            BoxTravelOffset = Vector2.Zero,
            DiagonalSpawnProbability = daggerDiagonalProbability
        };
        if (phase2)
        {
            daggerStats = daggerStats with
            {
                EndingBoxSize = Vector2.One * StatsRepository.BlackDaggerBombardment_DefaultBoxSize * 0.76f,
                EndingDaggerSpawnRate = StatsRepository.BlackDaggerBombardment_EndingDaggerSpawnRate * 0.8f
            };
        }
        options.Add(new BlackDaggerBombardment(battle, daggerStats));

        float starsDaggerSpawnRate = phase2 ? StatsRepository.FallingStars_DaggerSpawnRatePhase2 : StatsRepository.FallingStars_DaggerSpawnRate;
        if (scene.SideB)
            starsDaggerSpawnRate /= StatsRepository.FallingStars_DaggerSpawnRateSpeedupSideB;

        options.Add(new FallingStars(battle, new FallingStars.Stats()
        {
            CutSpawnStartTime = StatsRepository.FallingStars_CutSpawnStartTime,
            CutSpawnEndTime = StatsRepository.FallingStars_CutSpawnEndTime,
            CutSpawnGraceTime = StatsRepository.FallingStars_CutSpawnGraceTime,
            StarSummonDelay = StatsRepository.FallingStars_StarSummonDelay,
            StarSummonDuration = StatsRepository.FallingStars_StarSummonDuration,
            GrazePoints = StatsRepository.FallingStars_GrazePoints,
            BoxSize = Vector2.One * StatsRepository.FallingStars_StartingBoxSize,
            ShardDamage = (int)StatsRepository.FallingStars_CutShardDamage,
            StarDamage = (int)StatsRepository.FallingStars_StarDamage,
            DaggerDamage = (int)StatsRepository.FallingStars_DaggerDamage,
            DaggerSpawnRate = starsDaggerSpawnRate,
            Hard = phase2,
            HoverAtSidesCheeseAllowed = !phase2 && !scene.SideB
        }), !performedFirstAttack ? 7000f : 1f);

        float windVerticalBiasing = scene.Rng.NextFloat(0.3f, 0.4f);
        float windSoulPushSpeed = StatsRepository.HowlingWinds_BaseSoulWindPushSpeed;
        if (phase2)
        {
            windVerticalBiasing = scene.Rng.NextFloat(0.45f, 1.23f);
            windSoulPushSpeed *= 1.167f;
        }
        if (scene.SideB)
            windSoulPushSpeed *= 1.167f;

        Vector2 windDirection = new Vector2(-1f, windVerticalBiasing * (scene.Rng.NextBool() ? -1f : 1f));
        options.Add(new HowlingWinds(battle, new HowlingWinds.Stats()
        {
            BoxSize = Vector2.One * StatsRepository.HowlingWinds_BoxSize * float.Lerp(1f, 0.8f, difficulty),
            AttackDuration = StatsRepository.HowlingWinds_Duration * float.Lerp(1f, 0.85f, difficulty),
            ShardSpawnRate = StatsRepository.HowlingWinds_BaseShardSpawnRate * float.Lerp(1f, 0.8f, difficulty),
            WindDirection = windDirection.SafeNormalize(-Vector2.UnitX),
            IceShardGrazePoints = StatsRepository.HowlingWinds_IceShardGrazePoints,
            SoulPushSpeed = windSoulPushSpeed,
            BattleBoxFreezeTouchReboundSpeed = StatsRepository.HowlingWinds_BattleBoxFreezeTouchReboundSpeed,
            IceShardDamage = (int)StatsRepository.HowlingWinds_IceShardDamage,
            BattleBoxFreezeTouchDamage = (int)StatsRepository.HowlingWinds_BattleBoxFreezeTouchDamage,
            IceShardBaseSpeedFactor = StatsRepository.HowlingWinds_IceShardBaseSpeedFactor
        }));

        float swordTunnelSizeFactor = 1f;
        if (phase2)
            swordTunnelSizeFactor *= StatsRepository.SwordTunnel_BoxSizeFactorPhase2;

        if (scene.Rng.NextBool())
        {
            options.Add(new SwordTunnel(battle, new SwordTunnel.Stats()
            {
                SpinDuration = StatsRepository.SwordTunnel_SpinDuration * float.Lerp(1f, 1.25f, difficulty),
                OrbitRadius = StatsRepository.SwordTunnel_OrbitRadius * float.Lerp(1f, 0.7f, difficulty),
                SwordGapSize = StatsRepository.SwordTunnel_SwordGapSize * float.Lerp(1f, 0.85f, difficulty),
                GrazePoints = StatsRepository.SwordTunnel_GrazePoints,
                SwordDashAnimationDuration = StatsRepository.SwordTunnel_SwordDashAnimationDuration,
                SwordStandardDamage = (int)StatsRepository.SwordTunnel_SwordStandardDamage,
                SwordDashDamage = (int)StatsRepository.SwordTunnel_SwordDashDamage,
                BoxSize = new Vector2(StatsRepository.SwordTunnel_BoxSizeX, StatsRepository.SwordTunnel_BoxSizeY) * swordTunnelSizeFactor
            }));
        }
        else
        {
            float bobsToPerform = StatsRepository.SwordTunnelAlt_TotalBobsToPerform;
            if (scene.SideB)
                bobsToPerform = StatsRepository.SwordTunnelAlt_TotalBobsToPerformSideB;
            options.Add(new SwordTunnelAlt(battle, new SwordTunnelAlt.Stats()
            {
                AttackDuration = StatsRepository.SwordTunnelAlt_AttackDuration,
                SwordGapSize = StatsRepository.SwordTunnelAlt_SwordGapSize * float.Lerp(1f, 0.85f, difficulty),
                GrazePoints = StatsRepository.SwordTunnelAlt_GrazePoints,
                SwordDashAnimationDuration = StatsRepository.SwordTunnelAlt_SwordDashAnimationDuration,
                SwordStandardDamage = (int)StatsRepository.SwordTunnelAlt_SwordStandardDamage,
                SwordDashDamage = (int)StatsRepository.SwordTunnelAlt_SwordDashDamage,
                TotalBobsToPerform = bobsToPerform,
                BoxSize = new Vector2(StatsRepository.SwordTunnelAlt_BoxSizeX, StatsRepository.SwordTunnelAlt_BoxSizeY) * swordTunnelSizeFactor,
                StrongerWind = scene.SideB
            }));
        }

        int boxSplitShardCount = (int)StatsRepository.BoxSplit_SplitShardCount;
        if (phase2)
            boxSplitShardCount = (int)StatsRepository.BoxSplit_SplitShardCountPhase2;

        options.Add(new BoxSplit(battle, new BoxSplit.Stats()
        {
            SplitCount = (int)StatsRepository.BoxSplit_TotalSplitsToPerform,
            DiagonalSliceConsiderationProbability = float.Clamp(StatsRepository.BoxSplit_DiagonalSliceConsiderationProbability * float.Lerp(1f, 2f, difficulty), 0f, 0.75f),
            BoxSize = Vector2.One * StatsRepository.BoxSplit_BoxSize * float.Lerp(1f, 0.875f, float.Clamp(difficulty, 0f, 1f)),
            MaxSplitDistance = StatsRepository.BoxSplit_MaxSplitDistance,
            StartingTelegraphDuration = StatsRepository.BoxSplit_StartingTelegraphDuration * float.Lerp(1f, 0.875f, difficulty),
            SliceDuration = StatsRepository.BoxSplit_SliceDuration,
            SplitHoldDuration = StatsRepository.BoxSplit_SplitHoldDuration * float.Lerp(1f, 0.85f, difficulty),
            ShardCount = boxSplitShardCount,
            ShardDamage = (int)StatsRepository.BoxSplit_ShardDamage,
            ShardGrazePoints = StatsRepository.BoxSplit_ShardGrazePoints,
            MaxRepeatSplitDirections = (int)StatsRepository.BoxSplit_MaxRepeatSplitDirections,
            ShardSpeedFactor = 1f + float.Pow(difficulty, 1.64f) * 0.39f,
            CanCreateShardWalls = phase2
        }));

        if (phase2)
        {
            options.Add(new BackgroundBarrage(battle, new BackgroundBarrage.Stats()
            {
                BarrageCount = (int)StatsRepository.BackgroundBarrage_TotalBarragesToDo,
                AverageImpactShardCount = StatsRepository.BackgroundBarrage_ImpactShardCount,
                ShardDamage = (int)StatsRepository.BackgroundBarrage_ShardDamage,
                TotalSwordsOnEachSide = (int)StatsRepository.BackgroundBarrage_TotalSwordsOnEachSide,
                GeneralSpeedupFactor = StatsRepository.BackgroundBarrage_GeneralSpeedupFactor,
                ImpactShardBaseSpeed = scene.SideB ? StatsRepository.BackgroundBarrage_ImpactShardBaseSpeedSideB : StatsRepository.BackgroundBarrage_ImpactShardBaseSpeed,
                BoxSize = new Vector2(1.3f, 1f) * StatsRepository.BackgroundBarrage_BoxSize,
                GrazePoints = StatsRepository.BackgroundBarrage_GrazePoints,
            }), !performedFirstPhase2Attack ? 7000f : 1f);

            float redSoundBiasing = 0f;
            if (scene.Music.Tracks.Length >= 1)
            {
                float blackKnifePlayPosition = (float)scene.Music.Tracks[0].PlayPositionSeconds;

                // The guitar sounds really cool at the end of the song.
                if (blackKnifePlayPosition >= 81f)
                    redSoundBiasing = 33f;

                // There's nothing to really match the guitar to
                // at the start of Black Knife since it's mostly
                // just rhythmic elements, and as such the
                // guitar midi is empty during battle, meaning that
                // trying to start the attack there would result in
                // a whole bunch of Nothing.
                if (blackKnifePlayPosition <= 26f || blackKnifePlayPosition >= 98f)
                    redSoundBiasing = 0f;
            }

            options.Add(new RedSound(battle, new RedSound.Stats()
            {
                Duration = StatsRepository.RedSound_BaseDuration * StatsRepository.RedSound_MaxGameSpeedup,
                BoxSize = new Vector2(StatsRepository.RedSound_BoxSizeX, StatsRepository.RedSound_BoxSizeY),
                MaxGameSpeedup = StatsRepository.RedSound_MaxGameSpeedup,
                NoteGrazePoints = StatsRepository.RedSound_NoteGrazePoints,
                ShockwaveReleaseRate = StatsRepository.RedSound_MiniShockwaveReleaseRate,
                NoteDamage = (int)(scene.SideB ? StatsRepository.RedSound_NoteDamageSideB : StatsRepository.RedSound_NoteDamage),
                RadialSpreadUniformity = StatsRepository.RedSound_RadialSpreadUniformity
            }), redSoundBiasing);

            options.Add(new RedFlurry(battle, new RedFlurry.Stats()
            {
                SlashDamage = (int)StatsRepository.RedFlurry_SlashDamage,
                TotalTurnArounds = (int)StatsRepository.RedFlurry_TotalTurnArounds,
                SlashGrazePoints = StatsRepository.RedFlurry_SlashGrazePoints,
                TelegraphDuration = scene.SideB ? StatsRepository.RedFlurry_TelegraphDurationSideB : StatsRepository.RedFlurry_TelegraphDuration,
                StartingBoxSize = Vector2.One * StatsRepository.RedFlurry_StartingBoxSize,
                EndingBoxSize = Vector2.One * StatsRepository.RedFlurry_EndingBoxSize,
                SpinTurnAroundDuration = scene.SideB ? StatsRepository.RedFlurry_SpinTurnAroundDurationSideB : StatsRepository.RedFlurry_SpinTurnAroundDuration,
                SpinSpeedFactor = scene.SideB ? StatsRepository.RedFlurry_SpinSpeedFactorSideB : StatsRepository.RedFlurry_SpinSpeedFactor
            }), 1.65f);

            int upwardBoltCount = (int)float.Round(StatsRepository.SideFlurry_UpwardFlyingBoltCount);
            float upwardBoltAcceleration = StatsRepository.SideFlurry_UpwardFlyingBoltAcceleration;
            float flySpeedFactor = StatsRepository.SideFlurry_FlySpeedFactor;
            if (scene.SideB)
            {
                upwardBoltCount = (int)float.Round(StatsRepository.SideFlurry_UpwardFlyingBoltCountSideB);
                upwardBoltAcceleration = StatsRepository.SideFlurry_UpwardFlyingBoltAccelerationSideB;
                flySpeedFactor = StatsRepository.SideFlurry_FlySpeedFactorSideB;
            }

            options.Add(new SideFlurry(battle, new SideFlurry.Stats()
            {
                RedBoltDamage = (int)float.Round(StatsRepository.SideFlurry_RedBoltDamage),
                BoltBarrageCount = (int)float.Round(StatsRepository.SideFlurry_BoltBarrageCount),
                UpwardFlyingBoltCount = upwardBoltCount,
                UpwardFlyingBoltAcceleration = upwardBoltAcceleration,
                FlySpeedFactor = flySpeedFactor,
                SkipTauntAnimation = scene.SideB,
                BoxSize = new Vector2(267f, 200f) * StatsRepository.SideFlurry_BoxSizeFactor
            }));

            if (!performedFirstPhase2Attack)
                performedFirstPhase2Attack = true;
        }

        int tries = 0;
        do
        {
            result = options.Get();
            tries++;
            if (tries >= 50)
                break;
        }
        while (PreviousAttackTypes.Contains(result.GetType().TypeHandle));

        if (!performedFirstAttack)
            performedFirstAttack = true;

        bool shouldPerformFinalAttack = knightLifeRatio <= 0.15f;
        if (!performedFinalAttack && shouldPerformFinalAttack)
        {
            performedFinalAttack = true;
            scene.RoaringKnight.FireUpGlow = 0.001f;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightFireUp.wav")));

            scene.RoaringKnight.Afterimages.Clear();

            result = new FinalAttack(battle, new FinalAttack.Stats()
            {
                StarDamage = (int)StatsRepository.FinalAttack_StarDamage,
                WaveDamage = (int)StatsRepository.FinalAttack_WaveDamage,
                MaxKnightSlashSpeedup = StatsRepository.FinalAttack_MaxKnightSlashSpeedup,
                StarSpeedFactor = StatsRepository.FinalAttack_StarSpeedFactor,
                AnimationStartDuration = 1.4f,
                StartingBoxSize = Vector2.One * 250f
            });
            if (scene.SideB)
            {
                result = new FinalAttackSideB(battle, new FinalAttackSideB.Stats()
                {
                    AscentSwordDamage = (int)StatsRepository.FinalAttackSideB_AscentSwordDamage,
                    DaggerDamage = (int)StatsRepository.FinalAttackSideB_DaggerDamage,
                    SlashDamage = (int)StatsRepository.FinalAttackSideB_SlashDamage,
                    AscentDuration = StatsRepository.FinalAttackSideB_AscentDuration,
                    AscentSwordGapSize = StatsRepository.FinalAttackSideB_AscentSwordGapSize,
                    AscentSwordGrazePoints = StatsRepository.FinalAttackSideB_AscentSwordGrazePoints,
                    TotalTurnArounds = (int)StatsRepository.FinalAttackSideB_TotalTurnArounds,
                    SpinSpeedFactor = StatsRepository.FinalAttackSideB_SpinSpeedFactor,
                    SlashGrazePoints = StatsRepository.FinalAttackSideB_SlashGrazePoints,
                    SwordWallWaveFrequency = StatsRepository.FinalAttackSideB_SwordWallWaveFrequency,
                    SwordWallAmplitude = StatsRepository.FinalAttackSideB_SwordWallAmplitude,
                    DaggerSummonDelay = StatsRepository.FinalAttackSideB_DaggerSummonDelay,
                    DaggerSummonRateSpeedupFactor = StatsRepository.FinalAttackSideB_SpinSpeedFactor,
                    DaggerWaveCycleRate = (int)StatsRepository.FinalAttackSideB_DaggerWaveCycleRate,
                    BoxSize = Vector2.One * 99999f,
                });
            }
        }

        int maxAttacksToRemember = phase2 ? 4 : 3;
        PreviousAttackTypes.Add(result.GetType().TypeHandle);
        if (PreviousAttackTypes.Count > maxAttacksToRemember)
            PreviousAttackTypes.RemoveAt(0);

        return result;
    }
}
