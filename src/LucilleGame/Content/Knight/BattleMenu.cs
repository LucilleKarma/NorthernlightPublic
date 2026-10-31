using System.Numerics;
using Northernlight.Content.Knight.Items;
using Northernlight.Content.Knight.Particles;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents an instance of the battle menu
///     UI.
/// </summary>
public sealed class BattleMenu
{
    /// <summary>
    ///     The current state of this battle menu.
    /// </summary>
    public enum State
    {
        PlayerActionSelectionOngoing,
        PerformingChosenPlayerActions,
        PerformPlayerAttacks,
        LetEnemyAttack
    }

    private float weirdBugCountdownThing;

    private float tpCostFlashTimer;

    private float heroAnimationTimer;

    private float appearanceTimer;

    private List<Hero> heroesIntendingToAttack => Scene.Heroes.All.ToArray().Where(h => h.Attacking).ToList();

    internal Dictionary<Hero, List<QueuedHeroAction>> actionsTakenByHero = [];

    private readonly Font font;

    private readonly BattleMenuActionInterface[] interfaces;

    private readonly HeroAnimation krisThinkerPose;

    private readonly FlavorTextInterface flavorText;

    private readonly Dictionary<Hero, Item?> itemsHeroesWillUse = [];

    /// <summary>
    ///     The overriding bottomside overlay candidates of
    ///     this menu. The top one, if there is one, is
    ///     the one displayed.
    /// </summary>
    public Stack<BattleMenuBottomsideOverlay> OverridingBottomsideOverlayCandidates
    {
        get;
        internal set;
    } = [];

    /// <summary>
    ///     The overriding bottomside overlay of this menu.
    /// </summary>
    public BattleMenuBottomsideOverlay? OverridingBottomsideOverlay
    {
        get
        {
            OverridingBottomsideOverlayCandidates.TryPeek(out BattleMenuBottomsideOverlay? overlay);
            return overlay;
        }
    }

    /// <summary>
    ///     The active bottomside overlay of this menu.
    /// </summary>
    public BattleMenuBottomsideOverlay? BottomsideOverlay
    {
        get
        {
            if (CurrentState == State.LetEnemyAttack)
                return null;

            return OverridingBottomsideOverlay ?? flavorText;
        }
    }

    /// <summary>
    ///     The index of the active interface choice.
    /// </summary>
    public int ActiveInterfaceIndex
    {
        get;
        private set;
    }

    /// <summary>
    ///     A dedicated timer used to make this menu
    ///     go away.
    /// </summary>
    public float FadeOutTimer
    {
        get;
        set;
    }

    /// <summary>
    ///     The current state of this menu.
    /// </summary>
    public State CurrentState
    {
        get;
        set;
    } = State.PlayerActionSelectionOngoing;

