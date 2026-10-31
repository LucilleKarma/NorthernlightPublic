using System.CodeDom.Compiler;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

[GeneratedCode("AtlasGenerator", "1.0.0.0")]
public static class Atlases
{
    private static GraphicsDevice device;

    private static CommandBuffer.CopyPassScope activeCopyScope;

    internal static readonly Dictionary<string, Lazy<Atlas>> CreatedAtlases = [];

    public sealed class ChapterSelect
    {
        public static readonly Lazy<Atlas> Atlas = Register(nameof(ChapterSelect), [
            new Texture(@"chapterIcon.png", device),
            new Texture(@"heart.png", device),
            new Texture(@"horzBar.png", device),
            new Texture(@"text_register.png", device),
            new Texture(@"ui_crystal.png", device),
            new Texture(@"ui_dot.png", device),
            new Texture(@"ui_star.png", device),
            new Texture(@"vertBar.png", device),
        ]);

        public static AtlasEntry chapterIcon => Atlas.Value.Entries[new Atlas.AtlasKey("chapterIcon", 0)];

        public static AtlasEntry heart => Atlas.Value.Entries[new Atlas.AtlasKey("heart", 1)];

        public static AtlasEntry horzBar => Atlas.Value.Entries[new Atlas.AtlasKey("horzBar", 2)];

        public static AtlasEntry text_register => Atlas.Value.Entries[new Atlas.AtlasKey("text_register", 3)];

        public static AtlasEntry ui_crystal => Atlas.Value.Entries[new Atlas.AtlasKey("ui_crystal", 4)];

        public static AtlasEntry ui_dot => Atlas.Value.Entries[new Atlas.AtlasKey("ui_dot", 5)];

        public static AtlasEntry ui_star => Atlas.Value.Entries[new Atlas.AtlasKey("ui_star", 6)];

