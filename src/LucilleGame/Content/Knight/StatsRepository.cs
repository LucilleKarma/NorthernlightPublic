using System.CodeDom.Compiler;
using Northernlight.Core;

namespace Northernlight.Content.Knight;

[GeneratedCode("NorthernlightStatsRefsBuilder", "1.0.0.0")]
public sealed class StatsRepository
{
    private static string FilePath
    {
        get
        {
            string? environmentDictatedPath = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (environmentDictatedPath is null)
            {
                string currentDirectory = Program.ContentRoot;
                string path = Path.Combine(currentDirectory, "Data", FileName);
                environmentDictatedPath = path;
                Environment.SetEnvironmentVariable(EnvironmentVariableName, path);
            }
            return environmentDictatedPath;
        }
    }
    
    private static readonly ReloadableData<Dictionary<string, float>> stats = new ReloadableData<Dictionary<string, float>>(FilePath);
    
    private const string EnvironmentVariableName = "StatsRepositoryPath";
    
    private const string FileName = "StatsRepository.json";
    
    public static float AngelsBlessingHealAmount_NotRevive => stats.Value["AngelsBlessingHealAmount_NotRevive"];
    
    public static float AngelsBlessingHealAmount_Revive => stats.Value["AngelsBlessingHealAmount_Revive"];
    
    public static float AngelsBlessingHealAmount_SideB => stats.Value["AngelsBlessingHealAmount_SideB"];
    
    public static float AngelsBlessingTensionPointCost => stats.Value["AngelsBlessingTensionPointCost"];
    
    public static float BackgroundBarrage_BoxSize => stats.Value["BackgroundBarrage_BoxSize"];
    
    public static float BackgroundBarrage_GeneralSpeedupFactor => stats.Value["BackgroundBarrage_GeneralSpeedupFactor"];
    
    public static float BackgroundBarrage_GrazePoints => stats.Value["BackgroundBarrage_GrazePoints"];
    
    public static float BackgroundBarrage_ImpactShardBaseSpeed => stats.Value["BackgroundBarrage_ImpactShardBaseSpeed"];
    
    public static float BackgroundBarrage_ImpactShardBaseSpeedSideB => stats.Value["BackgroundBarrage_ImpactShardBaseSpeedSideB"];
    
    public static float BackgroundBarrage_ImpactShardCount => stats.Value["BackgroundBarrage_ImpactShardCount"];
    
    public static float BackgroundBarrage_ShardDamage => stats.Value["BackgroundBarrage_ShardDamage"];
    
    public static float BackgroundBarrage_TotalBarragesToDo => stats.Value["BackgroundBarrage_TotalBarragesToDo"];
    
    public static float BackgroundBarrage_TotalSwordsOnEachSide => stats.Value["BackgroundBarrage_TotalSwordsOnEachSide"];
    
    public static float BlackDaggerBombardment_DaggerDamage => stats.Value["BlackDaggerBombardment_DaggerDamage"];
    
    public static float BlackDaggerBombardment_DaggerTelegraphDuration => stats.Value["BlackDaggerBombardment_DaggerTelegraphDuration"];
    
    public static float BlackDaggerBombardment_DefaultBoxSize => stats.Value["BlackDaggerBombardment_DefaultBoxSize"];
    
    public static float BlackDaggerBombardment_Duration => stats.Value["BlackDaggerBombardment_Duration"];
    
    public static float BlackDaggerBombardment_EndingDaggerSpawnRate => stats.Value["BlackDaggerBombardment_EndingDaggerSpawnRate"];
    
    public static float BlackDaggerBombardment_EndingDaggerSpawnRateSideB => stats.Value["BlackDaggerBombardment_EndingDaggerSpawnRateSideB"];
    
    public static float BlackDaggerBombardment_GrazePoints => stats.Value["BlackDaggerBombardment_GrazePoints"];
    
