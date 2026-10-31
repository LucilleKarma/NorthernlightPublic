using System.Numerics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Physics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Enemies;

/// <summary>
///     Represents an instance of the Roaring Knight
///     in the battle sequence.
/// </summary>
public sealed class RoaringKnight : Enemy
{
    private float existenceTimer;

    private float shake;

    private float hesitationTimer;

    private readonly KnightAttackSelector attackSelector;

    private readonly OwnedTimer afterimageTimer;

    private readonly KnightRenderState idle = new KnightRenderState(Atlases.KnightScene.KnightIdle, totalFrames: 1);

    private readonly KnightRenderState sideADefeat = new KnightRenderState(Atlases.KnightScene.KnightWeak, totalFrames: 1);

    private readonly KnightRenderState hesitantStart = new KnightRenderState(Atlases.KnightScene.KnightHesitant, totalFrames: 3, loops: false);

    private readonly KnightRenderState hesitantEnd = new KnightRenderState(Atlases.KnightScene.KnightHesitant, totalFrames: 3, loops: false, animatesInReverse: true);

    /// <summary>
    ///     The timer responsible for all animations
    ///     of the Knight.
    /// </summary>
    public readonly OwnedTimer AnimationTimer;

    public sealed class KnightRenderState(AtlasEntry texture, int totalFrames, bool loops = true, bool animatesInReverse = false)
    {
        /// <summary>
        ///     Whether this render state loops or not.
        /// </summary>
        public bool Loops
        {
            get;
            set;
        } = loops;

        /// <summary>
        ///     Whether this animation should render
        ///     in reverse or not.
        /// </summary>
        public bool AnimatesInReverse
        {
            get;
            set;
        } = animatesInReverse;

        /// <summary>
        ///     The texture to use.
        /// </summary>
        public AtlasEntry Texture
        {
            get;
        } = texture;

        /// <summary>
        ///     The current frame.
        /// </summary>
        public int CurrentFrame
        {
            get;
            set;
        } = animatesInReverse ? (totalFrames - 1) : 0;

        /// <summary>
        ///     The amount of frames that the texture possesses.
        /// </summary>
        public int TotalFrames
        {
            get;
        } = totalFrames;

        /// <summary>
        ///     Clones this render state and returns the result.
        /// </summary>
        public KnightRenderState Clone() => new KnightRenderState(Texture, TotalFrames, Loops, AnimatesInReverse)
        {
            CurrentFrame = CurrentFrame
        };
    }

    /// <summary>
    ///     The overriding amount of afterimages the Knight
    ///     should emit per second.
    /// </summary>
    public int AfterimageCountPerSecondOverride
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the Knight performed a slash last frame.
    /// </summary>
    public bool JustSlashed
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the Knight's sprite should be visually flipped.
    /// </summary>
    public bool HorizontallyFlip
    {
        get;
        set;
    }

    /// <summary>
    ///     The opacity of the Knight.
    /// </summary>
    public float Opacity
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     Whether the Knight should render as a silhouette or not.
    /// </summary>
    public bool SilhouetteMode
    {
        get;
        set;
    }

    /// <summary>
    ///     How much the Knight has begun to fire
    ///     up in anticipation of their final attack.
    /// </summary>
    public float FireUpGlow
    {
        get;
        set;
    }

    /// <summary>
    ///     How long the Knight has to wait before being able
    ///     to automatically return to its resting position.
    /// </summary>
    public float RepositionDelay
    {
        get;
        set;
    }

    /// <summary>
    ///     The speedup factor for the Knight's slash animations.
    /// </summary>
    public float SlashAnimationSpeedup
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     How much longer the Knight should perform
    ///     its hurt animation visuals.
    /// </summary>
    public float HurtAnimationCountdown
    {
        get;
        set;
    }

    /// <summary>
    ///     How much longer the Knight should wait
    ///     before resetting its render override delegate.
    /// </summary>
    public float RenderOverrideResetCountdown
    {
        get;
        set;
    }