        public static AtlasEntry vertBar => Atlas.Value.Entries[new Atlas.AtlasKey("vertBar", 7)];
    }
    public sealed class KnightScene
    {
        public static readonly Lazy<Atlas> Atlas = Register(nameof(KnightScene), [
            new Texture(@"ActButton.png", device),
            new Texture(@"ActIcon.png", device),
            new Texture(@"AttackBarReticleThing.png", device),
            new Texture(@"AttackBarReticleThingGlow.png", device),
            new Texture(@"AttackBoxInner.png", device),
            new Texture(@"AttackBoxOuter.png", device),
            new Texture(@"AttackPressIndicator.png", device),
            new Texture(@"BattleBoxFlames.png", device),
            new Texture(@"BattleBoxForShatter.png", device),
            new Texture(@"BattleBoxResidue.png", device),
            new Texture(@"BattleBoxShard.png", device),
            new Texture(@"BattleBoxShardGlow.png", device),
            new Texture(@"BattleMenuHPDivider.png", device),
            new Texture(@"BattleMenuHPText.png", device),
            new Texture(@"BattleMenuIcon_Kris.png", device),
            new Texture(@"BattleMenuIcon_KrisDowned.png", device),
            new Texture(@"BattleMenuIcon_KrisHurt.png", device),
            new Texture(@"BattleMenuIcon_Noelle.png", device),
            new Texture(@"BattleMenuIcon_NoelleDowned.png", device),
            new Texture(@"BattleMenuIcon_NoelleHurt.png", device),
            new Texture(@"BattleMenuIcon_Susie.png", device),
            new Texture(@"BattleMenuIcon_SusieDowned.png", device),
            new Texture(@"BattleMenuIcon_SusieHurt.png", device),
            new Texture(@"BattleMenuName_Kris.png", device),
            new Texture(@"BattleMenuName_Noelle.png", device),
            new Texture(@"BattleMenuName_Susie.png", device),
            new Texture(@"BattleMenuPortraitDelimiter.png", device),
            new Texture(@"BlackDagger.png", device),
            new Texture(@"BlackDaggerAfterimage.png", device),
            new Texture(@"BlackDaggerWhite.png", device),
            new Texture(@"BlackSword.png", device),
            new Texture(@"Cracks.png", device),
            new Texture(@"CriticalStrikeStar.png", device),
            new Texture(@"Cut.png", device),
            new Texture(@"DefendButton.png", device),
            new Texture(@"DefendIcon.png", device),
            new Texture(@"DentProfiles.png", device),
            new Texture(@"DialogueBoxArrow.png", device),
            new Texture(@"DialogueBoxBottomLeft.png", device),
            new Texture(@"DialogueBoxBottomRight.png", device),
            new Texture(@"DialogueBoxTopLeft.png", device),
            new Texture(@"DialogueBoxTopRight.png", device),
            new Texture(@"FightButton.png", device),
            new Texture(@"FightIcon.png", device),
            new Texture(@"FountainBall.png", device),
            new Texture(@"Guitar.png", device),
            new Texture(@"HitImpactKris.png", device),
            new Texture(@"HitImpactNoelle.png", device),
            new Texture(@"HitImpactSusie.png", device),
            new Texture(@"IceShard.png", device),
            new Texture(@"IceShockSnowflake.png", device),
            new Texture(@"IsoscelesTriangle.png", device),
            new Texture(@"ItemButton.png", device),
            new Texture(@"ItemIcon.png", device),
            new Texture(@"KnightAuraFarmPose.png", device),
            new Texture(@"KnightBallFormAnimation.png", device),
            new Texture(@"KnightBallFormTransition.png", device),
            new Texture(@"KnightBallFormTransitionForward.png", device),
            new Texture(@"KnightBallFormTransitionForwardBladeless.png", device),
            new Texture(@"KnightBat.png", device),
            new Texture(@"KnightBladeAttack.png", device),
            new Texture(@"KnightFakeDeath.png", device),
            new Texture(@"KnightFlyingAnimation.png", device),
            new Texture(@"KnightFlyingAnimationFast.png", device),
            new Texture(@"KnightFlyingTransition.png", device),
            new Texture(@"KnightFrozen.png", device),
            new Texture(@"KnightGuitarHoldArm.png", device),
            new Texture(@"KnightGuitarHoldBody.png", device),
            new Texture(@"KnightGuitarHoldForearm.png", device),
            new Texture(@"KnightGuitarStrikeAnimation.png", device),
            new Texture(@"KnightGuitarStrikeAnimationArms.png", device),
            new Texture(@"KnightHesitant.png", device),
            new Texture(@"KnightHurt.png", device),
            new Texture(@"KnightIdle.png", device),
            new Texture(@"KnightIdleWhite.png", device),
            new Texture(@"KnightImpalingAttack.png", device),
            new Texture(@"KnightOpenEyeForward.png", device),
            new Texture(@"KnightOpenEyeForwardSlash.png", device),
            new Texture(@"KnightPointAnimation.png", device),
            new Texture(@"KnightSlashAnimation.png", device),
            new Texture(@"KnightSlashFlurryAnimation.png", device),
            new Texture(@"KnightSlashFlurryAnticipation.png", device),
            new Texture(@"KnightSlashVisual.png", device),
            new Texture(@"KnightSwordMaterializationAnimation.png", device),
            new Texture(@"KnightSwordThrow.png", device),
            new Texture(@"KnightSwordThrowAnticipation.png", device),
            new Texture(@"KnightTaunt.png", device),
            new Texture(@"KnightWeak.png", device),
            new Texture(@"KrisAct.png", device),
            new Texture(@"KrisActEnd.png", device),
            new Texture(@"KrisAttack.png", device),
            new Texture(@"KrisAttackAnticipation.png", device),
            new Texture(@"KrisAttackCrossSlash.png", device),
            new Texture(@"KrisDefend.png", device),
            new Texture(@"KrisDefendWindHair.png", device),
            new Texture(@"KrisDowned.png", device),
            new Texture(@"KrisHurt.png", device),
            new Texture(@"KrisIdle.png", device),
            new Texture(@"KrisItemPreparation.png", device),
            new Texture(@"KrisItemUse.png", device),
            new Texture(@"KrisStandStill.png", device),
            new Texture(@"KrisStandStillLookingDown.png", device),
            new Texture(@"KrisThink.png", device),
            new Texture(@"KrisWalk.png", device),
            new Texture(@"MagicButton.png", device),
            new Texture(@"MagicIcon.png", device),
            new Texture(@"Max.png", device),
            new Texture(@"MercyDenial.png", device),
            new Texture(@"Miss.png", device),
            new Texture(@"NoelleAppearSideB.png", device),
            new Texture(@"NoelleAttackAnticipation.png", device),
            new Texture(@"NoelleAttackNormal.png", device),
            new Texture(@"NoelleDefend.png", device),
            new Texture(@"NoelleDefendSideB.png", device),
            new Texture(@"NoelleDowned.png", device),
            new Texture(@"NoelleDownedSideB.png", device),
            new Texture(@"NoelleHurt.png", device),
            new Texture(@"NoelleHurtSideB.png", device),
            new Texture(@"NoelleIdle.png", device),
            new Texture(@"NoelleIdleSideB.png", device),
            new Texture(@"NoelleItemPreparation.png", device),
            new Texture(@"NoelleItemUse.png", device),
            new Texture(@"NoelleMagicCast.png", device),
            new Texture(@"NoelleStandStill.png", device),
            new Texture(@"NoelleStandStillSideB.png", device),
            new Texture(@"Pixel.png", device),
            new Texture(@"QuestionMarkTimesThree.png", device),
            new Texture(@"QuitText.png", device),
            new Texture(@"RadialBurst.png", device),
            new Texture(@"Rainbow.png", device),
            new Texture(@"RedBolt.png", device),
            new Texture(@"RedNoteEighth.png", device),
            new Texture(@"RedNoteHalf.png", device),
            new Texture(@"RedNoteQuarter.png", device),
            new Texture(@"RudeBusterImpact.png", device),
            new Texture(@"RudeBusterShot.png", device),
            new Texture(@"Shockwave.png", device),
            new Texture(@"SlashAnimation.png", device),
            new Texture(@"SlashMarker.png", device),
            new Texture(@"SnowgraveSnowParticles.png", device),
            new Texture(@"Soul.png", device),
            new Texture(@"SoulBlur.png", device),
            new Texture(@"SoulEnterVisual.png", device),
            new Texture(@"SoulGrazeInner.png", device),
            new Texture(@"SoulGrazeOuter.png", device),
            new Texture(@"SpareButton.png", device),
            new Texture(@"SpareIconA.png", device),
            new Texture(@"SpareIconB.png", device),
            new Texture(@"Star.png", device),
            new Texture(@"StarBullet.png", device),
            new Texture(@"StarExplosion.png", device),
            new Texture(@"StarGlow.png", device),
            new Texture(@"StarPart.png", device),
            new Texture(@"SusieAttackAnticipation.png", device),
            new Texture(@"SusieAttackNormal.png", device),
            new Texture(@"SusieAxeAnticipation.png", device),
            new Texture(@"SusieClashParticle.png", device),
            new Texture(@"SusieDefend.png", device),
            new Texture(@"SusieDowned.png", device),
            new Texture(@"SusieHeal.png", device),
            new Texture(@"SusieHealAnticipation.png", device),
            new Texture(@"SusieHurt.png", device),
            new Texture(@"SusieIdle.png", device),
            new Texture(@"SusieItemPreparation.png", device),
            new Texture(@"SusieItemUse.png", device),
            new Texture(@"SusieKnightClash.png", device),
            new Texture(@"SusiePoint.png", device),
            new Texture(@"SusieRudeBuster.png", device),
            new Texture(@"SusieStandStill.png", device),
            new Texture(@"SweetSweetHealParticle.png", device),
            new Texture(@"Swoon.png", device),
            new Texture(@"TensionPointBar.png", device),
            new Texture(@"TensionPointBarOutline.png", device),
            new Texture(@"TensionPointNumber.png", device),
            new Texture(@"TensionPointNumberless.png", device),
            new Texture(@"TobyWhyIsThisNotAFont.png", device),
            new Texture(@"Triangle.png", device),
            new Texture(@"Wave.png", device),
        ]);