    public static float BlackDaggerBombardment_StartingDaggerSpawnRate => stats.Value["BlackDaggerBombardment_StartingDaggerSpawnRate"];
    
    public static float BlackDaggerBombardment_StartingDaggerSpawnRateSideB => stats.Value["BlackDaggerBombardment_StartingDaggerSpawnRateSideB"];
    
    public static float BlackDaggerBombardment_VariantAEndingBoxSize => stats.Value["BlackDaggerBombardment_VariantAEndingBoxSize"];
    
    public static float BoxSplit_BoxSize => stats.Value["BoxSplit_BoxSize"];
    
    public static float BoxSplit_DiagonalSliceConsiderationProbability => stats.Value["BoxSplit_DiagonalSliceConsiderationProbability"];
    
    public static float BoxSplit_MaxRepeatSplitDirections => stats.Value["BoxSplit_MaxRepeatSplitDirections"];
    
    public static float BoxSplit_MaxSplitDistance => stats.Value["BoxSplit_MaxSplitDistance"];
    
    public static float BoxSplit_ShardDamage => stats.Value["BoxSplit_ShardDamage"];
    
    public static float BoxSplit_ShardGrazePoints => stats.Value["BoxSplit_ShardGrazePoints"];
    
    public static float BoxSplit_SliceDuration => stats.Value["BoxSplit_SliceDuration"];
    
    public static float BoxSplit_SplitHoldDuration => stats.Value["BoxSplit_SplitHoldDuration"];
    
    public static float BoxSplit_SplitShardCount => stats.Value["BoxSplit_SplitShardCount"];
    
    public static float BoxSplit_SplitShardCountPhase2 => stats.Value["BoxSplit_SplitShardCountPhase2"];
    
    public static float BoxSplit_StartingTelegraphDuration => stats.Value["BoxSplit_StartingTelegraphDuration"];
    
    public static float BoxSplit_TotalSplitsToPerform => stats.Value["BoxSplit_TotalSplitsToPerform"];
    
    public static float BulletDamageFactorSideB => stats.Value["BulletDamageFactorSideB"];
    
    public static float CriticalStrikeTimeWindowSeconds => stats.Value["CriticalStrikeTimeWindowSeconds"];
    
    public static float CrossSlashDamageFactor => stats.Value["CrossSlashDamageFactor"];
    
    public static float CrossSlashTensionPointCost => stats.Value["CrossSlashTensionPointCost"];
    
    public static float FallingStars_CutShardDamage => stats.Value["FallingStars_CutShardDamage"];
    
    public static float FallingStars_CutSpawnEndTime => stats.Value["FallingStars_CutSpawnEndTime"];
    
    public static float FallingStars_CutSpawnGraceTime => stats.Value["FallingStars_CutSpawnGraceTime"];
    
    public static float FallingStars_CutSpawnStartTime => stats.Value["FallingStars_CutSpawnStartTime"];
    
    public static float FallingStars_DaggerDamage => stats.Value["FallingStars_DaggerDamage"];
    
    public static float FallingStars_DaggerSpawnRate => stats.Value["FallingStars_DaggerSpawnRate"];
    
    public static float FallingStars_DaggerSpawnRatePhase2 => stats.Value["FallingStars_DaggerSpawnRatePhase2"];
    
    public static float FallingStars_DaggerSpawnRateSpeedupSideB => stats.Value["FallingStars_DaggerSpawnRateSpeedupSideB"];
    
    public static float FallingStars_GrazePoints => stats.Value["FallingStars_GrazePoints"];
    
    public static float FallingStars_StarDamage => stats.Value["FallingStars_StarDamage"];
    
    public static float FallingStars_StarSummonDelay => stats.Value["FallingStars_StarSummonDelay"];
    
    public static float FallingStars_StarSummonDuration => stats.Value["FallingStars_StarSummonDuration"];
    
    public static float FallingStars_StartingBoxSize => stats.Value["FallingStars_StartingBoxSize"];
    