    /// <summary>
    ///     The frame animation rate speedup factor.
    /// </summary>
    public float FrameUpdateRateSpeedup
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     Whether the Knight should emit
    ///     afterimages this frame or not.
    /// </summary>
    public bool EmitsAfterimages
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     Whether the Knight is being spared
    ///     this turn or not.
    /// </summary>
    public bool BeingSpared
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the Knight is being rendered in
    ///     the background currently or not.
    /// </summary>
    public bool InBackground
    {
        get;
        set;
    }

    /// <summary>
    ///     The Knight's rotation.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }

    /// <summary>
    ///     The spawn position of the Knight.
    /// </summary>
    public Vector3 SpawnPosition
    {
        get;
        private set;
    }

    /// <summary>
    ///     The Knight's velocity.
    /// </summary>
    public Vector3 Velocity
    {
        get;
        set;
    }

    /// <summary>
    ///     The Knight's currently used render state.
    /// </summary>
    public KnightRenderState RenderState
    {
        get;
        set;
    }

    /// <summary>
    ///     The standard render state of the Knight to be
    ///     used in the absence of other states.
    /// </summary>
    public KnightRenderState StandardRenderState
    {
        get
        {
            bool noelleIsTheLastOneStanding = true;
            foreach (Hero hero in scene.Heroes.All)
            {
                if (hero.HP >= 1 && hero != scene.Heroes.Noelle)
                {
                    noelleIsTheLastOneStanding = false;
                    break;
                }
            }

            if (scene.BattleHandler?.BattleIsDoneSideA ?? false)
                return sideADefeat;

            if (!noelleIsTheLastOneStanding || scene.SideB)
                return idle;

            return hesitationTimer < 3.1225f ? hesitantStart : hesitantEnd;
        }
    }

    /// <summary>
    ///     The motion driver for the Knight.
    /// </summary>
    public MotionCharacteristicImbuer Motion
    {
        get;
        private set;
    }

    /// <summary>
    ///     The set of all afterimages the Knight
    ///     maintains.
    /// </summary>
    public readonly List<KnightAfterimage> Afterimages = [];

    /// <summary>
    ///     The Knight's scale.
    /// </summary>
    public float Scale => InBackground ? 0.6f : 1.7f;

    /// <summary>
    ///     The one-frame render override action to apply
    ///     to the Knight.
    /// </summary>
    public Action<SpriteRenderer>? RenderOverrideAction
    {
        get;
        set;
    }

    /// <summary>
    ///     How long it takes for the Knight's sword
    ///     to materialize in their hand.
    /// </summary>
    public static float SwordMaterializationDuration => 0.9f;

    /// <summary>
    ///     The standard frame update rate.
    /// </summary>
    public static float StandardFrameUpdateRate => 0.067f;

    public RoaringKnight(KnightFightScene scene, Vector3 spawnPosition, int maxHp) :
        base(scene, spawnPosition, maxHp)
    {
        SpawnPosition = spawnPosition;

        afterimageTimer = scene.Time.Rent();
        AnimationTimer = scene.Time.Rent();

        RenderState = StandardRenderState;

        attackSelector = new KnightAttackSelector(this);
        StartupAnimation = new KnightBattleStartupAnimation(this);

        Motion = new MotionCharacteristicImbuer(spawnPosition, Vector3.Zero, MotionSystemSettings.Default)
        {
            InUse = false
        };
    }

    /// <summary>
    ///     Sets the Knight's active texture.
    /// </summary>
    public void SetTexture(AtlasEntry entry, int totalFrames, bool loops = true, bool animatesInReverse = false)
    {
        if (RenderState.Texture != entry || RenderState.TotalFrames != totalFrames)
            RenderState = new KnightRenderState(entry, totalFrames, loops, animatesInReverse);
        else
        {
            RenderState = new KnightRenderState(entry, totalFrames, loops, animatesInReverse)
            {
                CurrentFrame = RenderState.CurrentFrame
            };
        }
    }

    /// <summary>
    ///     Limits the texture frame for the Knight.
    /// </summary>
    public void LimitTextureFrame(int maxFrame)
    {
        if (RenderState.CurrentFrame >= maxFrame)
        {
            RenderState.CurrentFrame = maxFrame;
            AnimationTimer.Reset();
        }
    }