        public static AtlasEntry ActButton => Atlas.Value.Entries[new Atlas.AtlasKey("ActButton", 0)];

        public static AtlasEntry ActIcon => Atlas.Value.Entries[new Atlas.AtlasKey("ActIcon", 1)];

        public static AtlasEntry AttackBarReticleThing => Atlas.Value.Entries[new Atlas.AtlasKey("AttackBarReticleThing", 2)];

        public static AtlasEntry AttackBarReticleThingGlow => Atlas.Value.Entries[new Atlas.AtlasKey("AttackBarReticleThingGlow", 3)];

        public static AtlasEntry AttackBoxInner => Atlas.Value.Entries[new Atlas.AtlasKey("AttackBoxInner", 4)];

        public static AtlasEntry AttackBoxOuter => Atlas.Value.Entries[new Atlas.AtlasKey("AttackBoxOuter", 5)];

        public static AtlasEntry AttackPressIndicator => Atlas.Value.Entries[new Atlas.AtlasKey("AttackPressIndicator", 6)];

        public static AtlasEntry BattleBoxFlames => Atlas.Value.Entries[new Atlas.AtlasKey("BattleBoxFlames", 7)];

        public static AtlasEntry BattleBoxForShatter => Atlas.Value.Entries[new Atlas.AtlasKey("BattleBoxForShatter", 8)];

        public static AtlasEntry BattleBoxResidue => Atlas.Value.Entries[new Atlas.AtlasKey("BattleBoxResidue", 9)];

        public static AtlasEntry BattleBoxShard => Atlas.Value.Entries[new Atlas.AtlasKey("BattleBoxShard", 10)];

        public static AtlasEntry BattleBoxShardGlow => Atlas.Value.Entries[new Atlas.AtlasKey("BattleBoxShardGlow", 11)];

        public static AtlasEntry BattleMenuHPDivider => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuHPDivider", 12)];

        public static AtlasEntry BattleMenuHPText => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuHPText", 13)];

        public static AtlasEntry BattleMenuIcon_Kris => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_Kris", 14)];

        public static AtlasEntry BattleMenuIcon_KrisDowned => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_KrisDowned", 15)];

        public static AtlasEntry BattleMenuIcon_KrisHurt => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_KrisHurt", 16)];

        public static AtlasEntry BattleMenuIcon_Noelle => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_Noelle", 17)];

        public static AtlasEntry BattleMenuIcon_NoelleDowned => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_NoelleDowned", 18)];

        public static AtlasEntry BattleMenuIcon_NoelleHurt => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_NoelleHurt", 19)];

        public static AtlasEntry BattleMenuIcon_Susie => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_Susie", 20)];

        public static AtlasEntry BattleMenuIcon_SusieDowned => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_SusieDowned", 21)];

        public static AtlasEntry BattleMenuIcon_SusieHurt => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuIcon_SusieHurt", 22)];

        public static AtlasEntry BattleMenuName_Kris => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuName_Kris", 23)];

        public static AtlasEntry BattleMenuName_Noelle => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuName_Noelle", 24)];

        public static AtlasEntry BattleMenuName_Susie => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuName_Susie", 25)];

        public static AtlasEntry BattleMenuPortraitDelimiter => Atlas.Value.Entries[new Atlas.AtlasKey("BattleMenuPortraitDelimiter", 26)];

        public static AtlasEntry BlackDagger => Atlas.Value.Entries[new Atlas.AtlasKey("BlackDagger", 27)];

        public static AtlasEntry BlackDaggerAfterimage => Atlas.Value.Entries[new Atlas.AtlasKey("BlackDaggerAfterimage", 28)];

        public static AtlasEntry BlackDaggerWhite => Atlas.Value.Entries[new Atlas.AtlasKey("BlackDaggerWhite", 29)];

        public static AtlasEntry BlackSword => Atlas.Value.Entries[new Atlas.AtlasKey("BlackSword", 30)];

        public static AtlasEntry Cracks => Atlas.Value.Entries[new Atlas.AtlasKey("Cracks", 31)];