    public static float FinalAttack_MaxKnightSlashSpeedup => stats.Value["FinalAttack_MaxKnightSlashSpeedup"];
    
    public static float FinalAttack_StarDamage => stats.Value["FinalAttack_StarDamage"];
    
    public static float FinalAttack_StarSpeedFactor => stats.Value["FinalAttack_StarSpeedFactor"];
    
    public static float FinalAttack_WaveDamage => stats.Value["FinalAttack_WaveDamage"];
    
    public static float FinalAttackSideB_AscentDuration => stats.Value["FinalAttackSideB_AscentDuration"];
    
    public static float FinalAttackSideB_AscentSwordDamage => stats.Value["FinalAttackSideB_AscentSwordDamage"];
    
    public static float FinalAttackSideB_AscentSwordGapSize => stats.Value["FinalAttackSideB_AscentSwordGapSize"];
    
    public static float FinalAttackSideB_AscentSwordGrazePoints => stats.Value["FinalAttackSideB_AscentSwordGrazePoints"];
    
    public static float FinalAttackSideB_DaggerDamage => stats.Value["FinalAttackSideB_DaggerDamage"];
    
    public static float FinalAttackSideB_DaggerSummonDelay => stats.Value["FinalAttackSideB_DaggerSummonDelay"];
    
    public static float FinalAttackSideB_DaggerWaveCycleRate => stats.Value["FinalAttackSideB_DaggerWaveCycleRate"];
    
    public static float FinalAttackSideB_SlashDamage => stats.Value["FinalAttackSideB_SlashDamage"];
    
    public static float FinalAttackSideB_SlashGrazePoints => stats.Value["FinalAttackSideB_SlashGrazePoints"];
    
    public static float FinalAttackSideB_SpinSpeedFactor => stats.Value["FinalAttackSideB_SpinSpeedFactor"];
    
    public static float FinalAttackSideB_SwordWallAmplitude => stats.Value["FinalAttackSideB_SwordWallAmplitude"];
    
    public static float FinalAttackSideB_SwordWallWaveFrequency => stats.Value["FinalAttackSideB_SwordWallWaveFrequency"];
    
    public static float FinalAttackSideB_TotalTurnArounds => stats.Value["FinalAttackSideB_TotalTurnArounds"];
    
    public static float GeneralDamageFactor => stats.Value["GeneralDamageFactor"];
    
    public static float GoodHealHealQuantity => stats.Value["GoodHealHealQuantity"];
    
    public static float GoodHealTensionPointCost => stats.Value["GoodHealTensionPointCost"];
    
    public static float HowlingWinds_BaseShardSpawnRate => stats.Value["HowlingWinds_BaseShardSpawnRate"];
    
    public static float HowlingWinds_BaseSoulWindPushSpeed => stats.Value["HowlingWinds_BaseSoulWindPushSpeed"];
    
    public static float HowlingWinds_BattleBoxFreezeTouchDamage => stats.Value["HowlingWinds_BattleBoxFreezeTouchDamage"];
    
    public static float HowlingWinds_BattleBoxFreezeTouchReboundSpeed => stats.Value["HowlingWinds_BattleBoxFreezeTouchReboundSpeed"];
    
    public static float HowlingWinds_BoxSize => stats.Value["HowlingWinds_BoxSize"];
    
    public static float HowlingWinds_Duration => stats.Value["HowlingWinds_Duration"];
    
    public static float HowlingWinds_IceShardBaseSpeedFactor => stats.Value["HowlingWinds_IceShardBaseSpeedFactor"];
    
    public static float HowlingWinds_IceShardDamage => stats.Value["HowlingWinds_IceShardDamage"];
    
    public static float HowlingWinds_IceShardGrazePoints => stats.Value["HowlingWinds_IceShardGrazePoints"];
    
    public static float IceShockDamageFactor => stats.Value["IceShockDamageFactor"];
    
    public static float IceShockDamageFactorSideB => stats.Value["IceShockDamageFactorSideB"];
    
