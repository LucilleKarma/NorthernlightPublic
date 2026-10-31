using System.Numerics;
using Northernlight.Content.Knight.Dialogue;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Content.Knight.Items;
using Northernlight.Core;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     A mechanism responsible for the handling of
///     attacks in battle.
/// </summary>
public sealed class BattleHandler
{
    public enum State
    {
        BattleStartupAnimation,

        Inactive,
        WaitingForDialogue,
        PreparingSoulAndBox,
        Attack,
        EndAttack,

        SusieKnightClash,
        KnightDeath
    }

    /// <summary>
    ///     Represents an instance of a deferred tension
    ///     point modifier that will be applied in full
    ///     at the end of the player's turn.
    /// </summary>
    public sealed class TensionPointModifier
    {
        public float Payment { get; }

        private TensionPointModifier(float amount)
        {
            Payment = amount;
        }

        public static TensionPointModifier Take(float payment) => new TensionPointModifier(-payment);

        public static TensionPointModifier Give(float amount) => new TensionPointModifier(amount);
    }

    /// <summary>
    ///     The set of all tension point payments
    ///     to incur.
    /// </summary>
    public readonly List<TensionPointModifier> TensionPointPayments = [];

    private State currentState;

    private Vector3 soulPositionAtEndOfAttack;

    private Vector2 boxSizeAtEndOfAttack;

    private readonly KnightClashAnimationHandler knightClashAnimationHandler;

    private readonly KnightDeathAnimationHandler knightDeathAnimationHandler;

    internal float screenWhitening;

    /// <summary>
    ///     The current turn number, starting from one.
    /// </summary>
    public int Turn
    {
        get;
        private set;
    } = 1;

    /// <summary>
    ///     Whether Kris held their breath and the soul
    ///     should thusly more faster.
    /// </summary>
    public bool KrisHeldBreath
    {
        get;
        set;
    }

    /// <summary>
    ///     How many times Kris has held their breath.
    /// </summary>
    public int KrisHoldBreathCounter
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the side A battle is done or not.
    /// </summary>
    public bool BattleIsDoneSideA
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the side B battle is done or not.
    /// </summary>
    public bool BattleIsDoneSideB
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether soul input movement is currently disabled or not.
    /// </summary>
    /// <remarks>
    ///     This value is reset after one frame of movement is cancelled.
    /// </remarks>
    public bool SoulMovementDisabled
    {
        get;
        set;
    }

    /// <summary>
    ///     The animation progress of Kris' soul exit visuals.
    /// </summary>
    public float SoulBodyExitVisualsProgress
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of tension points that the player
    ///     has when factoring in all deferred payments.
    /// </summary>
    public float ConsideredTensionPoints
    {
        get
        {
            var tp = TensionPoints;

            foreach (var payment in TensionPointPayments)
            {
                // Manually clamping at each value is necessary to prevent
                // actions like queueing a Defend at 100% TP before using a
                // magic action, resulting in the action being compensated by
                // the earlier Defend more than it should have.
                tp = float.Clamp(tp + payment.Payment, 0f, 100f);
            }

            return tp;
        }
    }

    /// <summary>
    ///     The current attack.
    /// </summary>
    public Attack? Attack
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of tension points that
    ///     the heroes have accumulated.
    /// </summary>
    public float TensionPoints
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of tension points to display
    ///     to the player.
    /// </summary>
    /// <remarks>
    ///     This value approaches <see cref="ConsideredTensionPoints"/>
    ///     over time.
    /// </remarks>
    public float VisualTensionPoints
    {
        get;
        set;
    }

    /// <summary>
    ///     The currently considered tension point payment by
    ///     the player.
    /// </summary>
    public TensionPointModifier? ConsideredPayment
    {
        get;
        set;
    }

