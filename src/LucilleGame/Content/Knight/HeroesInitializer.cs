using System.Numerics;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight;

/// <summary>
///     A simple initializer for hero instances for the battle.
/// </summary>
public sealed class HeroesInitializer
{
    private Hero[] heroes;

    private static QueuedHeroAction? crossSlashAction;

    private static QueuedHeroAction? rudeBusterAction;

    private static QueuedHeroAction? goodHealAction;

    private static QueuedHeroAction? angelsBlessingAction;

    private static QueuedHeroAction? iceShockAction;

    private const int SpellActionPriority = 5;

    private const int TextPriority = 10;

    /// <summary>
    ///     Kris' battle information.
    /// </summary>
    public Hero Kris
    {
        get;
        private set;
    }

    /// <summary>
    ///     Susie's battle information.
    /// </summary>
    public Hero Susie
    {
        get;
        private set;
    }

    /// <summary>
    ///     Noelle's battle information.
    /// </summary>
    public Hero Noelle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The managing game for Game initializer.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
        private set;
    }

    /// <summary>
    ///     The ordered set of all heroes active in the battle.
    /// </summary>
    /// <remarks>
    ///     The order of heroes in this set indicates the order
    ///     of their actions in said battle.
    /// </remarks>
    public ReadOnlySpan<Hero> All => heroes;

    public HeroesInitializer(KnightFightScene scene) => Scene = scene;

    internal static void OnGameRestart()
    {
        rudeBusterAction = null;
        goodHealAction = null;
        angelsBlessingAction = null;
        iceShockAction = null;
        crossSlashAction = null;
    }

    /// <summary>
    ///     Initializes all heroes.
    /// </summary>
    public void Initialize()
    {
        float attackAnimationRate = 3f / 60f;
        float hurtAnimationRate = 0.35f;

        int krisHP = (int)float.Round(StatsRepository.KrisMaxHP);
        int noelleHP = (int)float.Round(StatsRepository.NoelleMaxHP);
        HeroAnimation noelleAttackAnticipationAnimation = new HeroAnimation(Atlases.KnightScene.NoelleAttackAnticipation, totalHorizontalFrames: 1, animationRateSeconds: attackAnimationRate, game: Scene.Game, loops: false);
        HeroAnimation noelleStandStillAnimation = new HeroAnimation(Atlases.KnightScene.NoelleStandStill, totalHorizontalFrames: 1, game: Scene.Game);
        HeroAnimation noelleIdleAnimation = new HeroAnimation(Atlases.KnightScene.NoelleIdle, totalHorizontalFrames: 5, game: Scene.Game, horizontalOffset: 8f);
        HeroAnimation noelleHurtAnimation = new HeroAnimation(Atlases.KnightScene.NoelleHurt, totalHorizontalFrames: 1, game: Scene.Game, animationRateSeconds: hurtAnimationRate);
        HeroAnimation noelleDefendAnimation = new HeroAnimation(Atlases.KnightScene.NoelleDefend, totalHorizontalFrames: 1, game: Scene.Game, loops: false);
        HeroAnimation noelleDownedAnimation = new HeroAnimation(Atlases.KnightScene.NoelleDowned, totalHorizontalFrames: 1, game: Scene.Game);
        if (Scene.SideB)
        {
            krisHP = (int)float.Round(StatsRepository.KrisMaxHPSideB);
            noelleHP = (int)float.Round(StatsRepository.NoelleMaxHPSideB);

            noelleAttackAnticipationAnimation = noelleIdleAnimation;

            noelleStandStillAnimation = new HeroAnimation(Atlases.KnightScene.NoelleStandStillSideB, totalHorizontalFrames: 1, game: Scene.Game);
            noelleIdleAnimation = new HeroAnimation(Atlases.KnightScene.NoelleIdleSideB, totalHorizontalFrames: 5, game: Scene.Game, horizontalOffset: 8f);
            noelleDefendAnimation = new HeroAnimation(Atlases.KnightScene.NoelleDefendSideB, totalHorizontalFrames: 6, game: Scene.Game, loops: false, horizontalOffset: 32f);
            noelleHurtAnimation = new HeroAnimation(Atlases.KnightScene.NoelleHurtSideB, totalHorizontalFrames: 1, game: Scene.Game, animationRateSeconds: hurtAnimationRate);
            noelleDownedAnimation = new HeroAnimation(Atlases.KnightScene.NoelleDownedSideB, totalHorizontalFrames: 1, game: Scene.Game);
        }

        Kris = new Hero(Scene, maxHp: krisHP)
        {
            BattleMenuBorderColor = new Color(25, 246, 244),
            AttackBorderColor = new Color(0, 0, 175),
            AttackBarEdgeColor = new Color(0, 162, 232),
            ButtonIconColor = new Color(0, 162, 232),
            DamageColor = new Color(128, 255, 255),
            Name = LocalizationEntries.Kris,

            AttackHitSound = "BasicKnightHit.wav",

            BaseAttackDamage = Scene.HeroStats.KrisBaseDamage,

            UsesActCommand = true,

            BattleMenuIcon = Atlases.KnightScene.BattleMenuIcon_Kris,
            BattleMenuIconHurt = Atlases.KnightScene.BattleMenuIcon_Kris,
            BattleMenuIconDowned = Atlases.KnightScene.BattleMenuIcon_KrisDowned,
            BattleMenuNameSprite = Atlases.KnightScene.BattleMenuName_Kris,

            StandStillAnimation = new HeroAnimation(Atlases.KnightScene.KrisStandStill, totalHorizontalFrames: 1, game: Scene.Game),
            IdleAnimation = new HeroAnimation(Atlases.KnightScene.KrisIdle, totalHorizontalFrames: 6, game: Scene.Game),
            StandardAttackAnimation = new HeroAnimation(Atlases.KnightScene.KrisAttack, totalHorizontalFrames: 8, animationRateSeconds: attackAnimationRate, game: Scene.Game, loops: false, horizontalOffset: 26f, verticalOffset: 6f),
            StandardAttackAnticipationAnimation = new HeroAnimation(Atlases.KnightScene.KrisAttackAnticipation, totalHorizontalFrames: 1, animationRateSeconds: attackAnimationRate, game: Scene.Game, loops: false),
            DefendAnimation = new HeroAnimation(Atlases.KnightScene.KrisDefend, totalHorizontalFrames: 6, game: Scene.Game, animationRateSeconds: 3f / 60f, loops: false),
            HurtAnimation = new HeroAnimation(Atlases.KnightScene.KrisHurt, totalHorizontalFrames: 1, game: Scene.Game, animationRateSeconds: hurtAnimationRate),
            DownedAnimation = new HeroAnimation(Atlases.KnightScene.KrisDowned, totalHorizontalFrames: 1, game: Scene.Game),
            ItemUseAnimation = new HeroAnimation(Atlases.KnightScene.KrisItemUse, totalHorizontalFrames: 8, animationRateSeconds: 0.09f, game: Scene.Game, horizontalOffset: 14f),
            ItemPreparationAnimation = new HeroAnimation(Atlases.KnightScene.KrisItemPreparation, totalHorizontalFrames: 2, game: Scene.Game, horizontalOffset: 12f),

            TotalHitImpactFrames = 3,
            HitImpactTexture = Atlases.KnightScene.HitImpactKris,

            ActMagicOptionsGenerator = CreateKrisActs(),

            TextSoundFileName = null
        };
        Susie = new Hero(Scene, maxHp: (int)float.Round(StatsRepository.SusieMaxHP))
        {
            BattleMenuBorderColor = new Color(255, 0, 255),
            AttackBorderColor = new Color(128, 0, 128),
            AttackBarEdgeColor = new Color(234, 121, 200),
            ButtonIconColor = new Color(234, 121, 200),
            DamageColor = new Color(200, 156, 200),
            Name = LocalizationEntries.Susie,

            UsesAltSpareIcon = true,

            AttackHitSound = "BasicKnightHit.wav",

            BaseAttackDamage = Scene.HeroStats.SusieBaseDamage,

            BattleMenuIcon = Atlases.KnightScene.BattleMenuIcon_Susie,
            BattleMenuIconHurt = Atlases.KnightScene.BattleMenuIcon_SusieHurt,
            BattleMenuIconDowned = Atlases.KnightScene.BattleMenuIcon_SusieDowned,
            BattleMenuNameSprite = Atlases.KnightScene.BattleMenuName_Susie,

            StandStillAnimation = new HeroAnimation(Atlases.KnightScene.SusieStandStill, totalHorizontalFrames: 1, game: Scene.Game),
            IdleAnimation = new HeroAnimation(Atlases.KnightScene.SusieIdle, totalHorizontalFrames: 4, game: Scene.Game),
            StandardAttackAnimation = new HeroAnimation(Atlases.KnightScene.SusieAttackNormal, totalHorizontalFrames: 4, animationRateSeconds: attackAnimationRate, game: Scene.Game, loops: false, horizontalOffset: 32f, verticalOffset: 14f),
            StandardAttackAnticipationAnimation = new HeroAnimation(Atlases.KnightScene.SusieAttackAnticipation, totalHorizontalFrames: 1, animationRateSeconds: attackAnimationRate, game: Scene.Game, loops: false),
            DefendAnimation = new HeroAnimation(Atlases.KnightScene.SusieDefend, totalHorizontalFrames: 6, game: Scene.Game, animationRateSeconds: 3f / 60f, loops: false),
            HurtAnimation = new HeroAnimation(Atlases.KnightScene.SusieHurt, totalHorizontalFrames: 1, game: Scene.Game, animationRateSeconds: hurtAnimationRate),
            DownedAnimation = new HeroAnimation(Atlases.KnightScene.SusieDowned, totalHorizontalFrames: 1, game: Scene.Game),
            ItemUseAnimation = new HeroAnimation(Atlases.KnightScene.SusieItemUse, totalHorizontalFrames: 5, animationRateSeconds: 0.09f, game: Scene.Game),
            ItemPreparationAnimation = new HeroAnimation(Atlases.KnightScene.SusieItemPreparation, totalHorizontalFrames: 2, game: Scene.Game),

            TotalHitImpactFrames = 4,
            HitImpactTexture = Atlases.KnightScene.HitImpactSusie,

            ActMagicOptionsGenerator = CreateSusieMagicActs(),

            TextSoundFileName = "SusieSpeak.wav"
        };

        Noelle = new Hero(Scene, maxHp: noelleHP)
        {
            BattleMenuBorderColor = new Color(255, 255, 84),
            AttackBorderColor = new Color(255, 255, 0),
            AttackBarEdgeColor = new Color(255, 255, 255),
            ButtonIconColor = new Color(255, 255, 0),
            DamageColor = new Color(255, 255, 128),
            Name = LocalizationEntries.Noelle,

            UsesAltSpareIcon = true,

            AttackHitSound = "BasicKnightHit.wav",

            BaseAttackDamage = Scene.HeroStats.NoelleBaseDamage,

            BattleMenuIcon = Atlases.KnightScene.BattleMenuIcon_Noelle,
            BattleMenuIconHurt = Atlases.KnightScene.BattleMenuIcon_NoelleHurt,
            BattleMenuIconDowned = Atlases.KnightScene.BattleMenuIcon_NoelleDowned,
            BattleMenuNameSprite = Atlases.KnightScene.BattleMenuName_Noelle,

            StandStillAnimation = noelleStandStillAnimation,
            IdleAnimation = noelleIdleAnimation,
            StandardAttackAnimation = new HeroAnimation(Atlases.KnightScene.NoelleAttackNormal, totalHorizontalFrames: 5, animationRateSeconds: attackAnimationRate, game: Scene.Game, loops: false, horizontalOffset: 14f),
            StandardAttackAnticipationAnimation = noelleAttackAnticipationAnimation,
            DefendAnimation = noelleDefendAnimation,
            HurtAnimation = noelleHurtAnimation,
            DownedAnimation = noelleDownedAnimation,
            ItemUseAnimation = new HeroAnimation(Atlases.KnightScene.NoelleItemUse, totalHorizontalFrames: 10, animationRateSeconds: 0.09f, game: Scene.Game),
            ItemPreparationAnimation = new HeroAnimation(Atlases.KnightScene.NoelleItemPreparation, totalHorizontalFrames: 2, game: Scene.Game, horizontalOffset: 8f),

            TotalHitImpactFrames = 4,
            HitImpactTexture = Atlases.KnightScene.HitImpactNoelle,

            ActMagicOptionsGenerator = CreateNoelleMagicActs(),

            TextSoundFileName = "NoelleSpeak.wav"
        };

        if (Scene.SideB)
            heroes = [Kris, Noelle];
        else
            heroes = [Kris, Susie, Noelle];

        Susie.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.SusiePoint, 1, Scene.Game);
    }

    private static Func<OptionSelectionInterface, bool> TensionPointCost(float cost) => option => option.Menu.Scene.BattleHandler.ConsideredTensionPoints >= cost;

    private static Func<OptionSelectionInterface, QueuedHeroAction, OptionSelectionInterface.Option[]> CreateKrisActs()
    {
        return (options, baseAction) =>
        {
            bool finished = false;
            var checkTitle = new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.CheckName, Color.White);
            OptionSelectionInterface.OptionDescriptionLine[] checkLines =
            [
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.CheckDescription1, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.CheckDescription2, new Color(128, 128, 128))
            ];
            OptionSelectionInterface.Option check = new OptionSelectionInterface.Option(_ => true, _ => { }, _ => { }, option =>
            {
                option.Menu.OverridingBottomsideOverlayCandidates.Push(new TargetSelectionInterface(option.Hero, option.Menu, () => { }, () =>
                {
                    baseAction.ClearPerformanceEvents();

                    void OnClose(ManualTextInterface @interface)
                    {
                        finished = true;
                        option.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.KrisActEnd, 4, option.Menu.Scene.Game, loops: false, onFrameChange: f =>
                        {
                            if (f == 3)
                                option.Hero.ActiveAnimation = option.Hero.IdleAnimation;
                        });
                    }

                    ManualTextInterface text = new ManualTextInterface(option.Menu, OnClose, automaticallyInsertAsterisks: false,
                    [
                        $"* {LocalizationEntries.CheckText.Value}"
                    ]);
                    baseAction.HasFinished = a => finished || !option.Menu.OverridingBottomsideOverlayCandidates.Contains(text);
                    baseAction.OnStartPerformance += a =>
                    {
                        option.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.KrisAct, 7, option.Menu.Scene.Game, loops: false);
                        option.Menu.OverridingBottomsideOverlayCandidates.Clear();
                        option.Menu.OverridingBottomsideOverlayCandidates.Push(text);
                    };

                    option.Menu.OverridingBottomsideOverlayCandidates.Clear();
                    option.Menu.AdvanceInterfaceIndex();
                    return true;
                }, () =>
                {
                    options.Action.Cancel(option.Menu.Scene);
                }));
            }, checkTitle, checkLines);

            var holdBreathTitle = new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.HoldBreathName, Color.White);
            OptionSelectionInterface.Option holdBreath = new OptionSelectionInterface.Option(_ => true, _ => { }, _ => { }, option =>
            {
                baseAction.ClearPerformanceEvents();
                baseAction.Priority = TextPriority;

                void OnClose(ManualTextInterface @interface)
                {
                    finished = true;
                    option.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.KrisActEnd, 4, option.Menu.Scene.Game, loops: false, onFrameChange: f =>
                    {
                        if (f == 3)
                            option.Hero.ActiveAnimation = option.Hero.IdleAnimation;
                    });
                }

                int holdBreathCounter = option.Menu.Scene.BattleHandler.KrisHoldBreathCounter + 1;
                bool ripBozo = holdBreathCounter == 5;
                string textEntry = option.Menu.Scene.BattleHandler.KrisHeldBreath ? $"* {LocalizationEntries.HoldBreathRepeatText.Value}" : $"* {LocalizationEntries.HoldBreathText.Value}";
                if (ripBozo)
                    textEntry = $"* {LocalizationEntries.HoldBreathRepeatTextEasterEgg.Value}";

                ManualTextInterface text = new ManualTextInterface(option.Menu, OnClose, automaticallyInsertAsterisks: false, [textEntry]);
                baseAction.HasFinished = a => finished || !option.Menu.OverridingBottomsideOverlayCandidates.Contains(text);
                baseAction.OnStartPerformance += a =>
                {
                    option.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.KrisAct, 7, option.Menu.Scene.Game, loops: false);
                    option.Menu.OverridingBottomsideOverlayCandidates.Clear();
                    option.Menu.OverridingBottomsideOverlayCandidates.Push(text);
                    option.Menu.Scene.BattleHandler.KrisHeldBreath = true;
                    option.Menu.Scene.BattleHandler.KrisHoldBreathCounter++;

                    if (ripBozo)
                        option.Menu.Scene.Heroes.Kris.Hurt(option.Menu.Scene.Heroes.Kris.HP + 150, false);
                };

                option.Menu.OverridingBottomsideOverlayCandidates.Clear();
                option.Menu.AdvanceInterfaceIndex();
            }, holdBreathTitle, []);

            HeroStats stats = options.Menu.Scene.HeroStats;
            float crossSlashTensionPointCost = stats.CrossSlashTensionPointCost;
            var crossSlashTitle = new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.CrossSlashName, () => ChooseMagicColor(crossSlashTensionPointCost, options.Menu.Scene));
            OptionSelectionInterface.OptionDescriptionLine[] crossSlashLines =
            [
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.CrossSlashDescription1, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.CrossSlashDescription2, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.ArbitraryTP.WithArgs(crossSlashTensionPointCost.ToString()), new Color(255, 160, 64)),
            ];

            BattleHandler.TensionPointModifier crossSlashPayment = BattleHandler.TensionPointModifier.Take(crossSlashTensionPointCost);
            void CrossSlashHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = crossSlashPayment;
            }

            void CrossSlashCancelledHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = null;
            }

            OptionSelectionInterface.Option crossSlash = new OptionSelectionInterface.Option(TensionPointCost(crossSlashTensionPointCost), CrossSlashHover, CrossSlashCancelledHover, option =>
            {
                // Holy nesting...
                crossSlashAction ??= new QueuedHeroAction(option.Hero, option.Button, a =>
                {
                    if (a.Hero.ActiveAnimation.Texture != Atlases.KnightScene.KrisAttackCrossSlash)
                        return false;

                    return a.Hero.ActiveAnimation.Frame >= a.Hero.ActiveAnimation.TotalHorizontalFrames - 1;
                })
                {
                    Priority = SpellActionPriority,
                    OccursAsynchronously = true
                };
                option.Menu.OverridingBottomsideOverlayCandidates.Push(new TargetSelectionInterface(option.Hero, option.Menu, () => { }, () =>
                {
                    option.Menu.Scene.BattleHandler.ConsideredPayment = null;
                    option.Menu.OverridingBottomsideOverlayCandidates.Clear();

                    if (!option.Menu.Scene.BattleHandler.TensionPointPayments.Contains(crossSlashPayment))
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Add(crossSlashPayment);

                    crossSlashAction.ClearPerformanceEvents();
                    crossSlashAction.OnCancellation += a =>
                    {
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Remove(crossSlashPayment);
                    };
                    crossSlashAction.OnStartPerformance += a =>
                    {
                        option.Menu.StartCrossSlashAnimation(a);
                    };
                    crossSlashAction.OnEndPerformance += a =>
                    {
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                    };

                    crossSlashAction.Select(option.Menu.Scene);
                    options.Action.SubActions.Add(crossSlashAction);
                    option.Menu.AdvanceInterfaceIndex();
                    return true;
                }, () => { }));
            }, crossSlashTitle, crossSlashLines);

            if (options.Menu.Scene.SideB)
                return [check, holdBreath, crossSlash];
            else
                return [check, holdBreath];
        };
    }

    private static Func<OptionSelectionInterface, QueuedHeroAction, OptionSelectionInterface.Option[]> CreateSusieMagicActs()
    {
        return (options, baseAction) =>
        {
            HeroStats stats = options.Menu.Scene.HeroStats;

            float rudeBusterTensionPointCost = stats.RudeBusterTensionPointCost;
            var rudeBusterTitle = new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.RudeBusterName, () => ChooseMagicColor(rudeBusterTensionPointCost, options.Menu.Scene));
            OptionSelectionInterface.OptionDescriptionLine[] rudeBusterLines =
            [
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.RudeBusterDescription1, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.RudeBusterDescription2, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.ArbitraryTP.WithArgs(rudeBusterTensionPointCost.ToString()), new Color(255, 160, 64)),
            ];

            BattleHandler.TensionPointModifier rudeBusterPayment = BattleHandler.TensionPointModifier.Take(rudeBusterTensionPointCost);
            void RudeBusterHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = rudeBusterPayment;
            }

            void RudeBusterCancelledHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = null;
            }

            OptionSelectionInterface.Option rudeBuster = new OptionSelectionInterface.Option(TensionPointCost(rudeBusterTensionPointCost), RudeBusterHover, RudeBusterCancelledHover, option =>
            {
                // Holy nesting...
                rudeBusterAction ??= new QueuedHeroAction(option.Hero, option.Button, a =>
                {
                    if (a.Hero.ActiveAnimation.Texture != Atlases.KnightScene.SusieRudeBuster)
                        return false;

                    return a.Hero.ActiveAnimation.Frame >= a.Hero.ActiveAnimation.TotalHorizontalFrames - 1;
                })
                {
                    Priority = SpellActionPriority,
                    OccursAsynchronously = true
                };
                option.Menu.OverridingBottomsideOverlayCandidates.Push(new TargetSelectionInterface(option.Hero, option.Menu, () => { }, () =>
                {
                    option.Menu.Scene.BattleHandler.ConsideredPayment = null;
                    option.Menu.OverridingBottomsideOverlayCandidates.Clear();
                    option.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.SusieAxeAnticipation, 4, option.Menu.Scene.Game);

                    if (!option.Menu.Scene.BattleHandler.TensionPointPayments.Contains(rudeBusterPayment))
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Add(rudeBusterPayment);

                    rudeBusterAction.ClearPerformanceEvents();
                    rudeBusterAction.OnCancellation += a =>
                    {
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Remove(rudeBusterPayment);
                    };
                    rudeBusterAction.OnStartPerformance += a =>
                    {
                        option.Menu.StartRudeBusterAnimation(a);
                    };
                    rudeBusterAction.OnEndPerformance += a =>
                    {
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                    };

                    rudeBusterAction.Select(option.Menu.Scene);
                    options.Action.SubActions.Add(rudeBusterAction);
                    option.Menu.AdvanceInterfaceIndex();
                    return true;
                }, () => { }));
            }, rudeBusterTitle, rudeBusterLines);

            int goodHealHealQuantity = stats.GoodHealHealQuantity;
            float goodHealTensionPointCost = stats.GoodHealTensionPointCost;
            var goodHealTitle = new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.GoodHealName, () => ChooseMagicColor(goodHealTensionPointCost, options.Menu.Scene));
            OptionSelectionInterface.OptionDescriptionLine[] goodHealLines =
            [
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.GoodHealDescription1, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.GoodHealDescription2, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.ArbitraryTP.WithArgs(goodHealTensionPointCost.ToString()), new Color(255, 160, 64)),
            ];

            BattleHandler.TensionPointModifier goodHealPayment = BattleHandler.TensionPointModifier.Take(goodHealTensionPointCost);
            void GoodHealHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = goodHealPayment;
            }

            void GoodHealCancelledHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = null;
            }

            void GoodHealSelectionUpdate(HeroSelectionInterface @interface)
            {
                ReadOnlySpan<Hero> heroes = options.Menu.Scene.Heroes.All;
                for (int i = 0; i < heroes.Length; i++)
                    heroes[i].SelectionFlash = i == @interface.SelectionIndex;
            }

            OptionSelectionInterface.Option goodHeal = new OptionSelectionInterface.Option(TensionPointCost(goodHealTensionPointCost), GoodHealHover, GoodHealCancelledHover, option =>
            {
                option.Menu.OverridingBottomsideOverlayCandidates.Push(new HeroSelectionInterface(option.Hero, option.Menu, GoodHealSelectionUpdate, () => { }, @interface =>
                {
                    option.Menu.Scene.BattleHandler.ConsideredPayment = null;
                    option.Menu.OverridingBottomsideOverlayCandidates.Clear();
                    option.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.SusieHealAnticipation, 4, option.Menu.Scene.Game, horizontalOffset: 28f, verticalOffset: 6f);

                    goodHealAction ??= new QueuedHeroAction(option.Hero, option.Button, a =>
                    {
                        if (a.Hero.ActiveAnimation.Texture != Atlases.KnightScene.SusieHeal)
                            return false;

                        return a.Hero.ActiveAnimation.Frame >= a.Hero.ActiveAnimation.TotalHorizontalFrames - 1;
                    })
                    {
                        Priority = SpellActionPriority,
                        OccursAsynchronously = true
                    };
                    if (!option.Menu.Scene.BattleHandler.TensionPointPayments.Contains(goodHealPayment))
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Add(goodHealPayment);

                    KnightFightScene scene = option.Menu.Scene;
                    Hero heroToHeal = scene.Heroes.All[@interface.SelectionIndex];
                    heroToHeal.SelectionFlash = false;

                    goodHealAction.ClearPerformanceEvents();
                    goodHealAction.OnSelection += a =>
                    {
                        heroToHeal.SelectionFlash = false;
                    };
                    goodHealAction.OnCancellation += a =>
                    {
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Remove(goodHealPayment);
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                    };
                    goodHealAction.OnStartPerformance += a =>
                    {
                        float animationRate = 5f / 60f;
                        KnightFightScene scene = option.Menu.Scene;
                        a.AnimationTimer.Reset();
                        a.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.SusieHeal, 8, scene.Game, animationRate, horizontalOffset: 28f, verticalOffset: 6f, loops: false, onFrameChange: f =>
                        {
                            if (f == 7)
                                heroToHeal.Heal(goodHealHealQuantity);
                        });
                    };
                    goodHealAction.OnEndPerformance += a =>
                    {
                        heroToHeal.SelectionFlash = false;
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                    };

                    goodHealAction.Select(option.Menu.Scene);
                    options.Action.SubActions.Add(goodHealAction);
                    option.Menu.AdvanceInterfaceIndex();
                    return true;
                }));
            }, goodHealTitle, goodHealLines);

            return
            [
                rudeBuster,
                goodHeal
            ];
        };
    }

    private static Func<OptionSelectionInterface, QueuedHeroAction, OptionSelectionInterface.Option[]> CreateNoelleMagicActs()
    {
        return (options, baseAction) =>
        {
            HeroStats stats = options.Menu.Scene.HeroStats;

            float angelsBlessingTensionPointCost = stats.AngelsBlessingTensionPointCost;
            var angelsBlessingTitle = new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.AngelsBlessingName, () => ChooseMagicColor(angelsBlessingTensionPointCost, options.Menu.Scene));
            OptionSelectionInterface.OptionDescriptionLine[] angelsBlessingLines =
            [
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.AngelsBlessingDescription1, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.AngelsBlessingDescription2, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.ArbitraryTP.WithArgs(angelsBlessingTensionPointCost.ToString()), new Color(255, 160, 64)),
            ];

            BattleHandler.TensionPointModifier angelsBlessingPayment = BattleHandler.TensionPointModifier.Take(angelsBlessingTensionPointCost);
            void AngelsBlessingHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = angelsBlessingPayment;
            }

            void AngelsBlessingCancelledHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = null;
            }

            void AngelsBlessingSelectionUpdate(HeroSelectionInterface @interface)
            {
                ReadOnlySpan<Hero> heroes = options.Menu.Scene.Heroes.All;
                for (int i = 0; i < heroes.Length; i++)
                    heroes[i].SelectionFlash = i == @interface.SelectionIndex;
            }

            OptionSelectionInterface.Option angelsBlessing = new OptionSelectionInterface.Option(TensionPointCost(angelsBlessingTensionPointCost), AngelsBlessingHover, AngelsBlessingCancelledHover, option =>
            {
                option.Menu.OverridingBottomsideOverlayCandidates.Push(new HeroSelectionInterface(option.Hero, option.Menu, AngelsBlessingSelectionUpdate, () => { }, @interface =>
                {
                    option.Menu.Scene.BattleHandler.ConsideredPayment = null;
                    option.Menu.OverridingBottomsideOverlayCandidates.Clear();

                    angelsBlessingAction ??= new QueuedHeroAction(option.Hero, option.Button, a =>
                    {
                        if (a.Hero.ActiveAnimation.Texture != Atlases.KnightScene.NoelleMagicCast)
                            return false;

                        return a.Hero.ActiveAnimation.Frame >= a.Hero.ActiveAnimation.TotalHorizontalFrames - 1;
                    })
                    {
                        Priority = SpellActionPriority,
                        OccursAsynchronously = true
                    };
                    if (!option.Menu.Scene.BattleHandler.TensionPointPayments.Contains(angelsBlessingPayment))
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Add(angelsBlessingPayment);

                    KnightFightScene scene = option.Menu.Scene;
                    Hero heroToHeal = scene.Heroes.All[@interface.SelectionIndex];
                    heroToHeal.SelectionFlash = false;

                    angelsBlessingAction.ClearPerformanceEvents();
                    angelsBlessingAction.OnSelection += a =>
                    {
                        heroToHeal.SelectionFlash = false;
                    };
                    angelsBlessingAction.OnCancellation += a =>
                    {
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Remove(angelsBlessingPayment);
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                        a.Hero.SelectionFlash = false;
                    };
                    angelsBlessingAction.OnStartPerformance += a =>
                    {
                        float animationRate = 5f / 60f;
                        KnightFightScene scene = option.Menu.Scene;
                        a.AnimationTimer.Reset();
                        a.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.NoelleMagicCast, 9, scene.Game, animationRate, horizontalOffset: 14f, verticalOffset: -2f, loops: false, onFrameChange: f =>
                        {
                            if (f == 6)
                            {
                                if (scene.SideB)
                                {
                                    // showMaxTextIfNeeded is false for a reason here.
                                    // According to the JSON, the heal value here is 777, a
                                    // notoriously angelic/holy number in religious symbolism.
                                    // I want the player to be able to see that number
                                    // and potentially make the associative connection.
                                    if (heroToHeal.HP <= 0)
                                        heroToHeal.Revive(stats.AngelsBlessingHealAmount_SideB, showMaxTextIfNeeded: false);
                                    else
                                        heroToHeal.Heal(stats.AngelsBlessingHealAmount_SideB, showMaxTextIfNeeded: false);
                                }
                                else
                                {
                                    if (heroToHeal.HP <= 0)
                                        heroToHeal.Revive(stats.AngelsBlessingHealAmount_Revive);
                                    else
                                        heroToHeal.Heal(stats.AngelsBlessingHealAmount_NotRevive);
                                }
                            }
                        });
                    };
                    angelsBlessingAction.OnEndPerformance += a =>
                    {
                        heroToHeal.SelectionFlash = false;
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                    };

                    angelsBlessingAction.Select(option.Menu.Scene);
                    options.Action.SubActions.Add(angelsBlessingAction);
                    option.Menu.AdvanceInterfaceIndex();
                    return true;
                }));
            }, angelsBlessingTitle, angelsBlessingLines);

            float iceShockTensionPointCost = stats.IceShockTensionPointCost;
            var iceShockTitle = new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.IceShockName, () => ChooseMagicColor(iceShockTensionPointCost, options.Menu.Scene));
            OptionSelectionInterface.OptionDescriptionLine[] iceShockLines =
            [
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.IceShockDescription1, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.IceShockDescription2, new Color(128, 128, 128)),
                new OptionSelectionInterface.OptionDescriptionLine(LocalizationEntries.ArbitraryTP.WithArgs(iceShockTensionPointCost.ToString()), new Color(255, 160, 64)),
            ];

            BattleHandler.TensionPointModifier iceShockPayment = BattleHandler.TensionPointModifier.Take(iceShockTensionPointCost);
            void IceShockHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = iceShockPayment;
            }

            void IceShockCancelledHover(OptionSelectionInterface @interface)
            {
                @interface.Menu.Scene.BattleHandler.ConsideredPayment = null;
            }

            OptionSelectionInterface.Option iceShock = new OptionSelectionInterface.Option(TensionPointCost(iceShockTensionPointCost), IceShockHover, IceShockCancelledHover, option =>
            {
                // Holy nesting...
                iceShockAction ??= new QueuedHeroAction(option.Hero, option.Button, a =>
                {
                    if (a.Hero.ActiveAnimation.Texture != Atlases.KnightScene.NoelleMagicCast)
                        return false;

                    return a.Hero.ActiveAnimation.Frame >= a.Hero.ActiveAnimation.TotalHorizontalFrames - 1;
                })
                {
                    Priority = SpellActionPriority,
                    OccursAsynchronously = true
                };
                option.Menu.OverridingBottomsideOverlayCandidates.Push(new TargetSelectionInterface(option.Hero, option.Menu, () => { }, () =>
                {
                    option.Menu.Scene.BattleHandler.ConsideredPayment = null;
                    option.Menu.OverridingBottomsideOverlayCandidates.Clear();

                    if (!option.Menu.Scene.BattleHandler.TensionPointPayments.Contains(iceShockPayment))
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Add(iceShockPayment);

                    iceShockAction.ClearPerformanceEvents();
                    iceShockAction.OnCancellation += a =>
                    {
                        option.Menu.Scene.BattleHandler.TensionPointPayments.Remove(iceShockPayment);
                    };
                    iceShockAction.OnStartPerformance += a =>
                    {
                        option.Menu.StartIceShockAnimation(a);
                    };
                    iceShockAction.OnEndPerformance += a =>
                    {
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                    };

                    iceShockAction.Select(option.Menu.Scene);
                    options.Action.SubActions.Add(iceShockAction);
                    option.Menu.AdvanceInterfaceIndex();
                    return true;
                }, () => { }));
            }, iceShockTitle, iceShockLines);

            return
            [
                angelsBlessing, iceShock
            ];
        };
    }

    private static Color ChooseMagicColor(float cost, KnightFightScene game)
    {
        bool affordable = game.BattleHandler.ConsideredTensionPoints >= cost;
        if (affordable)
            return Color.White;

        return new Color(128, 128, 128);
    }

    /// <summary>
    ///     Updates all Heroes.
    /// </summary>
    public void Update()
    {
        Vector2 gameResolution = NorthernlightGame.KnightFightResolution;
        if (Kris.Position == Vector3.Zero)
            Kris.Position = Kris.StartingPosition = new Vector3(gameResolution * new Vector2(0.265f, 0.53f) + Scene.Backdrop.ForegroundVerticalOffset, 10f);
        if (Susie.Position == Vector3.Zero)
            Susie.Position = Susie.StartingPosition = new Vector3(gameResolution * new Vector2(0.29f, 0.575f) + Scene.Backdrop.ForegroundVerticalOffset, 12f);
        if (Noelle.Position == Vector3.Zero)
            Noelle.Position = Noelle.StartingPosition = new Vector3(gameResolution * new Vector2(0.24f, 0.63f) + Scene.Backdrop.ForegroundVerticalOffset, 14f);

        Kris.Update();
        Susie.Update();
        Noelle.Update();
    }
}