    public static float IceShockTensionPointCost => stats.Value["IceShockTensionPointCost"];
    
    public static float KnightMaxHP => stats.Value["KnightMaxHP"];
    
    public static float KnightMaxHPSideB => stats.Value["KnightMaxHPSideB"];
    
    public static float KrisBaseDamage => stats.Value["KrisBaseDamage"];
    
    public static float KrisMaxHP => stats.Value["KrisMaxHP"];
    
    public static float KrisMaxHPSideB => stats.Value["KrisMaxHPSideB"];
    
    public static float NoelleBaseDamage => stats.Value["NoelleBaseDamage"];
    
    public static float NoelleMaxHP => stats.Value["NoelleMaxHP"];
    
    public static float NoelleMaxHPSideB => stats.Value["NoelleMaxHPSideB"];
    
    public static float PlayerHoldBreathSpeedFactor => stats.Value["PlayerHoldBreathSpeedFactor"];
    
    public static float PlayerMovementSpeed => stats.Value["PlayerMovementSpeed"];
    
    public static float PlayerSlowdownSpeedFactor => stats.Value["PlayerSlowdownSpeedFactor"];
    
    public static float RedFlurry_EndingBoxSize => stats.Value["RedFlurry_EndingBoxSize"];
    
    public static float RedFlurry_SlashDamage => stats.Value["RedFlurry_SlashDamage"];
    
    public static float RedFlurry_SlashGrazePoints => stats.Value["RedFlurry_SlashGrazePoints"];
    
    public static float RedFlurry_SpinSpeedFactor => stats.Value["RedFlurry_SpinSpeedFactor"];
    
    public static float RedFlurry_SpinSpeedFactorSideB => stats.Value["RedFlurry_SpinSpeedFactorSideB"];
    
    public static float RedFlurry_SpinTurnAroundDuration => stats.Value["RedFlurry_SpinTurnAroundDuration"];
    
    public static float RedFlurry_SpinTurnAroundDurationSideB => stats.Value["RedFlurry_SpinTurnAroundDurationSideB"];
    
    public static float RedFlurry_StartingBoxSize => stats.Value["RedFlurry_StartingBoxSize"];
    
    public static float RedFlurry_TelegraphDuration => stats.Value["RedFlurry_TelegraphDuration"];
    
    public static float RedFlurry_TelegraphDurationSideB => stats.Value["RedFlurry_TelegraphDurationSideB"];
    
    public static float RedFlurry_TotalTurnArounds => stats.Value["RedFlurry_TotalTurnArounds"];
    
    public static float RedSound_BaseDuration => stats.Value["RedSound_BaseDuration"];
    
    public static float RedSound_BoxSizeX => stats.Value["RedSound_BoxSizeX"];
    
    public static float RedSound_BoxSizeY => stats.Value["RedSound_BoxSizeY"];
    
    public static float RedSound_MaxGameSpeedup => stats.Value["RedSound_MaxGameSpeedup"];
    
    public static float RedSound_MiniShockwaveReleaseRate => stats.Value["RedSound_MiniShockwaveReleaseRate"];
    
    public static float RedSound_NoteDamage => stats.Value["RedSound_NoteDamage"];
    
    public static float RedSound_NoteDamageSideB => stats.Value["RedSound_NoteDamageSideB"];
    
    public static float RedSound_NoteGrazePoints => stats.Value["RedSound_NoteGrazePoints"];
    
    public static float RedSound_RadialSpreadUniformity => stats.Value["RedSound_RadialSpreadUniformity"];
    
    public static float RudeBusterDamageFactor => stats.Value["RudeBusterDamageFactor"];
    
    public static float RudeBusterTensionPointCost => stats.Value["RudeBusterTensionPointCost"];
    
    public static float SideFlurry_BoltBarrageCount => stats.Value["SideFlurry_BoltBarrageCount"];
    
    public static float SideFlurry_BoxSizeFactor => stats.Value["SideFlurry_BoxSizeFactor"];
    