        public static AtlasEntry CriticalStrikeStar => Atlas.Value.Entries[new Atlas.AtlasKey("CriticalStrikeStar", 32)];

        public static AtlasEntry Cut => Atlas.Value.Entries[new Atlas.AtlasKey("Cut", 33)];

        public static AtlasEntry DefendButton => Atlas.Value.Entries[new Atlas.AtlasKey("DefendButton", 34)];

        public static AtlasEntry DefendIcon => Atlas.Value.Entries[new Atlas.AtlasKey("DefendIcon", 35)];

        public static AtlasEntry DentProfiles => Atlas.Value.Entries[new Atlas.AtlasKey("DentProfiles", 36)];

        public static AtlasEntry DialogueBoxArrow => Atlas.Value.Entries[new Atlas.AtlasKey("DialogueBoxArrow", 37)];

        public static AtlasEntry DialogueBoxBottomLeft => Atlas.Value.Entries[new Atlas.AtlasKey("DialogueBoxBottomLeft", 38)];

        public static AtlasEntry DialogueBoxBottomRight => Atlas.Value.Entries[new Atlas.AtlasKey("DialogueBoxBottomRight", 39)];

        public static AtlasEntry DialogueBoxTopLeft => Atlas.Value.Entries[new Atlas.AtlasKey("DialogueBoxTopLeft", 40)];

        public static AtlasEntry DialogueBoxTopRight => Atlas.Value.Entries[new Atlas.AtlasKey("DialogueBoxTopRight", 41)];

        public static AtlasEntry FightButton => Atlas.Value.Entries[new Atlas.AtlasKey("FightButton", 42)];

        public static AtlasEntry FightIcon => Atlas.Value.Entries[new Atlas.AtlasKey("FightIcon", 43)];

        public static AtlasEntry FountainBall => Atlas.Value.Entries[new Atlas.AtlasKey("FountainBall", 44)];

        public static AtlasEntry Guitar => Atlas.Value.Entries[new Atlas.AtlasKey("Guitar", 45)];

        public static AtlasEntry HitImpactKris => Atlas.Value.Entries[new Atlas.AtlasKey("HitImpactKris", 46)];

        public static AtlasEntry HitImpactNoelle => Atlas.Value.Entries[new Atlas.AtlasKey("HitImpactNoelle", 47)];

        public static AtlasEntry HitImpactSusie => Atlas.Value.Entries[new Atlas.AtlasKey("HitImpactSusie", 48)];

        public static AtlasEntry IceShard => Atlas.Value.Entries[new Atlas.AtlasKey("IceShard", 49)];

        public static AtlasEntry IceShockSnowflake => Atlas.Value.Entries[new Atlas.AtlasKey("IceShockSnowflake", 50)];

        public static AtlasEntry IsoscelesTriangle => Atlas.Value.Entries[new Atlas.AtlasKey("IsoscelesTriangle", 51)];

        public static AtlasEntry ItemButton => Atlas.Value.Entries[new Atlas.AtlasKey("ItemButton", 52)];

        public static AtlasEntry ItemIcon => Atlas.Value.Entries[new Atlas.AtlasKey("ItemIcon", 53)];

        public static AtlasEntry KnightAuraFarmPose => Atlas.Value.Entries[new Atlas.AtlasKey("KnightAuraFarmPose", 54)];

        public static AtlasEntry KnightBallFormAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightBallFormAnimation", 55)];

        public static AtlasEntry KnightBallFormTransition => Atlas.Value.Entries[new Atlas.AtlasKey("KnightBallFormTransition", 56)];

        public static AtlasEntry KnightBallFormTransitionForward => Atlas.Value.Entries[new Atlas.AtlasKey("KnightBallFormTransitionForward", 57)];

        public static AtlasEntry KnightBallFormTransitionForwardBladeless => Atlas.Value.Entries[new Atlas.AtlasKey("KnightBallFormTransitionForwardBladeless", 58)];

        public static AtlasEntry KnightBat => Atlas.Value.Entries[new Atlas.AtlasKey("KnightBat", 59)];

        public static AtlasEntry KnightBladeAttack => Atlas.Value.Entries[new Atlas.AtlasKey("KnightBladeAttack", 60)];

        public static AtlasEntry KnightFakeDeath => Atlas.Value.Entries[new Atlas.AtlasKey("KnightFakeDeath", 61)];

        public static AtlasEntry KnightFlyingAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightFlyingAnimation", 62)];

        public static AtlasEntry KnightFlyingAnimationFast => Atlas.Value.Entries[new Atlas.AtlasKey("KnightFlyingAnimationFast", 63)];

        public static AtlasEntry KnightFlyingTransition => Atlas.Value.Entries[new Atlas.AtlasKey("KnightFlyingTransition", 64)];

        public static AtlasEntry KnightFrozen => Atlas.Value.Entries[new Atlas.AtlasKey("KnightFrozen", 65)];

        public static AtlasEntry KnightGuitarHoldArm => Atlas.Value.Entries[new Atlas.AtlasKey("KnightGuitarHoldArm", 66)];

        public static AtlasEntry KnightGuitarHoldBody => Atlas.Value.Entries[new Atlas.AtlasKey("KnightGuitarHoldBody", 67)];

        public static AtlasEntry KnightGuitarHoldForearm => Atlas.Value.Entries[new Atlas.AtlasKey("KnightGuitarHoldForearm", 68)];

