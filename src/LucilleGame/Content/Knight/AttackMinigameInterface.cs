using System.Numerics;
using Northernlight.Content.Knight.Particles;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

public sealed class AttackMinigameInterface : BattleMenuBottomsideOverlay
{
    /// <summary>
    ///     The set of all heroes available.
    /// </summary>
    public Hero[] AllHeroes
    {
        get;
    }

    /// <summary>
    ///     The set of all strikes incoming.
    /// </summary>
    public StrikeState[] IncomingStrikes
    {
        get;
    }

    public sealed class BarReticleInstance
    {
        /// <summary>
        ///     How long this reticle has existed for, in seconds.
        /// </summary>
        public float ExistenceTimer
        {
            get;
            set;
        }

        /// <summary>
        ///     The time value of this reticle relative
        ///     to the critical strick.
        /// </summary>
        public float Time
        {
            get;
            set;
        }

        /// <summary>
        ///     The scale factor of this reticle.
        /// </summary>
        public float ScaleFactor
        {
            get;
            set;
        } = 1f;

        /// <summary>
        ///     The opacity of this reticle.
        /// </summary>
        public float Opacity
        {
            get;
            set;
        }

        /// <summary>
        ///     Updates this reticle instance.
        /// </summary>
        public void Update(BattleMenu menu)
        {
            ExistenceTimer += menu.Scene.Time.DeltaTime;
        }

        /// <summary>
        ///     Prepares this reticle for rendering.
        /// </summary>
        public void PrepareRender(SpriteRenderer sprites, Color color, Vector3 criticalPoint, float opacity)
        {
            if (Opacity <= 0f)
                return;

            AtlasEntry glow = Atlases.KnightScene.AttackBarReticleThingGlow;

            float overallOpacity = Opacity * opacity;
            Vector3 drawPosition = criticalPoint + Vector3.UnitX * Scale * Time / StandardStrikeDelay * 200f;
            drawPosition.Z += overallOpacity * 200f;

            sprites.Enqueue(glow, new SpriteData(glow)
            {
                Position = drawPosition,
                Scale = Vector2.One * Scale * new Vector2(1f, 1.6f) * ScaleFactor,
                Tint = color * overallOpacity
            }, BlendState.Additive);
        }
    }

    public sealed class StrikeState
    {
        private float postStrikeTimer;

        private float hurtSoundCountdown;

        private OwnedTimer reticleTimer;

        private readonly List<BarReticleInstance> afterimages = [];

        private readonly KnightFightScene scene;

        /// <summary>
        ///     Whether the player performed the strike
        ///     attempt yet or not.
        /// </summary>
        public bool StrikeAttemptCompleted
        {
            get;
            set;
        }

        /// <summary>
        ///     The amount of tension points to award the player
        ///     with upon hitting the target.
        /// </summary>
        public float TensionPointsToAward
        {
            get;
            set;
        }

        /// <summary>
        ///     Whether the attack missed or not.
        /// </summary>
        public bool Missed
        {
            get;
            set;
        }

        /// <summary>
        ///     The main reticle.
        /// </summary>
        public BarReticleInstance MainReticle
        {
            get;
            set;
        } = new BarReticleInstance();

        /// <summary>
        ///     The hero creating this strike.
        /// </summary>
        public required Hero Hero
        {
            get;
            init;
        }

        /// <summary>
        ///     How long this strike has before it
        ///     reaches its most critical state, in seconds.
        /// </summary>
        public float RemainingTimeUntilCritical
        {
            get;
            set;
        }

        /// <summary>
        ///     Whether the player received a successful
        ///     critical strike or not.
        /// </summary>
        public bool SuccessfulCriticalStrike
        {
            get;
            set;
        }

        public StrikeState(float remainingTimeUntilCritical, KnightFightScene scene)
        {
            this.scene = scene;
            RemainingTimeUntilCritical = remainingTimeUntilCritical;
        }

        internal float CalculateAccuracyScore()
        {
            float accuracyScore = 1f;
            float normalizedStrikeTime = RemainingTimeUntilCritical / StandardStrikeDelay;
            if (SuccessfulCriticalStrike)
                accuracyScore = 1.5f;
            else if (normalizedStrikeTime >= 0f)
            {
                if (normalizedStrikeTime < 0.07f)
                    accuracyScore = 1.2f;
                else if (normalizedStrikeTime < 0.1f)
                    accuracyScore = 1.1f;
                else
                    accuracyScore = 1f - normalizedStrikeTime;
            }

            return float.Max(0f, accuracyScore);
        }