    /// <summary>
    ///     An ever-incrementing timer reserved for
    ///     the current state. Gets reset to zero upon
    ///     state change.
    /// </summary>
    public OwnedTimer StateTimer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The game responsible for this
    ///     battle.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
        private set;
    }

    /// <summary>
    ///     The battle box that the soul is confined to.
    /// </summary>
    public BattleBox Box
    {
        get;
        private set;
    }

    /// <summary>
    ///     The soul.
    /// </summary>
    public Soul Soul
    {
        get;
        private set;
    }

    /// <summary>
    ///     The in-battle dialogue handler.
    /// </summary>
    public CharacterDialogueHandler Dialogue
    {
        get;
        private set;
    }

    /// <summary>
    ///     The current state of this battle.
    /// </summary>
    public State CurrentState
    {
        get => currentState;
        internal set
        {
            if (currentState == value)
                return;

            StateTimer.Reset();
            currentState = value;
        }
    }

    /// <summary>
    ///     The dormant position of the Soul.
    /// </summary>
    public Vector3 DormantSoulPosition => Scene.Heroes.Kris.Position + new Vector3(-10f, -60f, 300f);

    /// <summary>
    ///     The starting position of the box
    ///     when an attack begins.
    /// </summary>
    public static Vector2 StartingBoxCenter => NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 0.375f);

    /// <summary>
    ///     The event fired when this battle handler is finished.
    /// </summary>
    public event Action OnEnd;

    /// <summary>
    ///     The set of all items contained with the
    ///     inventory.
    /// </summary>
    public readonly List<Item> Inventory = [];

    /// <summary>
    ///     The set of all enemies in the battle.
    /// </summary>
    public readonly List<Enemy> Enemies = [];

    public BattleHandler(KnightFightScene scene, params List<Enemy> enemies)
    {
        Enemies = enemies;
        Scene = scene;
        Soul = new Soul(Scene);
        Box = new BattleBox();
        StateTimer = Scene.Time.Rent();
        Dialogue = new CharacterDialogueHandler(this);
        knightDeathAnimationHandler = new KnightDeathAnimationHandler(this);
        knightClashAnimationHandler = new KnightClashAnimationHandler(this);

        RegenerateInventory();
    }

    private void RegenerateInventory()
    {
        Inventory.Clear();
        foreach (VanillaGameItemRegistry.ID id in Scene.SaveData.Inventory)
        {
            if (VanillaGameItemRegistry.TryMakeOfId(id, out Item? item))
                Inventory.Add(item);
        }
    }

    /// <summary>
    ///     Starts an attack.
    /// </summary>
    public void Start()
    {
        // If there is no dialogue, this just instantly gets skipped
        // over next frame.
        CurrentState = State.WaitingForDialogue;
        Dialogue.Evaluate();

        foreach (Hero hero in Scene.Heroes.All)
        {
            hero.SelectionFlash = false;
            if (hero.ActiveAnimation != hero.DefendAnimation && hero.ActiveAnimation != hero.StandardAttackAnticipationAnimation)
                hero.ActiveAnimation = hero.IdleAnimation;
        }
    }

    /// <summary>
    ///     Ends the ongoing attack from an enemy.
    /// </summary>
    /// <remarks>
    ///     This method does nothing if there is no attack ongoing.
    /// </remarks>
    public void EndAttack()
    {
        if (CurrentState == State.Inactive)
            return;

        Turn++;
        soulPositionAtEndOfAttack = Soul.Position;
        boxSizeAtEndOfAttack = Box.Size;
        CurrentState = State.EndAttack;
        ChooseNextAttack();
        OnEnd?.Invoke();
    }

    /// <summary>
    ///     Pays off all deferred tension point payments in the
    ///     <see cref="ConsideredTensionPoints"/> cache.
    /// </summary>
    public void PayOffTensionPointDebt()
    {
        TensionPoints = ConsideredTensionPoints;
        TensionPointPayments.Clear();
    }

    /// <summary>
    ///     Updates this battle.
    /// </summary>
    public void Update()
    {
        Soul.Update();
        Box.Update();

        if (Scene.DeathScreen.PlayerWon)
            return;

        switch (CurrentState)
        {
            case State.BattleStartupAnimation:
                bool done = Enemies[0].StartupAnimation?.Update(this, Scene) ?? true;
                if (done)
                {
                    StateTimer.Reset();
                    CurrentState = State.Inactive;
                }

                break;
            case State.Inactive:
                Soul.StayDormant(DormantSoulPosition);
                Box.CutAngles.Clear();
                if (BattleIsDoneSideB)
                    StartSideBKnightDeathAnimation();
                break;
            case State.WaitingForDialogue:
                screenWhitening = float.Max(0f, screenWhitening - Scene.Time.DeltaTime * 0.5f);
                Soul.StayDormant(DormantSoulPosition);

                if (screenWhitening <= 0.001f)
                {
                    if (BattleIsDoneSideA && !Dialogue.Active)
                        Dialogue.SetLines(new CharacterDialogueLine(Scene.Heroes.Susie, LocalizationEntries.EnoughAlready));

                    Dialogue.Dialogue?.Update();
                    if (!Dialogue.Active)
                    {
                        CurrentState = State.PreparingSoulAndBox;
                        if (BattleIsDoneSideA)
                            CurrentState = State.SusieKnightClash;
                        if (BattleIsDoneSideB)
                            StartSideBKnightDeathAnimation();

                        Box.ClearAfterimages();
                    }
                }
                break;
            case State.PreparingSoulAndBox:
                PrepareSoulAndBox();
                break;
            case State.Attack:
                HandleAttack();
                break;
            case State.EndAttack:
                HandleAttackEnd();
                break;

            case State.SusieKnightClash:
                knightClashAnimationHandler.Update(StateTimer);
                break;
            case State.KnightDeath:
                knightDeathAnimationHandler.Update();
                break;
        }

        float desiredVisualPoints = ConsideredTensionPoints;
        float delta = desiredVisualPoints - VisualTensionPoints;
        float direction = float.Sign(delta);
        float speed = float.Abs(delta) * 3f;
        if (delta > 0f)
            speed = 120f;

        VisualTensionPoints += direction * Scene.Time.DeltaTime * speed;
        if (CurrentState == State.Attack)
            VisualTensionPoints = TensionPoints;
    }

    /// <summary>
    ///     Starts the Side B Knight death animation.
    /// </summary>
    public void StartSideBKnightDeathAnimation()
    {
        // Make the battle menu go away, we're done with it
        // now.
        if (Scene.battleMenu.FadeOutTimer <= 0f)
            Scene.battleMenu.FadeOutTimer = 0.0001f;

        SoulBodyExitVisualsProgress = 1f;
        CurrentState = State.KnightDeath;
        Box.ClearAfterimages();
    }

    /// <summary>
    ///     Selects a hero according to the Knight's targeting
    ///     rules, damages them, and returns the hero that was hit.
    /// </summary>
    /// <remarks>
    ///     If Noelle is the target selected, <paramref name="swoonsOnDowning"/> is ignored, for narrative reasons.
    /// </remarks>
    public Hero Hurt(int damage, bool swoonsOnDowning)
    {
        Enemy enemyDoingDamage = Enemies[Scene.Rng.Next(Enemies.Count)];
        Hero target = enemyDoingDamage.ChooseTargetForHurtDamage(this, damage);

        // Noelle is never hurt to a point of being swooned.
        // This is minor since she'll always be the last
        // one to go down and as such the on-downed freeze-frames
        // will rapidly overtake the game scene before the
        // damage text spells itself out, but it's a
        // good detail nonetheless.
        if (target == Scene.Heroes.Noelle)
            swoonsOnDowning = false;

        target.Hurt(damage, swoonsOnDowning);

        Scene.BattleHandler.Soul.GrazeIndicatorIntensity = 0f;
        return target;
    }

    private void PrepareSoulAndBox()
    {
        if (Attack is null)
            ChooseNextAttack();

        // Normally I'd ease this, but Deltarune
        // doesn't seem to do that here, and so I
        // shall respect that.
        float boxAnimationDuration = 0.4f;
        float soulAnimationProgress = float.InverseLerp(0f, 0.2f, StateTimer);
        if (Attack!.SkipsBoxAppearance)
        {
            boxAnimationDuration = 0.0001f;
            soulAnimationProgress = 1f;
        }

        Soul.Position = Vector3.Lerp(DormantSoulPosition, new Vector3(StartingBoxCenter, DormantSoulPosition.Z), soulAnimationProgress);
        Soul.Opacity = float.InverseLerp(0f, 0.95f, soulAnimationProgress).Squared();
        SoulBodyExitVisualsProgress = soulAnimationProgress;

        float totalBoxSpin = Angle.Pi;
        float boxAnimationProgress = float.InverseLerp(0f, boxAnimationDuration, StateTimer);

        Box.Position = StartingBoxCenter;
        Box.Size = boxAnimationProgress * Attack!.StartingBoxSize;
        Box.Rotation = boxAnimationProgress * totalBoxSpin;
        Box.Opacity = boxAnimationProgress;
        Box.AfterimageZDirection = -1;

        bool animationsFinished = soulAnimationProgress >= 1f && boxAnimationProgress >= 1f;
        if (animationsFinished)
        {
            Box.Rotation = totalBoxSpin;
            SoulBodyExitVisualsProgress = 0f;
            CurrentState = State.Attack;
            Attack!.OnStart();
        }
    }

    private void ChooseNextAttack()
    {
        Enemy attackChooser = Enemies[Scene.Rng.Next(Enemies.Count)];
        Attack = attackChooser.ChooseNextAttack(this);
        Attack.AttackerGeneric = attackChooser;
    }

    private void HandleAttack()
    {
        float movementSpeed = StatsRepository.PlayerMovementSpeed;

        Vector3 movement = Vector3.Zero;
        if (ActiveKeymap.Up.IsPressed)
            movement -= Vector3.UnitY * movementSpeed;
        if (ActiveKeymap.Down.IsPressed)
            movement += Vector3.UnitY * movementSpeed;
        if (ActiveKeymap.Left.IsPressed)
            movement -= Vector3.UnitX * movementSpeed;
        if (ActiveKeymap.Right.IsPressed)
            movement += Vector3.UnitX * movementSpeed;
        if (ActiveKeymap.Cancel.IsPressed)
            movement *= StatsRepository.PlayerSlowdownSpeedFactor;
        if (KrisHeldBreath)
            movement *= StatsRepository.PlayerHoldBreathSpeedFactor;

        if (SoulMovementDisabled)
        {
            movement = Vector3.Zero;
            SoulMovementDisabled = false;
        }

        Soul.Position += movement * Scene.Time.DeltaTime;

        Vector2 lockedSoulPosition = Box.KeepInside(Soul.Position.AsVector2(), Vector2.One * Soul.Scale * 20f);
        Vector2 collisionCorrection = lockedSoulPosition - Soul.Position.AsVector2();
        float correctionDistance = collisionCorrection.Length();
        if (correctionDistance >= 0.0001f)
            Soul.Position += new Vector3(collisionCorrection, 0f);

        Attack?.Update();
        if (Attack is null)
            EndAttack();
    }

    private void HandleAttackEnd()
    {
        float soulAnimationProgress = float.InverseLerp(0f, 0.25f, StateTimer);
        Soul.Position = Vector3.Lerp(soulPositionAtEndOfAttack, DormantSoulPosition, soulAnimationProgress);
        Soul.Opacity = float.InverseLerp(1f, 0f, soulAnimationProgress).Squared();
        SoulBodyExitVisualsProgress = soulAnimationProgress;

        float boxClearDuration = 0.5f;
        float boxClearProgress = float.InverseLerp(0f, boxClearDuration, StateTimer);
        boxClearProgress = float.Smoothstep(boxClearProgress);

        float totalBoxSpin = 2.3f;
        Box.Size = Vector2.One * float.Pow(1f - boxClearProgress, 1.25f) * boxSizeAtEndOfAttack;
        Box.Rotation += Scene.Time.DeltaTime / boxClearDuration * totalBoxSpin;
        Box.AfterimageZDirection = -1;

        for (int i = 0; i < Box.afterimages.Count; i++)
        {
            float progress = 1f - i / (float)Box.afterimages.Count;
            float opacity = float.InverseLerp(1.6f, 1.1f, boxClearProgress * 0.9f + progress * 0.9f) * Box.Opacity;
            opacity *= 1f - progress;

            Box.afterimages[i] = Box.afterimages[i] with
            {
                Opacity = opacity
            };
        }

        if (soulAnimationProgress >= 1f && StateTimer >= boxClearDuration + 2f)
        {
            Box.CutAngles.Clear();

            Box.Opacity = 1f;
            Box.ClearAfterimages(false);
            SoulBodyExitVisualsProgress = 0f;
            Box.Size = Vector2.Zero;
            CurrentState = State.Inactive;

            Box.OnAttackEnd();
        }
    }

    /// <summary>
    ///     Prepares various renderable elements for
    ///     this battle.
    /// </summary>
    public void PrepareRender(Renderer renderer, SpriteRenderer sprites, TextRenderer text, MeshRenderer trails, FrameContext frame)
    {
        int maxAfterimageCount = 1000;
        if (CurrentState == State.Attack)
            maxAfterimageCount = 1;

        if ((!Attack?.OverrideBattleBoxRender(renderer, frame)) ?? true)
            Box.PrepareRender(sprites, trails, frame, maxAfterimageCount);
        Attack?.PrepareRender(sprites, frame);
        Dialogue?.Dialogue?.PrepareRender(sprites, text);

        if (SoulBodyExitVisualsProgress > 0f && SoulBodyExitVisualsProgress < 1f && Scene.Game.CurrentScene is KnightFightScene scene &&
            !scene.DeathScreen.PlayerDied)
        {
            AtlasEntry soulVisual = Atlases.KnightScene.SoulEnterVisual;
            for (int i = 0; i < 4; i++)
            {
                float expandProgress = (i / 4f + SoulBodyExitVisualsProgress * 0.5f) % 1f;
                float opacity = float.Cbrt(1f - expandProgress) * float.Sqrt(1f - SoulBodyExitVisualsProgress);
                float scale = expandProgress * Soul.Scale * 3f;
                sprites.Enqueue(soulVisual, new SpriteData(soulVisual)
                {
                    Position = DormantSoulPosition,
                    Tint = new Color(1f, 1f, 1f) * opacity * 1.5f,
                    Scale = new Vector2(1.6f, 0.9f) * scale
                }, BlendState.NonPremultiplied);
            }
        }

        if (screenWhitening > 0f)
        {
            AtlasEntry pixel = Atlases.Menu.Pixel;
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = Vector3.UnitZ * 99f,
                Scale = Vector2.One * 99999f,
                Tint = Color.White * screenWhitening
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }

        knightDeathAnimationHandler.PrepareRender(sprites);
    }
}