        public static AtlasEntry KnightGuitarStrikeAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightGuitarStrikeAnimation", 69)];

        public static AtlasEntry KnightGuitarStrikeAnimationArms => Atlas.Value.Entries[new Atlas.AtlasKey("KnightGuitarStrikeAnimationArms", 70)];

        public static AtlasEntry KnightHesitant => Atlas.Value.Entries[new Atlas.AtlasKey("KnightHesitant", 71)];

        public static AtlasEntry KnightHurt => Atlas.Value.Entries[new Atlas.AtlasKey("KnightHurt", 72)];

        public static AtlasEntry KnightIdle => Atlas.Value.Entries[new Atlas.AtlasKey("KnightIdle", 73)];

        public static AtlasEntry KnightIdleWhite => Atlas.Value.Entries[new Atlas.AtlasKey("KnightIdleWhite", 74)];

        public static AtlasEntry KnightImpalingAttack => Atlas.Value.Entries[new Atlas.AtlasKey("KnightImpalingAttack", 75)];

        public static AtlasEntry KnightOpenEyeForward => Atlas.Value.Entries[new Atlas.AtlasKey("KnightOpenEyeForward", 76)];

        public static AtlasEntry KnightOpenEyeForwardSlash => Atlas.Value.Entries[new Atlas.AtlasKey("KnightOpenEyeForwardSlash", 77)];

        public static AtlasEntry KnightPointAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightPointAnimation", 78)];

        public static AtlasEntry KnightSlashAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightSlashAnimation", 79)];

        public static AtlasEntry KnightSlashFlurryAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightSlashFlurryAnimation", 80)];

        public static AtlasEntry KnightSlashFlurryAnticipation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightSlashFlurryAnticipation", 81)];

        public static AtlasEntry KnightSlashVisual => Atlas.Value.Entries[new Atlas.AtlasKey("KnightSlashVisual", 82)];

        public static AtlasEntry KnightSwordMaterializationAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightSwordMaterializationAnimation", 83)];

        public static AtlasEntry KnightSwordThrow => Atlas.Value.Entries[new Atlas.AtlasKey("KnightSwordThrow", 84)];

        public static AtlasEntry KnightSwordThrowAnticipation => Atlas.Value.Entries[new Atlas.AtlasKey("KnightSwordThrowAnticipation", 85)];

        public static AtlasEntry KnightTaunt => Atlas.Value.Entries[new Atlas.AtlasKey("KnightTaunt", 86)];

        public static AtlasEntry KnightWeak => Atlas.Value.Entries[new Atlas.AtlasKey("KnightWeak", 87)];

        public static AtlasEntry KrisAct => Atlas.Value.Entries[new Atlas.AtlasKey("KrisAct", 88)];

        public static AtlasEntry KrisActEnd => Atlas.Value.Entries[new Atlas.AtlasKey("KrisActEnd", 89)];

        public static AtlasEntry KrisAttack => Atlas.Value.Entries[new Atlas.AtlasKey("KrisAttack", 90)];

        public static AtlasEntry KrisAttackAnticipation => Atlas.Value.Entries[new Atlas.AtlasKey("KrisAttackAnticipation", 91)];

        public static AtlasEntry KrisAttackCrossSlash => Atlas.Value.Entries[new Atlas.AtlasKey("KrisAttackCrossSlash", 92)];

        public static AtlasEntry KrisDefend => Atlas.Value.Entries[new Atlas.AtlasKey("KrisDefend", 93)];

        public static AtlasEntry KrisDefendWindHair => Atlas.Value.Entries[new Atlas.AtlasKey("KrisDefendWindHair", 94)];

        public static AtlasEntry KrisDowned => Atlas.Value.Entries[new Atlas.AtlasKey("KrisDowned", 95)];

        public static AtlasEntry KrisHurt => Atlas.Value.Entries[new Atlas.AtlasKey("KrisHurt", 96)];

        public static AtlasEntry KrisIdle => Atlas.Value.Entries[new Atlas.AtlasKey("KrisIdle", 97)];

        public static AtlasEntry KrisItemPreparation => Atlas.Value.Entries[new Atlas.AtlasKey("KrisItemPreparation", 98)];

        public static AtlasEntry KrisItemUse => Atlas.Value.Entries[new Atlas.AtlasKey("KrisItemUse", 99)];

        public static AtlasEntry KrisStandStill => Atlas.Value.Entries[new Atlas.AtlasKey("KrisStandStill", 100)];

        public static AtlasEntry KrisStandStillLookingDown => Atlas.Value.Entries[new Atlas.AtlasKey("KrisStandStillLookingDown", 101)];

        public static AtlasEntry KrisThink => Atlas.Value.Entries[new Atlas.AtlasKey("KrisThink", 102)];

        public static AtlasEntry KrisWalk => Atlas.Value.Entries[new Atlas.AtlasKey("KrisWalk", 103)];

        public static AtlasEntry MagicButton => Atlas.Value.Entries[new Atlas.AtlasKey("MagicButton", 104)];

        public static AtlasEntry MagicIcon => Atlas.Value.Entries[new Atlas.AtlasKey("MagicIcon", 105)];

        public static AtlasEntry Max => Atlas.Value.Entries[new Atlas.AtlasKey("Max", 106)];

        public static AtlasEntry MercyDenial => Atlas.Value.Entries[new Atlas.AtlasKey("MercyDenial", 107)];