    /// <summary>
    ///     The game responsible for this
    ///     menu.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
        private set;
    }

    /// <summary>
    ///     The action queue used by the player in
    ///     accordance with their selections
    ///     in this menu.
    /// </summary>
    public HeroActionQueue PlayerActionQueue
    {
        get;
        private set;
    } = new HeroActionQueue();

    /// <summary>
    ///     The resolution of the game responsible
    ///     for this menu.
    /// </summary>
    public required Vector2 GameResolution
    {
        get;
        init;
    }

    /// <summary>
    ///     How much this battle menu has visually appeared so far.
    /// </summary>
    public float AppearanceProgress => float.InverseLerp(0f, 0.4f, appearanceTimer) * float.InverseLerp(0.4f, 0f, FadeOutTimer);

    public float DedicatedTextSpaceHeight => GameResolution.Y * 0.2377f;

    public BattleMenu(KnightFightScene scene)
    {
        Scene = scene;

        flavorText = new FlavorTextInterface(this);

        font = new Font("8bitoperator.ttf", 48f);
        interfaces = new BattleMenuActionInterface[Scene.Heroes.All.Length];
        for (int i = 0; i < Scene.Heroes.All.Length; i++)
        {
            Hero hero = Scene.Heroes.All[i];
            BattleMenuActionInterface.Button[] buttons =
            [
                new BattleMenuActionInterface.Button(Atlases.KnightScene.FightButton, null, Fight),
                new BattleMenuActionInterface.Button(
                    hero.UsesActCommand ? Atlases.KnightScene.ActButton : Atlases.KnightScene.MagicButton,
                    hero.UsesActCommand ? Atlases.KnightScene.ActIcon : Atlases.KnightScene.MagicIcon,
                    ActOrMagic),
                new BattleMenuActionInterface.Button(Atlases.KnightScene.ItemButton, Atlases.KnightScene.ItemIcon, UseItem),
                new BattleMenuActionInterface.Button(Atlases.KnightScene.SpareButton, hero.UsesAltSpareIcon ? Atlases.KnightScene.SpareIconB : Atlases.KnightScene.SpareIconA, Spare),
                new BattleMenuActionInterface.Button(Atlases.KnightScene.DefendButton,Atlases.KnightScene.DefendIcon, Defend),
            ];

            interfaces[i] = new BattleMenuActionInterface(this, hero, buttons);
        }

        krisThinkerPose = new HeroAnimation(Atlases.KnightScene.KrisThink, 2, scene.Game);

        Scene.BattleHandler.OnEnd += () =>
        {
            weirdBugCountdownThing = 0.1f;
            CurrentState = State.PlayerActionSelectionOngoing;
            foreach (Hero hero in Scene.Heroes.All)
                hero.ApplyEndOfTurnEffects();
            foreach (BattleMenuActionInterface i in interfaces)
                i.ShoveDown();

            flavorText.RerollText();
        };
    }

    private QueuedHeroAction Fight(Hero hero, BattleMenuActionInterface.Button button)
    {
        QueuedHeroAction attackAction = new QueuedHeroAction(hero, button);
        attackAction.OnSelection += a =>
        {
            OverridingBottomsideOverlayCandidates.Push(new TargetSelectionInterface(a.Hero, this, () => a.Hero.Attacking = false, () =>
            {
                a.Hero.Attacking = true;
                return true;
            }, () => attackAction.Cancel(Scene)));

            a.Hero.ActiveAnimation = hero.StandardAttackAnticipationAnimation;
        };
        attackAction.OnCancellation += a =>
        {
            a.Hero.ActiveAnimation = hero.IdleAnimation;
        };

        return attackAction;
    }

    private QueuedHeroAction Defend(Hero hero, BattleMenuActionInterface.Button button)
    {
        var defendPayment = BattleHandler.TensionPointModifier.Give(16f);
        QueuedHeroAction action = new QueuedHeroAction(hero, button);
        action.OnSelection += a =>
        {
            if (!Scene.BattleHandler.TensionPointPayments.Contains(defendPayment))
                Scene.BattleHandler.TensionPointPayments.Add(defendPayment);

            if (a.Hero.ActiveAnimation != hero.DefendAnimation)
                a.Hero.ActiveAnimation = hero.DefendAnimation;
            a.Hero.Defending = true;
        };

        // Mostly done to account for Kris, so that they
        // get the heck out of their thinking pose.
        action.OnStartPerformance += a =>
        {
            // This gets reset at the end of the turn.
            if (a.Hero == Scene.Heroes.Kris && a.Hero.ActiveAnimation != hero.DefendAnimation)
                a.Hero.ActiveAnimation = hero.DefendAnimation;
            a.Hero.Defending = true;
        };
        action.OnCancellation += a =>
        {
            Scene.BattleHandler.TensionPointPayments.Remove(defendPayment);

            a.Hero.ActiveAnimation = hero.IdleAnimation;
            a.Hero.Defending = false;
        };

        return action;
    }

    private QueuedHeroAction Spare(Hero hero, BattleMenuActionInterface.Button button)
    {
        QueuedHeroAction action = new QueuedHeroAction(hero, button);
        action.OnSelection += a =>
        {
            Scene.RoaringKnight.BeingSpared = true;
        };
        action.OnCancellation += a =>
        {
            Scene.RoaringKnight.BeingSpared = false;
        };

        return action;
    }

    private QueuedHeroAction UseItem(Hero hero, BattleMenuActionInterface.Button button)
    {
        void SelectionUpdate(HeroSelectionInterface @interface)
        {
            ReadOnlySpan<Hero> heroes = Scene.Heroes.All;
            for (int i = 0; i < heroes.Length; i++)
                heroes[i].SelectionFlash = i == @interface.SelectionIndex;
        }

        Item? item = itemsHeroesWillUse.GetValueOrDefault(hero);
        var itemTpPayment = BattleHandler.TensionPointModifier.Give(item?.TensionPointBonusPercentage ?? 0f);
        QueuedHeroAction action = new QueuedHeroAction(hero, button, a => a.Hero.ActiveAnimation.Frame >= a.Hero.ActiveAnimation.TotalHorizontalFrames - 1)
        {
            OccursAsynchronously = true
        };

        void SelectionCancel(QueuedHeroAction a)
        {
            a.ClearPerformanceEvents();
            if (itemTpPayment.Payment != 0f)
                Scene.BattleHandler.TensionPointPayments.Remove(itemTpPayment);

            int previousHeroIndex = int.Max(0, ActiveInterfaceIndex - 1);
            for (int i = previousHeroIndex; i < Scene.Heroes.All.Length; i++)
            {
                if (previousHeroIndex == ActiveInterfaceIndex)
                    continue;

                Item? item = itemsHeroesWillUse.GetValueOrDefault(Scene.Heroes.All[i]);
                item?.IntendedToBeUsedUp = false;
                itemsHeroesWillUse.Remove(Scene.Heroes.All[i]);
            }

            a.Hero.ActiveAnimation = a.Hero.IdleAnimation;

            for (int i = 0; i < Scene.Heroes.All.Length; i++)
                Scene.Heroes.All[i].SelectionFlash = false;
        }

        action.OnSelection += a =>
        {
            List<Item> usableItems = Scene.BattleHandler.Inventory.Where(i => !i.IntendedToBeUsedUp).ToList();
            List<OptionSelectionInterface.Option> options = [];
            foreach (Item i in usableItems)
            {
                void OnItemSelect(OptionSelectionInterface @interface)
                {
                    item = i;
                    itemsHeroesWillUse[a.Hero] = i;
                    action.Select(Scene);
                }

                var title = new OptionSelectionInterface.OptionDescriptionLine(i.Name, Color.White);
                var description = i.DescriptionLines.Select(l => new OptionSelectionInterface.OptionDescriptionLine(l, new Color(127, 127, 127))).ToArray();

                options.Add(new OptionSelectionInterface.Option(_ => true, _ => { }, _ => { }, OnItemSelect, title, description));
            }

            if (item is null)
                OverridingBottomsideOverlayCandidates.Push(new OptionSelectionInterface(a.Hero, button, a, this, () => SelectionCancel(a), (_, _2) => options.ToArray()));
            if (usableItems.Count >= 1)
                a.Hero.ActiveAnimation = a.Hero.ItemPreparationAnimation;

            if (item is null)
                return;

            item.IntendedToBeUsedUp = true;

            if (item.TensionPointBonusPercentage != 0)
            {
                itemTpPayment = BattleHandler.TensionPointModifier.Give(item.TensionPointBonusPercentage);
                if (!Scene.BattleHandler.TensionPointPayments.Contains(itemTpPayment))
                    Scene.BattleHandler.TensionPointPayments.Add(itemTpPayment);
                return;
            }

            if (item.AffectsEveryone)
            {
                action.OnStartPerformance += a =>
                {
                    KnightFightScene scene = Scene;
                    a.AnimationTimer.Reset();
                    a.Hero.ActiveAnimation = a.Hero.ItemUseAnimation;
                };
                action.OnEndPerformance += a =>
                {
                    a.Hero.ActiveAnimation = a.Hero.IdleAnimation;
                    foreach (Hero hero in Scene.Heroes.All)
                        item.OnUse(hero);

                    Scene.BattleHandler.Inventory.Remove(item);
                    itemsHeroesWillUse.Remove(a.Hero);
                };
                OverridingBottomsideOverlay?.Pop(true);
            }
            else
            {
                OverridingBottomsideOverlayCandidates.Push(new HeroSelectionInterface(a.Hero, this, SelectionUpdate, () => SelectionCancel(a), @interface =>
                {
                    Scene.BattleHandler.ConsideredPayment = null;
                    OverridingBottomsideOverlayCandidates.Clear();

                    KnightFightScene scene = Scene;
                    Hero heroToGiveItem = scene.Heroes.All[@interface.SelectionIndex];
                    heroToGiveItem.SelectionFlash = false;
                    action.OnCancellation += a =>
                    {
                        heroToGiveItem.SelectionFlash = false;
                    };
                    action.OnSelection += a =>
                    {
                        heroToGiveItem.SelectionFlash = false;
                    };
                    action.OnStartPerformance += a =>
                    {
                        KnightFightScene scene = Scene;
                        a.AnimationTimer.Reset();
                        a.Hero.ActiveAnimation = a.Hero.ItemUseAnimation;
                    };
                    action.OnEndPerformance += a =>
                    {
                        heroToGiveItem.SelectionFlash = false;
                        a.Hero.ActiveAnimation = a.Hero.IdleAnimation;

                        item.OnUse(heroToGiveItem);
                        Scene.BattleHandler.Inventory.Remove(item);
                        itemsHeroesWillUse.Remove(a.Hero);
                    };

                    AdvanceInterfaceIndex();
                    return true;
                }));
            }
        };
        action.OnCancellation += SelectionCancel;

        return action;
    }

    private QueuedHeroAction ActOrMagic(Hero hero, BattleMenuActionInterface.Button button)
    {
        QueuedHeroAction action = new QueuedHeroAction(hero, button);
        action.OnSelection += a =>
        {
            OverridingBottomsideOverlayCandidates.Push(new OptionSelectionInterface(a.Hero, button, a, this, () => { Scene.BattleHandler.ConsideredPayment = null; }, a.Hero.ActMagicOptionsGenerator));
        };
        action.OnCancellation += a =>
        {
            // Scene.BattleHandler.ConsideredPayment = null;
        };

        return action;
    }

    /// <summary>
    ///     Starts Kris' cross slash animation.
    /// </summary>
    public void StartCrossSlashAnimation(QueuedHeroAction action)
    {
        float animationRate = 14f / 60f;
        action.AnimationTimer.Reset();
        action.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.KrisAttackCrossSlash, 4, Scene.Game, animationRate, loops: false, onFrameChange: f =>
        {
            if (f % 2 == 1)
            {
                float damageFactor = Scene.HeroStats.CrossSlashDamageFactor * Scene.HeroStats.GeneralDamageFactor;
                int damage = (int)float.Ceiling(action.Hero.BaseAttackDamage * damageFactor);

                Scene.AddScreenShake(10f);
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("CrossSlash.wav")));
                Scene.RoaringKnight.AddShake(16f);
                Scene.RoaringKnight.Hurt(damage, true);
                action.Hero.CountdownFlash = 1.25f;

                float afterimageLifetime = 0.75f;
                action.Hero.CreateAfterimage(Vector3.UnitX * 120f / afterimageLifetime, afterimageLifetime, 1f);
                action.Hero.CreateAfterimage(Vector3.UnitX * 172f / afterimageLifetime, afterimageLifetime, 0.5f);

                Scene.HitImpacts.Add(new HitImpact()
                {
                    Position = Scene.RoaringKnight.Position + new Vector3(25f, 45f, 120f),
                    Texture = action.Hero.HitImpactTexture,
                    TotalFrames = action.Hero.TotalHitImpactFrames
                });

                Scene.RoaringKnight.CreateHurtTextParticle(damage, action.Hero.DamageColor);
            }
        }, horizontalOffset: action.Hero.StandardAttackAnimation.TotalHorizontalFrames);
    }

    /// <summary>
    ///     Starts Susie's rude buster animation.
    /// </summary>
    public void StartRudeBusterAnimation(QueuedHeroAction action)
    {
        float animationRate = 5f / 60f;
        action.AnimationTimer.Reset();
        action.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.SusieRudeBuster, 15, Scene.Game, animationRate, loops: false, onFrameChange: f =>
        {
            if (f == 6)
            {
                Vector2 spawnPosition = action.Hero.Position.AsVector2();
                Vector2 destination = Scene.RoaringKnight.Position.AsVector2();

                float baseDamage = (int)float.Ceiling(action.Hero.BaseAttackDamage * Scene.HeroStats.GeneralDamageFactor * Scene.HeroStats.RudeBusterDamageFactor);
                Scene.RudeBusterShots.Add(new RudeBusterShot(Scene)
                {
                    Damage = (int)float.Ceiling(baseDamage * Scene.Rng.NextFloat(0.93f, 1f)),
                    Position = action.Hero.Position.AsVector2() - Vector2.UnitY * 74f,
                    Rotation = destination - spawnPosition
                });
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("RudeBusterSwing.wav")));
            }
        });
    }

    /// <summary>
    ///     Starts Noelle's ice shock animation.
    /// </summary>
    public void StartIceShockAnimation(QueuedHeroAction action)
    {
        float animationRate = 5f / 60f;
        action.AnimationTimer.Reset();
        action.Hero.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.NoelleMagicCast, 9, Scene.Game, animationRate, loops: false, onFrameChange: f =>
        {
            if (f == 6)
            {
                bool strongAttack = Scene.SideB;
                float damageFactor = Scene.HeroStats.IceShockDamageFactor * Scene.HeroStats.GeneralDamageFactor;
                float baseDamage = (int)float.Ceiling(action.Hero.BaseAttackDamage * damageFactor);
                int damage = (int)float.Ceiling(baseDamage * Scene.Rng.NextFloat(0.93f, 1f));
                Scene.IceShockParticles.Add(new IceShockParticle(damage)
                {
                    Position = Scene.RoaringKnight.Position + Vector3.UnitZ * 4f,
                    Strong = strongAttack
                });
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("IceShock.wav"), 0.74f));
            }
        }, horizontalOffset: 18f);
    }

    /// <summary>
    ///     Updates this battle menu.
    /// </summary>
    public void Update()
    {
        if (FadeOutTimer > 0f)
            FadeOutTimer += Scene.Time.DeltaTime;

        if (Scene.BattleHandler.CurrentState == BattleHandler.State.BattleStartupAnimation)
            return;

        if (AppearanceProgress < 1f)
        {
            bool justStarted = heroAnimationTimer <= 0f;
            float heroAnimationDuration = 0.45f;
            if (Scene.SideB)
                heroAnimationDuration = 0.67f;

            if (justStarted)
            {
                Scene.AddScreenShake(4.5f);
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("BattleStart.wav"), 1.5f));
                foreach (Hero h in Scene.Heroes.All)
                {
                    if (h != Scene.Heroes.Noelle)
                        h.ActiveAnimation = h.StandardAttackAnimation;

                    if (Scene.SideB && h == Scene.Heroes.Noelle)
                        h.ActiveAnimation = new HeroAnimation(Atlases.KnightScene.NoelleAppearSideB, 15, Scene.Game, animationRateSeconds: heroAnimationDuration / 15f, loops: false, horizontalOffset: -6f);
                }
            }

            heroAnimationTimer += Scene.Time.DeltaTime;

            float heroAnimationProgress = float.InverseLerp(0f, heroAnimationDuration, heroAnimationTimer);
            if (heroAnimationProgress >= 1f && appearanceTimer <= 0f)
            {
                foreach (Hero h in Scene.Heroes.All)
                    h.ActiveAnimation = h.IdleAnimation;

                appearanceTimer = 0.0001f;
                Scene.StartBlackKnifeTrack();
            }

            if (appearanceTimer > 0f)
                appearanceTimer += Scene.Time.DeltaTime;

            return;
        }

        if (Scene.BattleHandler.ConsideredPayment is not null)
            tpCostFlashTimer += Scene.Time.DeltaTime;
        else
            tpCostFlashTimer = 0f;

        bool everyoneIsDowned = interfaces.All(i => i.Hero.HP <= 0);
        if (everyoneIsDowned)
            return;

        DetermineInterfaceActivity();
        if (CurrentState == State.PlayerActionSelectionOngoing)
        {
            while (interfaces[ActiveInterfaceIndex].Hero.HP <= 0)
                AdvanceInterfaceIndex();

            AwaitPlayerInput();

            // Subtle detail time! Kris uses a designated thinking
            // pose animation when anyone else is "deciding"
            // what to do as a means of indicating that Kris is
            // doing the decisionmaking as the leader.
            if (!Scene.Heroes.Kris.Defending)
            {
                if (interfaces[ActiveInterfaceIndex].Hero != Scene.Heroes.Kris)
                {
                    if (!heroesIntendingToAttack.Contains(Scene.Heroes.Kris))
                        Scene.Heroes.Kris.ActiveAnimation = krisThinkerPose;
                }
                else if (Scene.Heroes.Kris.ActiveAnimation == krisThinkerPose)
                    Scene.Heroes.Kris.ActiveAnimation = Scene.Heroes.Kris.IdleAnimation;
            }
        }
        else if (CurrentState == State.PerformingChosenPlayerActions)
            UpdatePlayerActions();
        else if (CurrentState == State.PerformPlayerAttacks)
            UpdatePlayerAttacks();

        // Everything pertaining to the battle
        // is done in the dedicated battle handler
        // system, or interfacing with it via
        // events and such.

        foreach (BattleMenuActionInterface i in interfaces)
            i.Update();

        weirdBugCountdownThing = float.Clamp(weirdBugCountdownThing - Scene.Time.DeltaTime, 0f, 5f);
        BottomsideOverlay?.Update();
    }

    private void DetermineInterfaceActivity()
    {
        for (int i = 0; i < interfaces.Length; i++)
            interfaces[i].Active = CurrentState == State.PlayerActionSelectionOngoing && i == ActiveInterfaceIndex;
    }

    /// <summary>
    ///     Awaits input from the player for the
    ///     battle menu.
    /// </summary>
    public void AwaitPlayerInput()
    {
        for (int i = 0; i < interfaces.Length; i++)
        {
            BattleMenuActionInterface actionInterface = interfaces[i];
            if (!actionInterface.Active)
                continue;

            bool occupiedWithSubmenu = OverridingBottomsideOverlay?.RequiresInput ?? false;

            if (!occupiedWithSubmenu)
            {
                if (ActiveKeymap.Left.WasJustPressed)
                    actionInterface.ShiftButtonIndex(-1);
                else if (ActiveKeymap.Right.WasJustPressed)
                    actionInterface.ShiftButtonIndex(1);

                if (ActiveKeymap.Cancel.WasJustPressed)
                {
                    GoBackToPreviousChoice();
                    break;
                }
                else if (ActiveKeymap.Confirm.WasJustPressed)
                {
                    actionInterface.ClickButton();
                    break;
                }
            }
        }
    }

    private void UpdatePlayerActions()
    {
        bool updatedFirstSynchronousAction = false;
        int maxActionPriority = PlayerActionQueue.Actions.Count >= 1 ? PlayerActionQueue.Actions.Max(a => a.MaxPriorityConsideringSubActions) : 0;
        List<QueuedHeroAction> completedActions = [];
        foreach (QueuedHeroAction action in PlayerActionQueue.Actions)
        {
            if (action.MaxPriorityConsideringSubActions != maxActionPriority)
                continue;

            if (action.OccursAsynchronously && action.Update(Scene.Game))
                completedActions.Add(action);

            if (!updatedFirstSynchronousAction && !action.OccursAsynchronously)
            {
                if (action.Update(Scene.Game))
                    completedActions.Add(action);

                updatedFirstSynchronousAction = true;
            }
        }

        foreach (var completedAction in completedActions)
        {
            if (actionsTakenByHero.TryGetValue(completedAction.Hero, out var actions))
                actions.Remove(completedAction);
        }

        PlayerActionQueue.Actions = new Stack<QueuedHeroAction>(PlayerActionQueue.Actions.Where(a => !completedActions.Contains(a)));

        if (PlayerActionQueue.Actions.Count <= 0)
        {
            itemsHeroesWillUse.Clear();
            Scene.BattleHandler.PayOffTensionPointDebt();
            if (heroesIntendingToAttack.Count >= 1)
            {
                OverridingBottomsideOverlayCandidates.Push(new AttackMinigameInterface(this, Scene.Heroes.All.ToArray(), heroesIntendingToAttack.ToArray()));
                CurrentState = State.PerformPlayerAttacks;
            }
            else
                StartEnemyAttack();
        }
    }

    private void UpdatePlayerAttacks()
    {
        if (OverridingBottomsideOverlay is not AttackMinigameInterface { } minigame)
        {
            StartEnemyAttack();
            return;
        }

        if (minigame.FadeoutProgress >= 1f)
        {
            OverridingBottomsideOverlayCandidates.Clear();
            StartEnemyAttack();
        }
    }

    private void StartEnemyAttack()
    {
        foreach (Hero hero in Scene.Heroes.All)
            hero.ApplyStartOfEnemyTurnEffects();

        CurrentState = State.LetEnemyAttack;
        Scene.BattleHandler.Start();
    }

    /// <summary>
    ///     Reverses the <see cref="ActiveInterfaceIndex"/>, effectively
    ///     moving the player back to reconsider their last choice.
    /// </summary>
    public void GoBackToPreviousChoice()
    {
        // You haven't made a choice yet, dummy!
        if (ActiveInterfaceIndex <= 0)
            return;

        int validHeroIndex = -1;
        for (int i = ActiveInterfaceIndex - 1; i >= 0; i--)
        {
            if (Scene.Heroes.All[i].HP <= 0)
                continue;

            validHeroIndex = i;
            break;
        }
        if (validHeroIndex == -1)
            return;

        // It took me like 15 minutes to finally determine
        // what sound I had to play when the player
        // goes to their previous choice in the battle menu.
        // All these blasted youtubers, so confident in
        // their decisionmaking...
        Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));

        Hero previousHero = Scene.Heroes.All[validHeroIndex];
        if (actionsTakenByHero.TryGetValue(previousHero, out List<QueuedHeroAction>? actions))
        {
            while (actions.Count >= 1)
                actions[0].Cancel(Scene);

            actions.Clear();
        }

        ActiveInterfaceIndex = validHeroIndex;
        Scene.Heroes.All[validHeroIndex].Attacking = false;

        // Clear all queued actions for heroes ahead.
        PlayerActionQueue.Actions = new Stack<QueuedHeroAction>(PlayerActionQueue.Actions.Where((_, index) => index > ActiveInterfaceIndex));

        DetermineInterfaceActivity();
        interfaces[ActiveInterfaceIndex].PopUpImmediately();
    }

    /// <summary>
    ///     Advances the <see cref="ActiveInterfaceIndex"/>, effectively
    ///     moving the player along to choose the action for the next
    ///     hero, or ending the turn if every hero has been given an action to do.
    /// </summary>
    public void AdvanceInterfaceIndex()
    {
        ActiveInterfaceIndex++;

        int validHeroIndex = -1;
        for (int i = ActiveInterfaceIndex; i < Scene.Heroes.All.Length; i++)
        {
            if (Scene.Heroes.All[i].HP <= 0)
                continue;

            validHeroIndex = i;
            break;
        }
        if (validHeroIndex == -1)
        {
            FinishPlayerChoices();
            return;
        }

        ActiveInterfaceIndex = validHeroIndex;

        if (ActiveInterfaceIndex >= interfaces.Length)
            FinishPlayerChoices();
    }

    /// <summary>
    ///     Performs clean-up work for when the player
    ///     has made all choices for the turn for the heroes.
    /// </summary>
    private void FinishPlayerChoices()
    {
        ActiveInterfaceIndex = 0;
        CurrentState = State.PerformingChosenPlayerActions;
    }

    /// <summary>
    ///     Prepares this battle menu for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, TextRenderer text, float opacity)
    {
        float bottomDy = (1f - AppearanceProgress).Squared() * GameResolution.Y * 0.4f;
        PrepareHealthPortraitIndicators(sprites, opacity, bottomDy);
        PrepareTextZone(sprites, text, opacity, bottomDy);
        RenderTensionPointUI(sprites, text, opacity);
    }

    private void PrepareHealthPortraitIndicators(SpriteRenderer sprites, float opacity, float dy)
    {
        Vector2 bottom = new Vector2(0.5f, 1f) * GameResolution - Vector2.UnitY * DedicatedTextSpaceHeight;
        bottom.Y += dy;

        float left = -1f;
        float right = GameResolution.X + 1f;
        AtlasEntry portraitDelimiter = Atlases.KnightScene.BattleMenuPortraitDelimiter;

        Vector2 size = new Vector2(float.Abs(right - left), 1f);
        Vector2 center = new Vector2((left + right) * 0.5f, bottom.Y - size.Y * portraitDelimiter.Area.Height * 0.5f + 2f);

        sprites.Enqueue(portraitDelimiter, new SpriteData(portraitDelimiter)
        {
            Tint = Color.White * opacity,
            Position = new Vector3(center, 0f),
            Scale = size
        });

        float dedicatedPortraitSpace = float.Min(0.3333f, 1f / Scene.Heroes.All.Length) * size.X;

        for (int i = 0; i < Scene.Heroes.All.Length; i++)
        {
            float positionInterpolant = i / (float)Scene.Heroes.All.Length;
            if (Scene.Heroes.All.Length == 1)
                positionInterpolant = 0.5f;

            float popupAscent = 0f;
            if (i < interfaces.Length)
                popupAscent = interfaces[i].PopupAscent;

            float scale = 1f;
            float x = float.Lerp(left + dedicatedPortraitSpace * 0.25f, right, positionInterpolant);

            if (Scene.Heroes.All.Length == 2)
                x = float.Lerp(x, center.X, 0.35f);

            float y = center.Y + 4f - popupAscent;
            Vector3 portraitEnd = RenderPortrait(sprites, Scene.Heroes.All[i], new Vector3(x, y, 10f), i, opacity, scale);

            Vector3 interfaceCenter = new Vector3(x, y, 10f);
            Vector3 interfaceBottom = new Vector3(x, y + popupAscent + scale * portraitDelimiter.Area.Height * 0.5f - 10f, 5f);

            if (i < interfaces.Length && weirdBugCountdownThing <= 0f)
                interfaces[i].PrepareRender(sprites, interfaceCenter, interfaceBottom, portraitEnd, scale, opacity, i);
        }
    }

    private Vector3 RenderPortrait(SpriteRenderer sprites, Hero hero, Vector3 drawCenter, int index, float opacity, float scale = 1f)
    {
        Color iconColor = Color.White;
        AtlasEntry icon = hero.ActiveBattleMenuIcon;
        bool activeInterface = index < interfaces.Length && interfaces[index].Active;
        if (!activeInterface && PlayerActionQueue.Actions.FirstOrDefault(a => a.Hero.Name == hero.Name) is { } action && action.Button.Icon is not null)
        {
            icon = action.Button.Icon;
            iconColor = hero.ButtonIconColor;
        }
        if (hero.Defending)
        {
            icon = Atlases.KnightScene.DefendIcon;
            iconColor = hero.ButtonIconColor;
        }
        if (hero.Attacking)
        {
            icon = Atlases.KnightScene.FightIcon;
            iconColor = hero.ButtonIconColor;
        }

        Vector3 currentPosition = drawCenter;

        sprites.Enqueue(icon, new SpriteData(icon)
        {
            Tint = iconColor * opacity,
            Position = currentPosition + Vector3.UnitZ * 10f,
            Scale = Vector2.One * scale
        });

        float namePadding = 27.5f;
        currentPosition.X += (icon.Area.Width + namePadding) * scale;

        AtlasEntry nameSprite = hero.BattleMenuNameSprite;
        sprites.Enqueue(nameSprite, new SpriteData(nameSprite)
        {
            Tint = Color.White * opacity,
            Position = currentPosition + new Vector3(0f, -2f, 10f),
            Scale = Vector2.One * scale
        });

        float hpBarPadding = -30f;
        currentPosition.X += (nameSprite.Area.Width + hpBarPadding) * scale;

        AtlasEntry hpText = Atlases.KnightScene.BattleMenuHPText;
        float hpTextPadding = 20f;
        float hpTextLength = hpText.Area.Width;
        Vector3 hpTextDrawPosition = currentPosition + new Vector3(6f, 9f, 32f);
        sprites.Enqueue(hpText, new SpriteData(hpText)
        {
            Tint = Color.White * opacity,
            Position = hpTextDrawPosition,
            Scale = Vector2.One * scale * 0.85f
        });

        currentPosition.X += (hpTextLength + hpTextPadding) * scale * 0.5f;

        float hpRatio = float.Clamp(hero.HP / (float)hero.MaxHP, 0f, 1f);
        Vector2 barSize = new Vector2(120f, 16f);
        AtlasEntry pixel = Atlases.KnightScene.Pixel;

        // Active HP bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Tint = hero.BattleMenuBorderColor * opacity,
            Position = currentPosition + new Vector3(0.5f, 0f, 0f) * barSize.X * hpRatio * scale + new Vector3(0f, 9f, 13f),
            Scale = Vector2.One * scale * barSize * new Vector2(hpRatio, 1f)
        });

        // Lost HP bar.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Tint = new Color(128, 0, 8) * opacity,
            Position = currentPosition + new Vector3(0.5f, 0f, 0f) * barSize.X * scale + new Vector3(0f, 9f, 10.5f),
            Scale = Vector2.One * scale * barSize
        });

        Vector3 barCenter = currentPosition + new Vector3(0.5f, 0f, 0f) * barSize.X * scale + new Vector3(0f, 9f, 2f);
        Vector3 barTop = barCenter - Vector3.UnitY * barSize.Y * scale;

        float hpTextScaleFactor = 0.75f;
        AtlasEntry hpDivider = Atlases.KnightScene.BattleMenuHPDivider;
        Vector3 dividerCenter = barTop + new Vector3(2f, -6f, 8f) * scale;
        sprites.Enqueue(hpDivider, new SpriteData(hpDivider)
        {
            Tint = Color.White * opacity,
            Position = dividerCenter,
            Scale = Vector2.One * scale * hpTextScaleFactor
        });

        Color hpTextColor = Color.White;
        if (hero.HP <= hero.MaxHP * 0.25f)
            hpTextColor = new Color(255, 255, 0);
        if (hero.HP <= 0)
            hpTextColor = new Color(255, 0, 0);

        RenderNumberWithFreakyFauxFont(sprites, hpTextColor, hero.HP, dividerCenter + new Vector3(-18f, 0f, 20f) * scale, Vector2.One * scale * hpTextScaleFactor * 2f, -1, opacity);
        RenderNumberWithFreakyFauxFont(sprites, hpTextColor, hero.MaxHP, dividerCenter + new Vector3(24f, 0f, 20f) * scale, Vector2.One * scale * hpTextScaleFactor * 2f, 1, opacity);

        currentPosition.X += barSize.X * scale;

        return currentPosition;
    }

    private static void RenderNumberWithFreakyFauxFont(SpriteRenderer sprites, Color color, int number, Vector3 drawStart, Vector2 scale, int horizontalDirection, float opacity)
    {
        horizontalDirection = int.Sign(horizontalDirection);

        string numberText = number.ToString();
        int start = 0;
        int end = numberText.Length - 1;
        if (horizontalDirection < 0)
            (start, end) = (end, start);

        Vector3 drawPosition = drawStart;
        AtlasEntry fauxFont = Atlases.KnightScene.TobyWhyIsThisNotAFont;
        for (int i = start; i != end + horizontalDirection; i += horizontalDirection)
        {
            int frame = 0;
            switch (numberText[i])
            {
                case '0':
                    frame = 0;
                    break;
                case '1':
                    frame = 1;
                    break;
                case '2':
                    frame = 2;
                    break;
                case '3':
                    frame = 3;
                    break;
                case '4':
                    frame = 4;
                    break;
                case '5':
                    frame = 5;
                    break;
                case '6':
                    frame = 6;
                    break;
                case '7':
                    frame = 7;
                    break;
                case '8':
                    frame = 8;
                    break;
                case '9':
                    frame = 9;
                    break;
                case '-':
                    frame = 10;
                    break;
            }
            sprites.Enqueue(fauxFont, new SpriteData(fauxFont, 0, frame, 1, 11)
            {
                Position = drawPosition,
                Scale = scale,
                Tint = color * opacity
            });

            drawPosition.X += horizontalDirection * (fauxFont.Area.Width + 1f) * scale.X;
        }
    }

    private void PrepareTextZone(SpriteRenderer sprites, TextRenderer text, float opacity, float dy)
    {
        Vector2 bottom = new Vector2(0.5f, 1f) * GameResolution;
        bottom.Y += dy;

        Vector2 size = new Vector2(GameResolution.X, DedicatedTextSpaceHeight);
        Vector2 center = bottom - Vector2.UnitY * size * 0.5f;
        AtlasEntry pixel = Atlases.KnightScene.Pixel;

        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Tint = Color.Black * opacity,
            Position = new Vector3(center, -20f),
            Scale = size + Vector2.UnitX
        });

        if (CurrentState == State.PlayerActionSelectionOngoing || BottomsideOverlay is AttackMinigameInterface || BottomsideOverlay is ManualTextInterface)
            BottomsideOverlay?.PrepareRender(sprites, text, new Vector3(center, 50f), size);
    }

    private void RenderTensionPointUI(SpriteRenderer sprites, TextRenderer text, float opacity)
    {
        float tensionPoints = Scene.BattleHandler.VisualTensionPoints;
        float tpFillProgress = float.InverseLerp(0f, 100f, tensionPoints);
        float trueTpFillProgress = float.InverseLerp(0f, 100f, Scene.BattleHandler.ConsideredTensionPoints);
        float tpFillProgressMinusFirstCost = tpFillProgress;
        if (Scene.BattleHandler.ConsideredPayment is { } payment)
            tpFillProgressMinusFirstCost += payment.Payment / 100f;

        float deltaFillProgress = trueTpFillProgress - tpFillProgress;

        Vector3 drawPosition = new Vector3(GameResolution.X * 0.0685f, GameResolution.Y * 0.3f, 0f);
        drawPosition.X -= float.Smoothstep(1f - AppearanceProgress) * GameResolution.X * 0.12f;

        Vector2 tpNumberScale = Vector2.One * 1.5f;
        AtlasEntry tpNumberThing = Atlases.KnightScene.TensionPointNumber;
        if (tpFillProgress >= 1f)
            tpNumberThing = Atlases.KnightScene.TensionPointNumberless; // "NumberThing" and it's numberless. Heretical...

        sprites.Enqueue(tpNumberThing, new SpriteData(tpNumberThing)
        {
            Position = drawPosition,
            Tint = Color.White * opacity,
            Scale = tpNumberScale
        });

        if (tpFillProgress < 1f)
        {
            string percentage = ((int)tensionPoints).ToString();
            text.Enqueue(percentage, font, drawPosition + Vector3.UnitY * 7f, Color.White * opacity, Vector2.One, Vector2.One * 0.5f);
        }
        else
        {
            Color maxColor = new Color(255, 255, 0) * opacity;
            text.Enqueue("M", font, drawPosition + new Vector3(-6f, 7f, 0f), maxColor, Vector2.One, Vector2.One * 0.5f);
            text.Enqueue("A", font, drawPosition + new Vector3(-1f, 40f, 0f), maxColor, Vector2.One, Vector2.One * 0.5f);
            text.Enqueue("X", font, drawPosition + new Vector3(4f, 73f, 0f), maxColor, Vector2.One, Vector2.One * 0.5f);
        }

        drawPosition.X += tpNumberThing.Area.Width * tpNumberScale.X;

        Vector2 tpBarScale = Vector2.One * 0.8f;
        AtlasEntry tpBar = Atlases.KnightScene.TensionPointBar;
        AtlasEntry tpBarOutline = Atlases.KnightScene.TensionPointBarOutline;
        sprites.Enqueue(tpBarOutline, new SpriteData(tpBarOutline)
        {
            Position = drawPosition - Vector3.UnitY * tpBarScale.Y,
            Tint = Color.White * opacity,
            Scale = tpBarScale
        });

        SpriteData tpBarData = new SpriteData(tpBar)
        {
            Position = drawPosition,
            Tint = new Color(128, 0, 0) * opacity,
            Scale = tpBarScale
        };
        sprites.Enqueue(tpBar, tpBarData);
        float[] points =
        [
            0f,
            1f - float.Max(tpFillProgress, trueTpFillProgress),
            1f - float.Min(tpFillProgress, trueTpFillProgress),
            1f - tpFillProgressMinusFirstCost,
            1f
        ];

        Color fillColor = tpFillProgress >= 1f ? new Color(255, 208, 32) : new Color(255, 160, 64);

        for (int i = 0; i < points.Length - 1; i++)
        {
            Color color = fillColor;
            if (i == 0)
                color = Color.Transparent;
            if (i == 1)
                color = deltaFillProgress >= 0f ? Color.White : new Color(255, 0, 0);
            if (i == 2)
            {
                float wave = float.Abs(float.Sin(Scene.Time.ElapsedTime * 5f));
                float alpha = wave * 0.5f + 0.2f;
                color = Color.Lerp(color, Color.White, alpha * 0.3f);
                color = new Color(color.R, color.G, color.B, 1f) * alpha;
            }

            SpriteData barPieceData = new SpriteData(tpBar)
            {
                Position = drawPosition,
                Tint = color * opacity,
                Scale = tpBarScale
            };
            Rectangle area = barPieceData.Frame.VerticalSubdivide(points[i], points[i + 1]);
            barPieceData.Position.Y = drawPosition.Y + float.Lerp(0f, tpBar.Area.Height * tpBarScale.Y * 0.5f, points[i]) - (1f - points[i + 1]) * tpBar.Area.Height * tpBarScale.Y * 0.5f;
            barPieceData.Position.Z += 9f;
            barPieceData.Frame = area;
            sprites.Enqueue(tpBar, barPieceData);

            if (i == 1 && tpFillProgress < 1f)
            {
                barPieceData.Position.Y -= area.Height * barPieceData.Scale.Y * 0.5f;

                area = new Rectangle(area.Left, area.Top, area.Width, 4);
                barPieceData.Frame = area;
                barPieceData.Tint = Color.White * opacity;
                barPieceData.Position.Z += 20f;
                sprites.Enqueue(tpBar, barPieceData);
            }
        }
    }
}