    /// <summary>
    ///     Updates this Knight instance.
    /// </summary>
    public override void Update()
    {
        JustSlashed = false;

        if (shake > 0f)
        {
            Vector2 shakeOffset = Vector2.UnitX * float.Sin(float.Tau * existenceTimer * 12f) * shake * 30f;
            shakeOffset.Y += scene.Rng.NextFloat(-1f, 1f) * shake * 8f;

            Velocity = new Vector3(shakeOffset, 0f);
        }
        else if (RepositionDelay <= 0f)
        {
            Vector3 verticalBob = Vector3.UnitY * float.SinPi(existenceTimer * 0.7f) * 50f;
            if (scene.BattleHandler.BattleIsDoneSideA)
                verticalBob *= 0.05f;

            Vector3 moveToRestingPosition = RestingPosition - Position + verticalBob;
            Velocity = Vector3.LerpDt(Velocity, moveToRestingPosition, 30f, scene.Time);
        }
        if (FireUpGlow > 0f)
        {
            float glowUpdateDirection = 1f;
            if (scene.BattleHandler.Attack is FinalAttack && scene.BattleHandler.CurrentState == BattleHandler.State.Attack)
                glowUpdateDirection = -1f;

            FireUpGlow = float.Clamp(FireUpGlow + scene.Time.DeltaTime * glowUpdateDirection * 1.25f, 0f, 1f);
            scene.AddScreenShake(FireUpGlow * 0.75f);
        }

        shake = float.Clamp(shake - scene.Time.DeltaTime * 32f, 0f, 100f);
        RepositionDelay = float.Max(0f, RepositionDelay - scene.Time.DeltaTime);

        Time time = scene.Time;
        Position = Motion.Update(Position, Velocity, time.DeltaTime);
        Motion.InUse = false;

        existenceTimer += time.DeltaTime;
        HurtAnimationCountdown = float.Max(0f, HurtAnimationCountdown - time.DeltaTime);

        if (RenderOverrideResetCountdown <= 0f)
            RenderOverrideAction = null;
        RenderOverrideResetCountdown = float.Max(0f, RenderOverrideResetCountdown - time.DeltaTime);

        int afterimageCountPerSecond = 11;
        if (AfterimageCountPerSecondOverride >= 1)
            afterimageCountPerSecond = AfterimageCountPerSecondOverride;

        float afterimageLifetime = 1.3f;
        float afterimageTravelDistance = Scale * (1f - FireUpGlow) * 78f;
        if (!EmitsAfterimages)
        {
            afterimageTimer.Reset();
            EmitsAfterimages = true;
        }
        else if (afterimageTimer.Cycle(afterimageLifetime / afterimageCountPerSecond))
        {
            AfterimageCountPerSecondOverride = 0;
            Afterimages.Add(new KnightAfterimage()
            {
                RenderState = RenderState.Clone(),
                Lifetime = afterimageLifetime,
                Position = Position - Vector3.UnitZ * 12f,
                HorizontallyFlip = HorizontallyFlip,
                Scale = Vector2.One * Scale,
                Additive = FireUpGlow > 0f,
                Velocity = Vector3.UnitX * (HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / afterimageLifetime + new Vector3(scene.Rng.NextCircular(FireUpGlow * 200f), 0f)
            });
        }

        Animate();

        foreach (KnightAfterimage afterimage in Afterimages)
            afterimage.Update(time);
        Afterimages.RemoveAll(a => a.timer >= a.Lifetime);

        RestingPosition = SpawnPosition;

        if (RenderState == idle || RenderState == hesitantStart || RenderState == hesitantEnd)
            RenderState = StandardRenderState;
        if (RenderState == hesitantStart || RenderState == hesitantEnd)
            hesitationTimer += scene.Time.DeltaTime;
        if (RenderState == hesitantStart)
            hesitantEnd.CurrentFrame = hesitantStart.CurrentFrame;
    }

    private void Animate()
    {
        if (RenderState.TotalFrames == 1)
        {
            AnimationTimer.Reset();
            return;
        }

        int previousFrame = RenderState.CurrentFrame;
        float animationRate = StandardFrameUpdateRate;
        if (RenderState.Texture == Atlases.KnightScene.KnightSlashAnimation)
        {
            if (previousFrame == 1)
                animationRate = 0.4f / SlashAnimationSpeedup;
            if (previousFrame == 2)
                animationRate = 0.85f / SlashAnimationSpeedup;

            if (SlashAnimationSpeedup >= 10f)
                animationRate = 0.2f / SlashAnimationSpeedup;
        }
        if (RenderState.Texture == Atlases.KnightScene.KnightOpenEyeForwardSlash)
        {
            if (previousFrame == 1)
                animationRate = 0.45f / SlashAnimationSpeedup;
            if (previousFrame == 2)
                animationRate = 0.07f / SlashAnimationSpeedup;
            if (previousFrame == 5)
                animationRate = 0.7f / SlashAnimationSpeedup;
        }
        if (RenderState.Texture == Atlases.KnightScene.KnightSlashFlurryAnimation)
            animationRate = StandardFrameUpdateRate / SlashAnimationSpeedup * 0.333f;
        if (RenderState.Texture == Atlases.KnightScene.KnightSwordMaterializationAnimation)
        {
            if (previousFrame == 0)
                animationRate = SwordMaterializationDuration;
            else if (previousFrame < 7)
                animationRate = StandardFrameUpdateRate * 1.8f;
            else
                animationRate = StandardFrameUpdateRate * 1.45f;
        }
        if (RenderState.Texture == Atlases.KnightScene.KnightTaunt)
            animationRate = StandardFrameUpdateRate * 1.74f;

        if (AnimationTimer.Cycle(animationRate / FrameUpdateRateSpeedup))
        {
            int frameIncrement = RenderState.AnimatesInReverse ? -1 : 1;
            if (RenderState.Loops)
            {
                RenderState.CurrentFrame = (RenderState.CurrentFrame + frameIncrement) % RenderState.TotalFrames;
                if (RenderState.CurrentFrame < 0)
                    RenderState.CurrentFrame = RenderState.TotalFrames - 1;
            }
            else
                RenderState.CurrentFrame = int.Clamp(RenderState.CurrentFrame + frameIncrement, 0, RenderState.TotalFrames - 1);

            if (RenderState.Texture == Atlases.KnightScene.KnightOpenEyeForwardSlash && previousFrame == 1)
            {
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), InBackground ? 0.5f : 1f));
                JustSlashed = true;
            }