        private int CalculateDamage()
        {
            int damage = (int)float.Ceiling(Hero.BaseAttackDamage * scene.HeroStats.GeneralDamageFactor);
            return (int)float.Ceiling(damage * CalculateAccuracyScore());
        }

        /// <summary>
        ///     Updates this strike state.
        /// </summary>
        public void Update(BattleMenu menu)
        {
            reticleTimer ??= menu.Scene.Time.Rent();

            if (!StrikeAttemptCompleted)
            {
                RemainingTimeUntilCritical -= menu.Scene.Time.DeltaTime;
                if (reticleTimer.Cycle(0.07f) && RemainingTimeUntilCritical > 0f)
                {
                    afterimages.Add(new BarReticleInstance()
                    {
                        Time = RemainingTimeUntilCritical
                    });
                }

                hurtSoundCountdown = Hero.StandardAttackAnimation.Duration;
            }
            else
            {
                postStrikeTimer += menu.Scene.Time.DeltaTime;

                if (hurtSoundCountdown > 0f)
                {
                    hurtSoundCountdown -= menu.Scene.Time.DeltaTime;
                    if (hurtSoundCountdown <= 0f)
                    {
                        int damage = CalculateDamage();
                        if (Missed)
                            damage = 0;
                        else
                        {
                            menu.Scene.AddScreenShake(10f);
                            menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav(Hero.AttackHitSound)));
                            menu.Scene.RoaringKnight.AddShake(16f);
                            menu.Scene.RoaringKnight.Hurt(damage, false);

                            menu.Scene.HitImpacts.Add(new HitImpact()
                            {
                                Position = menu.Scene.RoaringKnight.Position + new Vector3(25f, 45f, 120f),
                                Texture = Hero.HitImpactTexture,
                                TotalFrames = Hero.TotalHitImpactFrames
                            });
                        }

                        DamageIndicator indicator = menu.Scene.RoaringKnight.CreateHurtTextParticle(damage, Hero.DamageColor);
                        indicator.Miss = Missed;

                        menu.Scene.BattleHandler.TensionPoints = float.Clamp(menu.Scene.BattleHandler.TensionPoints + TensionPointsToAward, 0f, 100f);
                        menu.Scene.BattleHandler.VisualTensionPoints = float.Clamp(menu.Scene.BattleHandler.VisualTensionPoints + TensionPointsToAward, 0f, 100f);
                    }
                }
            }

            foreach (BarReticleInstance afterimage in afterimages)
            {
                afterimage.Update(menu);
                afterimage.Opacity = float.InverseLerp(0.6f, 0f, afterimage.ExistenceTimer).Squared();
            }
            afterimages.RemoveAll(a => a.Opacity <= 0.2f);