        public static AtlasEntry Miss => Atlas.Value.Entries[new Atlas.AtlasKey("Miss", 108)];

        public static AtlasEntry NoelleAppearSideB => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleAppearSideB", 109)];

        public static AtlasEntry NoelleAttackAnticipation => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleAttackAnticipation", 110)];

        public static AtlasEntry NoelleAttackNormal => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleAttackNormal", 111)];

        public static AtlasEntry NoelleDefend => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleDefend", 112)];

        public static AtlasEntry NoelleDefendSideB => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleDefendSideB", 113)];

        public static AtlasEntry NoelleDowned => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleDowned", 114)];

        public static AtlasEntry NoelleDownedSideB => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleDownedSideB", 115)];

        public static AtlasEntry NoelleHurt => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleHurt", 116)];

        public static AtlasEntry NoelleHurtSideB => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleHurtSideB", 117)];

        public static AtlasEntry NoelleIdle => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleIdle", 118)];

        public static AtlasEntry NoelleIdleSideB => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleIdleSideB", 119)];

        public static AtlasEntry NoelleItemPreparation => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleItemPreparation", 120)];

        public static AtlasEntry NoelleItemUse => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleItemUse", 121)];

        public static AtlasEntry NoelleMagicCast => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleMagicCast", 122)];

        public static AtlasEntry NoelleStandStill => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleStandStill", 123)];

        public static AtlasEntry NoelleStandStillSideB => Atlas.Value.Entries[new Atlas.AtlasKey("NoelleStandStillSideB", 124)];

        public static AtlasEntry Pixel => Atlas.Value.Entries[new Atlas.AtlasKey("Pixel", 125)];

        public static AtlasEntry QuestionMarkTimesThree => Atlas.Value.Entries[new Atlas.AtlasKey("QuestionMarkTimesThree", 126)];

        public static AtlasEntry QuitText => Atlas.Value.Entries[new Atlas.AtlasKey("QuitText", 127)];

        public static AtlasEntry RadialBurst => Atlas.Value.Entries[new Atlas.AtlasKey("RadialBurst", 128)];

        public static AtlasEntry Rainbow => Atlas.Value.Entries[new Atlas.AtlasKey("Rainbow", 129)];

        public static AtlasEntry RedBolt => Atlas.Value.Entries[new Atlas.AtlasKey("RedBolt", 130)];

        public static AtlasEntry RedNoteEighth => Atlas.Value.Entries[new Atlas.AtlasKey("RedNoteEighth", 131)];

        public static AtlasEntry RedNoteHalf => Atlas.Value.Entries[new Atlas.AtlasKey("RedNoteHalf", 132)];

        public static AtlasEntry RedNoteQuarter => Atlas.Value.Entries[new Atlas.AtlasKey("RedNoteQuarter", 133)];

        public static AtlasEntry RudeBusterImpact => Atlas.Value.Entries[new Atlas.AtlasKey("RudeBusterImpact", 134)];

        public static AtlasEntry RudeBusterShot => Atlas.Value.Entries[new Atlas.AtlasKey("RudeBusterShot", 135)];

        public static AtlasEntry Shockwave => Atlas.Value.Entries[new Atlas.AtlasKey("Shockwave", 136)];

        public static AtlasEntry SlashAnimation => Atlas.Value.Entries[new Atlas.AtlasKey("SlashAnimation", 137)];

        public static AtlasEntry SlashMarker => Atlas.Value.Entries[new Atlas.AtlasKey("SlashMarker", 138)];

        public static AtlasEntry SnowgraveSnowParticles => Atlas.Value.Entries[new Atlas.AtlasKey("SnowgraveSnowParticles", 139)];

        public static AtlasEntry Soul => Atlas.Value.Entries[new Atlas.AtlasKey("Soul", 140)];

        public static AtlasEntry SoulBlur => Atlas.Value.Entries[new Atlas.AtlasKey("SoulBlur", 141)];

        public static AtlasEntry SoulEnterVisual => Atlas.Value.Entries[new Atlas.AtlasKey("SoulEnterVisual", 142)];

        public static AtlasEntry SoulGrazeInner => Atlas.Value.Entries[new Atlas.AtlasKey("SoulGrazeInner", 143)];

        public static AtlasEntry SoulGrazeOuter => Atlas.Value.Entries[new Atlas.AtlasKey("SoulGrazeOuter", 144)];

        public static AtlasEntry SpareButton => Atlas.Value.Entries[new Atlas.AtlasKey("SpareButton", 145)];

        public static AtlasEntry SpareIconA => Atlas.Value.Entries[new Atlas.AtlasKey("SpareIconA", 146)];

        public static AtlasEntry SpareIconB => Atlas.Value.Entries[new Atlas.AtlasKey("SpareIconB", 147)];

        public static AtlasEntry Star => Atlas.Value.Entries[new Atlas.AtlasKey("Star", 148)];

        public static AtlasEntry StarBullet => Atlas.Value.Entries[new Atlas.AtlasKey("StarBullet", 149)];

        public static AtlasEntry StarExplosion => Atlas.Value.Entries[new Atlas.AtlasKey("StarExplosion", 150)];

        public static AtlasEntry StarGlow => Atlas.Value.Entries[new Atlas.AtlasKey("StarGlow", 151)];

        public static AtlasEntry StarPart => Atlas.Value.Entries[new Atlas.AtlasKey("StarPart", 152)];