    public static float SideFlurry_FlySpeedFactor => stats.Value["SideFlurry_FlySpeedFactor"];
    
    public static float SideFlurry_FlySpeedFactorSideB => stats.Value["SideFlurry_FlySpeedFactorSideB"];
    
    public static float SideFlurry_RedBoltDamage => stats.Value["SideFlurry_RedBoltDamage"];
    
    public static float SideFlurry_UpwardFlyingBoltAcceleration => stats.Value["SideFlurry_UpwardFlyingBoltAcceleration"];
    
    public static float SideFlurry_UpwardFlyingBoltAccelerationSideB => stats.Value["SideFlurry_UpwardFlyingBoltAccelerationSideB"];
    
    public static float SideFlurry_UpwardFlyingBoltCount => stats.Value["SideFlurry_UpwardFlyingBoltCount"];
    
    public static float SideFlurry_UpwardFlyingBoltCountSideB => stats.Value["SideFlurry_UpwardFlyingBoltCountSideB"];
    
    public static float SusieBaseDamage => stats.Value["SusieBaseDamage"];
    
    public static float SusieMaxHP => stats.Value["SusieMaxHP"];
    
    public static float SwordTunnel_BoxSizeFactorPhase2 => stats.Value["SwordTunnel_BoxSizeFactorPhase2"];
    
    public static float SwordTunnel_BoxSizeX => stats.Value["SwordTunnel_BoxSizeX"];
    
    public static float SwordTunnel_BoxSizeY => stats.Value["SwordTunnel_BoxSizeY"];
    
    public static float SwordTunnel_GrazePoints => stats.Value["SwordTunnel_GrazePoints"];
    
    public static float SwordTunnel_OrbitRadius => stats.Value["SwordTunnel_OrbitRadius"];
    
    public static float SwordTunnel_SpinDuration => stats.Value["SwordTunnel_SpinDuration"];
    
    public static float SwordTunnel_SwordDashAnimationDuration => stats.Value["SwordTunnel_SwordDashAnimationDuration"];
    
    public static float SwordTunnel_SwordDashDamage => stats.Value["SwordTunnel_SwordDashDamage"];
    
    public static float SwordTunnel_SwordGapSize => stats.Value["SwordTunnel_SwordGapSize"];
    
    public static float SwordTunnel_SwordStandardDamage => stats.Value["SwordTunnel_SwordStandardDamage"];
    
    public static float SwordTunnelAlt_AttackDuration => stats.Value["SwordTunnelAlt_AttackDuration"];
    
    public static float SwordTunnelAlt_BoxSizeX => stats.Value["SwordTunnelAlt_BoxSizeX"];
    
    public static float SwordTunnelAlt_BoxSizeY => stats.Value["SwordTunnelAlt_BoxSizeY"];
    
    public static float SwordTunnelAlt_GrazePoints => stats.Value["SwordTunnelAlt_GrazePoints"];
    
    public static float SwordTunnelAlt_OrbitRadius => stats.Value["SwordTunnelAlt_OrbitRadius"];
    
    public static float SwordTunnelAlt_SwordDashAnimationDuration => stats.Value["SwordTunnelAlt_SwordDashAnimationDuration"];
    
    public static float SwordTunnelAlt_SwordDashDamage => stats.Value["SwordTunnelAlt_SwordDashDamage"];
    
    public static float SwordTunnelAlt_SwordGapSize => stats.Value["SwordTunnelAlt_SwordGapSize"];
    
    public static float SwordTunnelAlt_SwordStandardDamage => stats.Value["SwordTunnelAlt_SwordStandardDamage"];
    
    public static float SwordTunnelAlt_TotalBobsToPerform => stats.Value["SwordTunnelAlt_TotalBobsToPerform"];
    
    public static float SwordTunnelAlt_TotalBobsToPerformSideB => stats.Value["SwordTunnelAlt_TotalBobsToPerformSideB"];
}