            if (RenderState.Texture == Atlases.KnightScene.KnightSlashAnimation && previousFrame == 1)
            {
                float slashAfterimageLifetime = 1.5f;
                float afterimageTravelDistance = Scale * 118f;
                Afterimages.Add(new KnightAfterimage()
                {
                    RenderState = new KnightRenderState(Atlases.KnightScene.KnightSlashVisual, 1),
                    Lifetime = slashAfterimageLifetime,
                    Position = Position - Vector3.UnitZ * 12f,
                    HorizontallyFlip = HorizontallyFlip,
                    Scale = Vector2.One * Scale,
                    Velocity = Vector3.UnitX * (HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / slashAfterimageLifetime
                });

                Velocity -= Vector3.UnitX * (HorizontallyFlip ? -1f : 1f) * 6000f;
                scene.AddScreenShake(10f / float.Pow(SlashAnimationSpeedup, 0.625f));
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightBladeSwipe.wav"), InBackground ? 0.5f : 1f));

                JustSlashed = true;
            }
        }
    }

    /// <summary>
    ///     Adds shake to the Knight's visual
    ///     position.
    /// </summary>
    public void AddShake(float shakeIncrease) => shake += shakeIncrease;

    /// <summary>
    ///     Hurts the Knight for
    ///     a given damage quantity.
    /// </summary>
    public override void Hurt(int damage, bool strong)
    {
        base.Hurt(damage, strong);
        if (strong)
        {
            int variant = scene.Rng.Next(2);
            scene.Audio.Play(new Sound(AudioBuffer.FromWav($"KnightHurt{variant + 1}.wav"), 0.9f));
            scene.RoaringKnight.HurtAnimationCountdown = 0.7f;
        }
    }

    // This should never happen naturally.
    public override void OnKill() => scene.DeathScreen.StartAnimation();

    public override Hero ChooseTargetForHurtDamage(BattleHandler battle, int expectedBaseDamage)
    {
        KnightFightScene scene = battle.Scene;

        bool onlyNoelleIsLeft = true;
        foreach (Hero hero in scene.Heroes.All)
        {
            if (hero != scene.Heroes.Noelle && hero.HP >= 1)
            {
                onlyNoelleIsLeft = false;
                break;
            }
        }

        // For narrative reasons, Noelle is never selected
        // as a target while any other present hero remains.
        // She is thusly always targeted last.
        bool okButIsNoelleActuallyHere = scene.Heroes.All.Contains(scene.Heroes.Noelle);
        if (onlyNoelleIsLeft && okButIsNoelleActuallyHere)
            return scene.Heroes.Noelle;

        WeightedRandom<Hero> targetSelector = new WeightedRandom<Hero>(scene.Rng);
        foreach (Hero hero in scene.Heroes.All)
        {
            if (hero == scene.Heroes.Noelle)
                continue;

            if (hero.HP <= 0)
                continue;

            int damageIfHeroWereTargeted = hero.ModifyIncomingDamage(expectedBaseDamage);

            // The Knight targeting already weak heroes creates a snowball
            // effect of targeting whomever randomly gets selected first at
            // random unless they're healed. Too randomly decided in my
            // view, since it effectively leaves it up to chance whether
            // the Knight decides to relentlessly beat down Kris or Susie, the
            // latter of which being considerably stronger of the two.

            // The Knight targeting still-strong heroes naturally does the
            // inverse, where the diffusion of punishment is better
            // and the inexperienced player can use the party member's
            // designated roles to greater effect.
            // This has a downstream effect in theory of potentially being easier
            // difficulty-wise, but honestly I think there's more
            // natural ways of modifying that dial than this.
            float heroHpRatio = float.InverseLerp(0f, hero.MaxHP, hero.HP);
            float selectionWeight = 1f + heroHpRatio * 3.333f;

            // If the hero is defending, they're a bit less likely to be
            // targeted since that means that the Knight would
            // get less damage output overall.
            if (hero.Defending)
                selectionWeight *= 0.6f;

            // The Knight is very smart and so if any heroes
            // can be downed by the attack, they will be.
            if (damageIfHeroWereTargeted >= hero.HP)
                selectionWeight = 10000f;

            targetSelector.Add(hero, selectionWeight);
        }

        if (targetSelector.Empty)
            throw new InvalidOperationException("Where did the heroes of the prophecy go???");

        return targetSelector.Get();
    }

    public override Attack ChooseNextAttack(BattleHandler battle) => attackSelector.Choose(battle);

    public override void OnAttackEnd()
    {
        RenderState = StandardRenderState;
        SlashAnimationSpeedup = 1f;
    }

    public override void PrepareRender(SpriteRenderer sprites)
    {
        int layerPriority = 0;
        if (InBackground)
            layerPriority = 50;

        Color tint = Color.White;
        if (InBackground)
        {
            float darkening = 0.2f;
            Color fountainTint = new Color(239, 199, 219);
            tint *= fountainTint;
            tint *= new Color(float.Clamp(1f - darkening, 0f, 1f));
            tint *= 0.8f;
        }

        Angle rotation = Rotation;
        if (RenderOverrideAction is null && RenderOverrideResetCountdown <= 0f)
        {
            AtlasEntry texture = RenderState.Texture;
            int frame = RenderState.CurrentFrame;
            int totalFrames = RenderState.TotalFrames;
            if (HurtAnimationCountdown > 0f && scene.Rng.NextBool(2))
            {
                texture = Atlases.KnightScene.KnightHurt;
                frame = 0;
                totalFrames = 1;
            }

            Vector3 drawOffset = Vector3.Zero;
            if (RenderState.Texture == Atlases.KnightScene.KnightFlyingAnimation ||
                RenderState.Texture == Atlases.KnightScene.KnightFlyingTransition)
            {
                drawOffset = new Vector3(8f, 20f, 0f) * Scale;
            }

            sprites.Enqueue(texture, new SpriteData(texture, 0, frame, 1, totalFrames) with
            {
                Position = Position + drawOffset,
                Scale = Vector2.One * Scale * new Vector2(HorizontallyFlip ? -1f : 1f, 1f) * 2f,
                Tint = tint * Opacity,
                Rotation = rotation
            }, queuePriorityInfluence: layerPriority);

            float hurtResponseGlow = float.InverseLerp(0.4f, 0.65f, HurtAnimationCountdown);
            if (hurtResponseGlow > 0f)
            {
                AtlasEntry white = Atlases.KnightScene.KnightIdleWhite;
                sprites.Enqueue(white, new SpriteData(white) with
                {
                    Position = Position + drawOffset + Vector3.UnitZ * 12f,
                    Scale = Vector2.One * Scale * new Vector2(HorizontallyFlip ? -1f : 1f, 1f) * 2f,
                    Tint = tint * hurtResponseGlow * Opacity,
                    Rotation = rotation
                }, queuePriorityInfluence: layerPriority);
            }

            float selectionFlashIntensity = 0f;
            if (SelectionFlash)
                selectionFlashIntensity = float.Cos01(scene.Time.ElapsedTime * 5f);
            if (SilhouetteMode)
                selectionFlashIntensity = 1f;
            selectionFlashIntensity = float.Max(selectionFlashIntensity, FireUpGlow);

            float why = (1f - scene.BattleBackBackdropOpacity).Squared();
            if (scene.BattleBackBackdropOpacity >= 1f)
                why = 1f;

            if (selectionFlashIntensity > 0f)
            {
                sprites.Enqueue(texture, new SpriteData(texture, 0, frame, 1, totalFrames) with
                {
                    Position = Position + drawOffset + Vector3.UnitZ * 20f,
                    Scale = Vector2.One * Scale * new Vector2(HorizontallyFlip ? -1f : 1f, 1f) * 2f,
                    Tint = tint * Opacity * selectionFlashIntensity * why,
                    Rotation = rotation
                }, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.FullColorOverlayShader, (_, _2) => { }), queuePriorityInfluence: layerPriority);
            }

            if (RenderState.Texture == Atlases.KnightScene.KnightSwordMaterializationAnimation && RenderState.CurrentFrame < 8)
            {
                float batMaterializeProgress = 1f;
                if (RenderState.CurrentFrame == 0)
                    batMaterializeProgress = float.InverseLerp(0f, SwordMaterializationDuration, AnimationTimer);

                Vector3 batDrawPosition = Position + new Vector3((HorizontallyFlip ? -1f : 1f) * -77.5f, -84f, 0f) * Scale;
                batDrawPosition.Y += float.Cos(batMaterializeProgress * float.Pi * 1.5f) * (1f - batMaterializeProgress) * Scale * 60f;
                batDrawPosition.Z -= 6f;

                float flickerWave = float.Sin01(float.Pi * batMaterializeProgress * 7f);
                float batFlicker = float.Lerp(flickerWave, 1f, batMaterializeProgress.Cubed());
                float batOpacity = batFlicker >= 0.6f ? 1f : 0f;
                AtlasEntry bat = Atlases.KnightScene.KnightBat;
                sprites.Enqueue(bat, new SpriteData(bat) with
                {
                    Position = batDrawPosition,
                    Scale = Vector2.One * Scale * new Vector2(HorizontallyFlip ? -1f : 1f, 1f) * 2f,
                    Tint = tint * Opacity * batOpacity,
                    Rotation = rotation
                }, queuePriorityInfluence: layerPriority);
            }
        }
        else
            RenderOverrideAction?.Invoke(sprites);

        float afterimageGlow = 1f - SlashAnimationSpeedup * 0.03f;
        foreach (KnightAfterimage afterimage in Afterimages)
            afterimage.Prepare(sprites, tint * afterimageGlow * (1f + FireUpGlow * 50f));
    }
}