        public static AtlasEntry SusieAttackAnticipation => Atlas.Value.Entries[new Atlas.AtlasKey("SusieAttackAnticipation", 153)];

        public static AtlasEntry SusieAttackNormal => Atlas.Value.Entries[new Atlas.AtlasKey("SusieAttackNormal", 154)];

        public static AtlasEntry SusieAxeAnticipation => Atlas.Value.Entries[new Atlas.AtlasKey("SusieAxeAnticipation", 155)];

        public static AtlasEntry SusieClashParticle => Atlas.Value.Entries[new Atlas.AtlasKey("SusieClashParticle", 156)];

        public static AtlasEntry SusieDefend => Atlas.Value.Entries[new Atlas.AtlasKey("SusieDefend", 157)];

        public static AtlasEntry SusieDowned => Atlas.Value.Entries[new Atlas.AtlasKey("SusieDowned", 158)];

        public static AtlasEntry SusieHeal => Atlas.Value.Entries[new Atlas.AtlasKey("SusieHeal", 159)];

        public static AtlasEntry SusieHealAnticipation => Atlas.Value.Entries[new Atlas.AtlasKey("SusieHealAnticipation", 160)];

        public static AtlasEntry SusieHurt => Atlas.Value.Entries[new Atlas.AtlasKey("SusieHurt", 161)];

        public static AtlasEntry SusieIdle => Atlas.Value.Entries[new Atlas.AtlasKey("SusieIdle", 162)];

        public static AtlasEntry SusieItemPreparation => Atlas.Value.Entries[new Atlas.AtlasKey("SusieItemPreparation", 163)];

        public static AtlasEntry SusieItemUse => Atlas.Value.Entries[new Atlas.AtlasKey("SusieItemUse", 164)];

        public static AtlasEntry SusieKnightClash => Atlas.Value.Entries[new Atlas.AtlasKey("SusieKnightClash", 165)];

        public static AtlasEntry SusiePoint => Atlas.Value.Entries[new Atlas.AtlasKey("SusiePoint", 166)];

        public static AtlasEntry SusieRudeBuster => Atlas.Value.Entries[new Atlas.AtlasKey("SusieRudeBuster", 167)];

        public static AtlasEntry SusieStandStill => Atlas.Value.Entries[new Atlas.AtlasKey("SusieStandStill", 168)];

        public static AtlasEntry SweetSweetHealParticle => Atlas.Value.Entries[new Atlas.AtlasKey("SweetSweetHealParticle", 169)];

        public static AtlasEntry Swoon => Atlas.Value.Entries[new Atlas.AtlasKey("Swoon", 170)];

        public static AtlasEntry TensionPointBar => Atlas.Value.Entries[new Atlas.AtlasKey("TensionPointBar", 171)];

        public static AtlasEntry TensionPointBarOutline => Atlas.Value.Entries[new Atlas.AtlasKey("TensionPointBarOutline", 172)];

        public static AtlasEntry TensionPointNumber => Atlas.Value.Entries[new Atlas.AtlasKey("TensionPointNumber", 173)];

        public static AtlasEntry TensionPointNumberless => Atlas.Value.Entries[new Atlas.AtlasKey("TensionPointNumberless", 174)];

        public static AtlasEntry TobyWhyIsThisNotAFont => Atlas.Value.Entries[new Atlas.AtlasKey("TobyWhyIsThisNotAFont", 175)];

        public static AtlasEntry Triangle => Atlas.Value.Entries[new Atlas.AtlasKey("Triangle", 176)];

        public static AtlasEntry Wave => Atlas.Value.Entries[new Atlas.AtlasKey("Wave", 177)];
    }
    public sealed class LargeTextures
    {
        public static readonly Lazy<Atlas> Atlas = Register(nameof(LargeTextures), [
            new Texture(@"Aurora.png", device),
            new Texture(@"CloudyNoise.png", device),
            new Texture(@"DarkFountain.png", device),
            new Texture(@"DepthsBall.png", device),
            new Texture(@"DepthsBallFull.png", device),
            new Texture(@"DepthsBallSea.png", device),
            new Texture(@"DepthsBallSeaFull.png", device),
            new Texture(@"Foreground.png", device),
            new Texture(@"FrostNoise_OmitPadding.png", device),
            new Texture(@"Glow.png", device),
            new Texture(@"IMAGE_DEPTH_OmitPadding.png", device),
            new Texture(@"OverlayOpaque.png", device),
            new Texture(@"OverlayTranslucent.png", device),
            new Texture(@"PerlinNoise_OmitPadding.png", device),
            new Texture(@"SemiRandom_OmitPadding.png", device),
            new Texture(@"Sky.png", device),
            new Texture(@"SnowgraveGradient.png", device),
            new Texture(@"SoulShine.png", device),
            new Texture(@"WindLines.png", device),
        ]);

        public static AtlasEntry Aurora => Atlas.Value.Entries[new Atlas.AtlasKey("Aurora", 0)];

        public static AtlasEntry CloudyNoise => Atlas.Value.Entries[new Atlas.AtlasKey("CloudyNoise", 1)];

        public static AtlasEntry DarkFountain => Atlas.Value.Entries[new Atlas.AtlasKey("DarkFountain", 2)];

        public static AtlasEntry DepthsBall => Atlas.Value.Entries[new Atlas.AtlasKey("DepthsBall", 3)];