            MainReticle.Update(menu);
            MainReticle.Opacity = float.InverseLerp(-0.24f, 0f, RemainingTimeUntilCritical / StandardStrikeDelay);
            MainReticle.Time = RemainingTimeUntilCritical;
            if (postStrikeTimer > 0f)
            {
                float maxScaleIncrease = SuccessfulCriticalStrike ? 2.5f : 1f;
                MainReticle.ScaleFactor = 1f + float.InverseLerp(0f, 0.4f, postStrikeTimer) * maxScaleIncrease;
                MainReticle.Opacity *= 1f - float.InverseLerp(0.05f, 0.4f, postStrikeTimer);
            }
        }

        /// <summary>
        ///     Clears all afterimages.
        /// </summary>
        public void ClearAfterimages() => afterimages.Clear();

        /// <summary>
        ///     Prepares this strike for rendering.
        /// </summary>
        public void PrepareRender(SpriteRenderer sprites, BlendState blendState, Vector3 left, float opacity, float boxHeight, int index, int heroCount)
        {
            Vector3 drawPosition = left;
            AtlasEntry icon = Hero.BattleMenuIcon;
            drawPosition.X += icon.Area.Width * Scale * 0.5f;

            Vector3 iconDrawPosition = drawPosition;

            drawPosition.X = left.X + Scale * 100f;

            float pressIndicatorScale = Scale;
            AtlasEntry pressIndicator = Atlases.KnightScene.AttackPressIndicator;
            Vector3 pressIndicatorDrawPosition = drawPosition - new Vector3(0f, Scale * 14f, -100f);

            float horizontalPaddingToBox = Scale * 10f;
            drawPosition.X += pressIndicator.Area.Width * pressIndicatorScale * 0.5f + horizontalPaddingToBox;

            AtlasEntry attackBoxInner = Atlases.KnightScene.AttackBoxInner;
            float boxStretch = 1.5f;
            Vector2 boxScale = new Vector2(boxStretch, 1f) * boxHeight / attackBoxInner.Area.Height;
            Vector3 boxDrawPosition = drawPosition + Vector3.UnitX * boxScale.X * attackBoxInner.Area.Width * 0.5f;

            float lineScale = boxHeight * boxStretch * 6f;
            bool drawAtTop = index == heroCount - 1 && heroCount >= 2;
            float verticalDirection = drawAtTop ? -1f : 1f;
            AtlasEntry pixel = Atlases.KnightScene.Pixel;
            Vector3 barDrawPosition = drawPosition + new Vector3(lineScale * 0.5f + 2f, boxScale.Y * (attackBoxInner.Area.Height * 0.5f - 1.5f) * verticalDirection, -10f);

            AtlasEntry critical = Atlases.KnightScene.AttackBarReticleThing;
            Vector3 criticalPoint = drawPosition + new Vector3(critical.Area.Width * Scale, 0f, 60f);
            foreach (BarReticleInstance afterimage in afterimages)
                afterimage.PrepareRender(sprites, Color.White, criticalPoint, opacity);

            Color reticleColor = SuccessfulCriticalStrike ? new Color(255, 255, 0) : Color.White;
            MainReticle.PrepareRender(sprites, reticleColor, criticalPoint, opacity);

            if (scene.Heroes.All.Length >= 3)
            {
                sprites.Enqueue(pixel, new SpriteData(pixel)
                {
                    Position = barDrawPosition,
                    Scale = new Vector2(lineScale, 3f),
                    Tint = new Color(0, 0, 175) * opacity
                }, blendState, drawToDepthBuffer: false);
            }

            float attackImpactProgress = float.InverseLerp(0f, 0.3f, postStrikeTimer);
            float attackImpact = float.Bump(attackImpactProgress).Squared() * 0.8f;
            Color attackBorderColor = Color.Lerp(Hero.AttackBorderColor, Color.White, attackImpact);

            sprites.Enqueue(attackBoxInner, new SpriteData(attackBoxInner)
            {
                Position = boxDrawPosition,
                Scale = boxScale,
                Tint = attackBorderColor * opacity
            }, blendState, drawToDepthBuffer: false);

            sprites.Enqueue(critical, new SpriteData(critical)
            {
                Position = criticalPoint - Vector3.UnitZ * 20f,
                Scale = Vector2.One * Scale * new Vector2(1f, 1.6f),
                Tint = Color.White * opacity
            }, blendState, drawToDepthBuffer: false);

            sprites.Enqueue(pressIndicator, new SpriteData(pressIndicator)
            {
                Position = pressIndicatorDrawPosition,
                Scale = Vector2.One * pressIndicatorScale,
                Tint = Color.White * opacity
            }, blendState, drawToDepthBuffer: false);

            sprites.Enqueue(icon, new SpriteData(icon)
            {
                Position = iconDrawPosition,
                Scale = Vector2.One * Scale,
                Tint = Color.White * opacity
            }, blendState, drawToDepthBuffer: false);
        }
    }

    private float fadeoutTimer;

    /// <summary>
    ///     How far along this interface is in fading out
    ///     after all attack attempts are performed.
    /// </summary>
    public float FadeoutProgress => float.InverseLerp(0f, 1.75f, fadeoutTimer);

    /// <summary>
    ///     How long a standard strike wait lasts for, in
    ///     seconds.
    /// </summary>
    public static float StandardStrikeDelay => 0.6f;

    /// <summary>
    ///     The scale of this UI.
    /// </summary>
    public static float Scale => 0.75f;

    public AttackMinigameInterface(BattleMenu menu, Hero[] allHeroes, Hero[] attackingHeroes) : base(menu)
    {
        AllHeroes = allHeroes;
        IncomingStrikes = new StrikeState[attackingHeroes.Length];
        for (int i = 0; i < attackingHeroes.Length; i++)
        {
            float criticalDelay;
            do
            {
                criticalDelay = StandardStrikeDelay * (1.5f + menu.Scene.Rng.NextFloat(attackingHeroes.Length * 0.567f));
            }
            while (IncomingStrikes.Any(s => s is not null && float.Abs(s.RemainingTimeUntilCritical - criticalDelay) <= 0.16f));

            IncomingStrikes[i] = new StrikeState(criticalDelay, menu.Scene)
            {
                Hero = attackingHeroes[i]
            };
        }
    }

    public override void Update()
    {
        var remainingStrikesByTimeLeft = IncomingStrikes.Where(s => !s.StrikeAttemptCompleted).OrderBy(s => s.RemainingTimeUntilCritical).ToList();

        // If the player pressed the hit button, find
        // the strike closest to nearing its completion and
        // record its strike.
        if (remainingStrikesByTimeLeft.Count >= 1)
        {
            StrikeState strike = remainingStrikesByTimeLeft[0];
            float normalizedStrikeTime = strike.RemainingTimeUntilCritical / StandardStrikeDelay;
            bool wayTooEarly = normalizedStrikeTime >= 1f;
            bool tooLate = normalizedStrikeTime <= -0.1f;
            float criticalStrikeTimeWindow = StatsRepository.CriticalStrikeTimeWindowSeconds;

            if (ActiveKeymap.Confirm.WasJustPressed || tooLate)
            {
                strike.StrikeAttemptCompleted = true;
                strike.SuccessfulCriticalStrike = strike.RemainingTimeUntilCritical >= -criticalStrikeTimeWindow * 0.5f &&
                                                  strike.RemainingTimeUntilCritical <= criticalStrikeTimeWindow * 0.5f;
                strike.Missed = wayTooEarly || tooLate;
                strike.Hero.ActiveAnimation = strike.Hero.StandardAttackAnimation;
                strike.ClearAfterimages();

                Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("BaseSwing.wav")));
                if (strike.SuccessfulCriticalStrike)
                {
                    Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("CriticalSwing.wav")));

                    Random rng = Menu.Scene.Rng;
                    for (int i = 0; i < 3; i++)
                    {
                        Menu.Scene.CriticalStrikeParticles.Add(new CriticalParticle()
                        {
                            Position = strike.Hero.Position + new Vector3(rng.NextFloat(120f), rng.NextFloat(-15f, 15f), 0.5f),
                            Velocity = Vector3.UnitX * rng.NextFloat(120f, 360f)
                        });
                    }
                }

                float tpGain = strike.CalculateAccuracyScore() * 100f / 26f;
                if (strike.Missed)
                    tpGain = 0f;

                strike.TensionPointsToAward = tpGain;
            }
        }

        foreach (StrikeState strike in IncomingStrikes)
            strike.Update(Menu);

        if (remainingStrikesByTimeLeft.Count <= 0)
            fadeoutTimer += Menu.Scene.Time.DeltaTime;
    }

    public override void PrepareRender(SpriteRenderer sprites, TextRenderer text, Vector3 center, Vector2 size)
    {
        size.Y *= 0.75f;
        if (AllHeroes.Length == 2)
            size.Y *= 0.5f;

        float opacity = float.InverseLerp(1f, 0.75f, FadeoutProgress);
        Vector3 left = center - Vector3.UnitX * size.X * 0.5f;

        for (int i = AllHeroes.Length - 1; i >= 0; i--)
        {
            Hero hero = AllHeroes[i];
            if (IncomingStrikes.FirstOrDefault(s => s.Hero == hero) is not { } strike)
                continue;

            float dx = 32f;
            float dy = float.Lerp(-0.4f, 0.4f, i / (float)(AllHeroes.Length - 1f)) * size.Y * Scale;
            float dy2 = float.Lerp(-0.4f, 0.4f, (i + 1f) / (float)(AllHeroes.Length - 1f)) * size.Y * Scale;
            float boxHeight = float.Abs(dy2 - dy) + 2f;

            Vector3 localLeft = left + new Vector3(dx, dy - 6f, 0f);
            if (opacity < 0.99f)
                strike.PrepareRender(sprites, BlendState.Additive, localLeft, opacity, boxHeight, i, AllHeroes.Length);
            if (opacity > 0.9f)
                strike.PrepareRender(sprites, BlendState.AlphaBlend, localLeft, opacity, boxHeight, i, AllHeroes.Length);
        }
    }
}