        public static AtlasEntry DepthsBallFull => Atlas.Value.Entries[new Atlas.AtlasKey("DepthsBallFull", 4)];

        public static AtlasEntry DepthsBallSea => Atlas.Value.Entries[new Atlas.AtlasKey("DepthsBallSea", 5)];

        public static AtlasEntry DepthsBallSeaFull => Atlas.Value.Entries[new Atlas.AtlasKey("DepthsBallSeaFull", 6)];

        public static AtlasEntry Foreground => Atlas.Value.Entries[new Atlas.AtlasKey("Foreground", 7)];

        public static AtlasEntry FrostNoise_OmitPadding => Atlas.Value.Entries[new Atlas.AtlasKey("FrostNoise_OmitPadding", 8)];

        public static AtlasEntry Glow => Atlas.Value.Entries[new Atlas.AtlasKey("Glow", 9)];

        public static AtlasEntry IMAGE_DEPTH_OmitPadding => Atlas.Value.Entries[new Atlas.AtlasKey("IMAGE_DEPTH_OmitPadding", 10)];

        public static AtlasEntry OverlayOpaque => Atlas.Value.Entries[new Atlas.AtlasKey("OverlayOpaque", 11)];

        public static AtlasEntry OverlayTranslucent => Atlas.Value.Entries[new Atlas.AtlasKey("OverlayTranslucent", 12)];

        public static AtlasEntry PerlinNoise_OmitPadding => Atlas.Value.Entries[new Atlas.AtlasKey("PerlinNoise_OmitPadding", 13)];

        public static AtlasEntry SemiRandom_OmitPadding => Atlas.Value.Entries[new Atlas.AtlasKey("SemiRandom_OmitPadding", 14)];

        public static AtlasEntry Sky => Atlas.Value.Entries[new Atlas.AtlasKey("Sky", 15)];

        public static AtlasEntry SnowgraveGradient => Atlas.Value.Entries[new Atlas.AtlasKey("SnowgraveGradient", 16)];

        public static AtlasEntry SoulShine => Atlas.Value.Entries[new Atlas.AtlasKey("SoulShine", 17)];

        public static AtlasEntry WindLines => Atlas.Value.Entries[new Atlas.AtlasKey("WindLines", 18)];
    }
    public sealed class Logo
    {
        public static readonly Lazy<Atlas> Atlas = Register(nameof(Logo), [
            new Texture(@"IMAGE_LOGO.png", device),
            new Texture(@"Noise_OmitPadding.png", device),
            new Texture(@"PerlinNoise_OmitPadding.png", device),
        ]);

        public static AtlasEntry IMAGE_LOGO => Atlas.Value.Entries[new Atlas.AtlasKey("IMAGE_LOGO", 0)];

        public static AtlasEntry Noise_OmitPadding => Atlas.Value.Entries[new Atlas.AtlasKey("Noise_OmitPadding", 1)];

        public static AtlasEntry PerlinNoise_OmitPadding => Atlas.Value.Entries[new Atlas.AtlasKey("PerlinNoise_OmitPadding", 2)];
    }
    public sealed class Menu
    {
        public static readonly Lazy<Atlas> Atlas = Register(nameof(Menu), [
            new Texture(@"GiantDarkDoor.png", device),
            new Texture(@"Pixel.png", device),
            new Texture(@"SoulBlur.png", device),
            new Texture(@"SoulMini.png", device),
        ]);

        public static AtlasEntry GiantDarkDoor => Atlas.Value.Entries[new Atlas.AtlasKey("GiantDarkDoor", 0)];

        public static AtlasEntry Pixel => Atlas.Value.Entries[new Atlas.AtlasKey("Pixel", 1)];

        public static AtlasEntry SoulBlur => Atlas.Value.Entries[new Atlas.AtlasKey("SoulBlur", 2)];

        public static AtlasEntry SoulMini => Atlas.Value.Entries[new Atlas.AtlasKey("SoulMini", 3)];
    }
    public sealed class ___
    {
        public static readonly Lazy<Atlas> Atlas = Register(nameof(___), [
            new Texture(@"IMAGE_COVENANT.png", device),
        ]);

        public static AtlasEntry IMAGE_COVENANT => Atlas.Value.Entries[new Atlas.AtlasKey("IMAGE_COVENANT", 0)];
    }

    /// <summary>
    ///     Prepares this atlas system for use.
    /// </summary>
    public static void Initialize(GraphicsDevice device, FrameContext frameContext)
    {
        Atlases.device = device;
        _ = ChapterSelect.Atlas;
        _ = KnightScene.Atlas;
        _ = LargeTextures.Atlas;
        _ = Logo.Atlas;
        _ = Menu.Atlas;
        _ = ___.Atlas;
        Atlases.activeCopyScope = frameContext.CommandBuffer.Copy();
        foreach (Lazy<Atlas> atlas in CreatedAtlases.Values)
            atlas.Value.UploadSourcesIfNecessary(activeCopyScope);
        Atlases.activeCopyScope.Dispose();
        foreach (Lazy<Atlas> atlas in CreatedAtlases.Values)
            atlas.Value.BuildIfNecessary(device, frameContext);
    }

    /// <summary>
    ///     Registers a new atlas globally.
    /// </summary>
    public static Lazy<Atlas> Register(string name, List<Texture> sources) => CreatedAtlases[name] = new Lazy<Atlas>(() => new Atlas(name, sources));
}
