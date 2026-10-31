using System.Diagnostics;
using System.Numerics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Content.Knight.Dialogue;
using Northernlight.Content.Knight.Particles;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents an instance of a playable
///     hero in the battle.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class Hero : ISpeakingCharacter
{
    private int previousHealQuantity;

    private bool previousHealWasToMax;

    private float currentFlashGlow;

    private float hurtAnimationCountdown;

    private HeroAnimation activeAnimation;

    private OwnedTimer healTimer;

    private readonly KnightFightScene scene;

    private readonly List<HeroAfterimage> afterimages = [];

    private static readonly Font font = new Font("BattleDialogue.ttf", 64f);

    /// <summary>
    ///     The amount of HP this hero
    ///     currently has.
    /// </summary>
    /// <remarks>
    ///     This value may go below zero, indicating
    ///     a fainting status.
    /// </remarks>
    public int HP
    {
        get;
        set;
    }

    /// <summary>
    ///     The maximum amount of HP this hero
    ///     can have.
    /// </summary>
    public int MaxHP
    {
        get;
        init;
    }

    // I... genuinely have no idea why this discrepancy exists?
    /// <summary>
    ///     Whether this hero should use the alt
    ///     spare icon when performing that action
    ///     in the battle menu.
    /// </summary>
    public bool UsesAltSpareIcon
    {
        get;
        set;
    }

    /// <summary>
    ///     The intensity of the white glow
    ///     overlay over this hero as a
    ///     result of being healed.
    /// </summary>
    public float HealGlowIntensity
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this hero can act in place
    ///     of using magic.
    /// </summary>
    public bool UsesActCommand
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this hero is attacking currently or not.
    /// </summary>
    public bool Attacking
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this hero should flash due to
    ///     being selected for something by the
    ///     player, such as a heal effect.
    /// </summary>
    public bool SelectionFlash
    {
        get;
        set;
    }

    /// <summary>
    ///     A general counting down flash intensity value.
    /// </summary>
    public float CountdownFlash
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this hero is defending currently or not.
    /// </summary>
    public bool Defending
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of damage basic attacks by this hero
    ///     do as a baseline.
    /// </summary>
    public required int BaseAttackDamage
    {
        get;
        set;
    }

    /// <summary>
    ///     The hero's name.
    /// </summary>
    public required LanguageAgnosticText Name
    {
        get;
        init;
    }

    /// <summary>
    ///     The outline color to use when this
    ///     hero's action is being selected
    ///     in the battle menu.
    /// </summary>
    public required Color BattleMenuBorderColor
    {
        get;
        init;
    }

    /// <summary>
    ///     The outline color to use when this
    ///     hero is performing an attack.
    /// </summary>
    public required Color AttackBorderColor
    {
        get;
        init;
    }

    /// <summary>
    ///     The color to use for button icons.
    /// </summary>
    public required Color ButtonIconColor
    {
        get;
        init;
    }

    /// <summary>
    ///     The color to use for damage indidcator text.
    /// </summary>
    public required Color DamageColor
    {
        get;
        init;
    }

    /// <summary>
    ///     The bar edge color to use when this
    ///     hero is performing an attack.
    /// </summary>
    public required Color AttackBarEdgeColor
    {
        get;
        init;
    }

    /// <summary>
    ///     The sound to play when an attack is made
    ///     by this hero.
    /// </summary>
    public required string AttackHitSound
    {
        get;
        init;
    }

    /// <summary>
    ///     This hero's current position.
    /// </summary>
    public Vector3 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     This hero's starting position.
    /// </summary>
    public Vector3 StartingPosition
    {
        get;
        set;
    }

    /// <summary>
    ///     The act/magic options that can be selected for
    ///     use by this hero.
    /// </summary>
    public required Func<OptionSelectionInterface, QueuedHeroAction, OptionSelectionInterface.Option[]> ActMagicOptionsGenerator
    {
        get;
        set;
    }

    /// <summary>
    ///     This hero's active animation.
    /// </summary>
    public HeroAnimation ActiveAnimation
    {
        get
        {
            if (HP <= 0)
                return DownedAnimation;
            if (hurtAnimationCountdown > 0f)
                return HurtAnimation;

            return activeAnimation;
        }

        set
        {
            if (activeAnimation != value)
            {
                activeAnimation = value;
                activeAnimation.ResetFrame();
            }
        }
    }

    /// <summary>
    ///     The in-use battle menu icon.
    /// </summary>
    public AtlasEntry ActiveBattleMenuIcon
    {
        get
        {
            if (HP <= 0)
                return BattleMenuIconDowned;
            if (hurtAnimationCountdown > 0f)
                return BattleMenuIconHurt;

            return BattleMenuIcon;
        }
    }

    /// <summary>
    ///     This hero's battle menu icon sprite.
    /// </summary>
    public required AtlasEntry BattleMenuIcon
    {
        get;
        init;
    }

    /// <summary>
    ///     This hero's battle menu icon sprite when hurt.
    /// </summary>
    public required AtlasEntry BattleMenuIconHurt
    {
        get;
        init;
    }

    /// <summary>
    ///     This hero's battle menu icon sprite when downed.
    /// </summary>
    public required AtlasEntry BattleMenuIconDowned
    {
        get;
        init;
    }

    /// <summary>
    ///     This hero's battle menu name sprite.
    /// </summary>
    public required AtlasEntry BattleMenuNameSprite
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's stand-still animation.
    /// </summary>
    public required HeroAnimation StandStillAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's idle animation.
    /// </summary>
    public required HeroAnimation IdleAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's standard attack animation.
    /// </summary>
    public required HeroAnimation StandardAttackAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's standard attack anticipation animation.
    /// </summary>
    public required HeroAnimation StandardAttackAnticipationAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's defend animation.
    /// </summary>
    public required HeroAnimation DefendAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's hurt animation.
    /// </summary>
    public required HeroAnimation HurtAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's downed animation.
    /// </summary>
    public required HeroAnimation DownedAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's item preparation animation.
    /// </summary>
    public required HeroAnimation ItemPreparationAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The hero's item use animation.
    /// </summary>
    public required HeroAnimation ItemUseAnimation
    {
        get;
        init;
    }

    /// <summary>
    ///     The texture to use for the hit impact created
    ///     by this hero.
    /// </summary>
    public required AtlasEntry HitImpactTexture
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of frames in a hit impact created
    ///     by this hero.
    /// </summary>
    public required int TotalHitImpactFrames
    {
        get;
        init;
    }

    public required string? TextSoundFileName
    {
        get;
        init;
    }

    public Vector2 SpeakerPosition => Position.AsVector2() - Vector2.UnitY * 90f;

    public Hero(KnightFightScene scene, int maxHp)
    {
        this.scene = scene;
        MaxHP = maxHp;
        HP = maxHp;
    }

    /// <summary>
    ///     Applies various incoming-damage modifications
    ///     as necessary for this hero.
    /// </summary>
    public int ModifyIncomingDamage(int damage)
    {
        if (Defending)
            damage = (int)float.Ceiling(damage * 0.666667f);

        return damage;
    }

    /// <summary>
    ///     Applies start-of-enemy-turn effects to this hero.
    /// </summary>
    public void ApplyStartOfEnemyTurnEffects()
    {
        Attacking = false;

        if (Defending)
        {
            if (ActiveAnimation != DefendAnimation)
                ActiveAnimation = DefendAnimation;
        }
        else
            ActiveAnimation = IdleAnimation;
    }

    /// <summary>
    ///     Applies end-of-turn effects to this hero.
    ///     This mostly performs cleanup work, but also
    ///     applies the 1/8th natural heal-back and other
    ///     such effects.
    /// </summary>
    public void ApplyEndOfTurnEffects()
    {
        ActiveAnimation = IdleAnimation;
        Defending = false;
        Attacking = false;
        if (HP <= 0)
        {
            bool swooned = HP <= -500;
            if (!swooned)
            {
                int healBack = (int)float.Ceiling(MaxHP / 8f);
                Heal(healBack, playHealAnimation: false);
            }
        }
    }

    /// <summary>
    ///     Creates a new afterimage for this hero.
    /// </summary>
    public void CreateAfterimage(Vector3 velocity, float lifetime, float maxOpacity)
    {
        afterimages.Add(new HeroAfterimage(activeAnimation.Clone(), Position - Vector3.UnitZ * 4f, velocity, lifetime, maxOpacity));
    }

    /// <summary>
    ///     Hurts this hero for a given amount of damage.
    /// </summary>
    public void Hurt(int damage, bool swoon)
    {
        if (damage <= 0)
            return;

        if (HP <= 0)
            return;

        if (scene.BattleBackBackdropOpacity >= 0.999f && scene.BattleHandler.Attack is not FinalAttack && scene.BattleHandler.Attack is not FinalAttackSideB)
            return;

        damage = ModifyIncomingDamage(damage);
        HP -= damage;

        bool successfulSwoon = false;
        scene.BattleHandler.Soul.ImmunityFramesCountdown = Soul.ImmunityFramesDuration;
        if (HP <= 0)
        {
            if (swoon)
            {
                HP = -999;
                successfulSwoon = true;
            }
            else
                HP = -MaxHP / 2;
        }

        scene.Audio.Play(new Sound(AudioBuffer.FromWav("HeroHurt.wav")));
        hurtAnimationCountdown = HurtAnimation.Duration;

        Random rng = scene.Rng;
        Vector3 indicatorVelocity = new Vector3(rng.NextFloat(200f, 250f), rng.NextFloat(-200f, -150f), 0f);
        scene.DamageIndicators.Add(new DamageIndicator()
        {
            Value = damage,
            Miss = false,
            Swoon = successfulSwoon,
            Position = Position + new Vector3(0f, rng.NextFloat(-20f, 0f) - 30f, 120f),
            Velocity = indicatorVelocity,
            Scale = Vector2.One,
            Color = (successfulSwoon ? new Color(255, 0, 0) : Color.White) * (1f - scene.BattleBackBackdropOpacity)
        });
    }

    /// <summary>
    ///     Heals this hero.
    /// </summary>
    public void Heal(int healQuantity, bool playHealAnimation = true, bool playHealSound = true, bool showMaxTextIfNeeded = true)
    {
        bool wasDowned = HP <= 0;
        HP += healQuantity;
        bool wasRevived = wasDowned && HP >= 1;

        // According to the wiki, if a hero manages to to get a
        // natural heal-back that pushes them out of downed
        // range, their HP is restored to 1/6th of its maximum.
        // For a full revive, refer to the Revive method.
        if (wasRevived)
            HP = (int)float.Round(MaxHP / 6f);

        if (HP > MaxHP)
            HP = MaxHP;

        if (playHealAnimation)
        {
            if (healTimer is null)
                healTimer = scene.Time.Rent();
            else
                healTimer.Reset();

            if (playHealSound)
                scene.Audio.Play(new Sound(AudioBuffer.FromWav("Heal.wav")));
            previousHealQuantity = healQuantity;
            HealGlowIntensity = 1f;

            if (healQuantity >= MaxHP && HP >= MaxHP && showMaxTextIfNeeded)
                previousHealWasToMax = true;
        }
    }

    /// <summary>
    ///     Revives this hero.
    /// </summary>
    /// <remarks>
    ///     This does nothing if the hero is not downed.
    /// </remarks>
    public void Revive(int healQuantity, bool showMaxTextIfNeeded = true)
    {
        if (HP >= 1)
            return;

        HP = int.Clamp(healQuantity, 1, MaxHP);

        if (healTimer is null)
            healTimer = scene.Time.Rent();
        else
            healTimer.Reset();

        scene.Audio.Play(new Sound(AudioBuffer.FromWav("Heal.wav")));
        previousHealQuantity = healQuantity;
        HealGlowIntensity = 1f;

        if (healQuantity >= MaxHP && HP >= MaxHP && showMaxTextIfNeeded)
            previousHealWasToMax = true;
    }

    /// <summary>
    ///     Updates this hero.
    /// </summary>
    public void Update()
    {
        ActiveAnimation ??= IdleAnimation;
        ActiveAnimation.Update();

        if (StartingPosition == Vector3.Zero)
            StartingPosition = Position;

        hurtAnimationCountdown = float.Max(0f, hurtAnimationCountdown - scene.Time.DeltaTime);

        float previousHealGlow = HealGlowIntensity;
        HealGlowIntensity = float.Max(0f, HealGlowIntensity - scene.Time.DeltaTime * 2f);
        if (previousHealQuantity >= 1 && previousHealGlow > 0.5f && HealGlowIntensity <= 0.5f)
        {
            Random rng = scene.Rng;
            Vector3 indicatorVelocity = new Vector3(rng.NextFloat(200f, 250f), rng.NextFloat(-200f, -150f), 0f);
            scene.DamageIndicators.Add(new DamageIndicator()
            {
                Value = previousHealQuantity,
                Position = Position + new Vector3(0f, rng.NextFloat(-20f, 0f) - 30f, 250f),
                Velocity = indicatorVelocity,
                Scale = Vector2.One,
                Color = new Color(0, 255, 0),
                Max = previousHealWasToMax
            });

            previousHealWasToMax = false;
        }

        if (HealGlowIntensity >= 0.4f)
        {
            while (healTimer.CycleWhile(0.02f))
            {
                scene.HealParticles.Add(new HealParticle()
                {
                    Position = Position + new Vector3(scene.Rng.NextCircular(40f) - Vector2.UnitY * 75f, 20f),
                    Velocity = new Vector3(120f - scene.Rng.NextFloat(120f), -180f - scene.Rng.NextFloat(120f), 0f)
                });
            }
        }

        CountdownFlash = float.Max(0f, CountdownFlash - scene.Time.DeltaTime * 1.5f);

        foreach (HeroAfterimage afterimage in afterimages)
            afterimage.Update(scene);
        afterimages.RemoveAll(a => a.Dead);
    }

    /// <summary>
    ///     Prepares this hero for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        ActiveAnimation ??= IdleAnimation;

        Vector3 drawPosition = Position;

        float hurtAnimationProgress = float.InverseLerp(HurtAnimation.Duration, 0f, hurtAnimationCountdown);
        drawPosition.X += float.Sin(float.Tau * hurtAnimationProgress * 2f) * (Defending ? 8f : 12f) * (1f - hurtAnimationProgress);

        Color tint = new Color(0.5f, 0.46f, 0.63f);
        tint = Color.Lerp(tint, Color.Black, scene.BattleBackBackdropOpacity);

        float selectionFlashIntensity = 0f;
        if (SelectionFlash)
            selectionFlashIntensity = float.Cos01(scene.Time.ElapsedTime * 5f);

        currentFlashGlow = float.Max(HealGlowIntensity, selectionFlashIntensity);
        currentFlashGlow = float.Max(currentFlashGlow, CountdownFlash);
        ActiveAnimation.Render(sprites, drawPosition, tint, currentFlashGlow);

        foreach (HeroAfterimage afterimage in afterimages)
            afterimage.PrepareRender(sprites);
    }

    public void PrepareRenderTextAndBox(SpriteRenderer sprites, TextRenderer text, Vector2 start, int direction, string spokenText, string fullText)
    {
        int totalNewLines = 0;
        float generalScale = 1f;
        float z = 400f;
        float textScale = generalScale * 0.4f;
        float maxTextWidth = 300f;
        Vector2 boxSize = Vector2.Zero;
        Vector3 textStart = new Vector3(start + new Vector2(32f, -10f) * generalScale, z + 5f);
        Vector3 textPosition = textStart;

        foreach (char c in fullText)
        {
            float characterWidth = text.MeasureString(c.ToString(), font).X;
            if (c == CharacterDialogueHandler.PauseIndicator)
                continue;

            characterWidth *= textScale;
            textPosition.X += characterWidth;

            bool canEmitNewline = c == ' ';
            if (textPosition.X >= textStart.X + maxTextWidth && canEmitNewline)
            {
                textPosition.X = textStart.X;
                textPosition.Y += font.Size * textScale * 1.5f;
                totalNewLines++;
            }

            boxSize = Vector2.Max(boxSize, (textPosition - textStart).AsVector2() * new Vector2(1f, 0.75f));
        }

        textPosition = textStart;

        if (totalNewLines == 0)
            textPosition.Y += textScale * 20f;

        foreach (char c in spokenText)
        {
            if (c == CharacterDialogueHandler.PauseIndicator)
                continue;

            text.Enqueue(c.ToString(), font, textPosition, Color.Black, Vector2.One * textScale, new Vector2(0f, 0.5f));

            float characterWidth = text.MeasureString(c.ToString(), font).X;

            characterWidth *= textScale;
            textPosition.X += characterWidth;

            bool canEmitNewline = c == ' ';
            if (textPosition.X >= textStart.X + maxTextWidth && canEmitNewline)
            {
                textPosition.X = textStart.X;
                textPosition.Y += font.Size * textScale * 1.5f;
            }
        }

        boxSize.X += 6f;
        boxSize.Y += totalNewLines == 0 ? 40f : 56f;
        textStart.Y -= boxSize.Y * textScale * 0.5f;

        float arrowSquish = float.Min(1f, boxSize.Y / 100f);
        AtlasEntry arrow = Atlases.KnightScene.DialogueBoxArrow;
        sprites.Enqueue(arrow, new SpriteData(arrow)
        {
            Position = new Vector3(start, z),
            Scale = new Vector2(1f, arrowSquish) * generalScale,
            Tint = Color.White,
        });

        start.X += (arrow.Area.Width - 4f) * generalScale * 0.5f;

        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        AtlasEntry topLeft = Atlases.KnightScene.DialogueBoxTopLeft;
        AtlasEntry bottomLeft = Atlases.KnightScene.DialogueBoxBottomLeft;
        AtlasEntry topRight = Atlases.KnightScene.DialogueBoxTopRight;
        AtlasEntry bottomRight = Atlases.KnightScene.DialogueBoxBottomRight;
        sprites.Enqueue(topLeft, new SpriteData(topLeft)
        {
            Position = new Vector3(start + new Vector2(topLeft.Area.Width * 0.5f, boxSize.Y * -0.5f) * generalScale, z),
            Scale = Vector2.One * generalScale,
            Tint = Color.White,
        });
        sprites.Enqueue(bottomLeft, new SpriteData(bottomLeft)
        {
            Position = new Vector3(start + new Vector2(bottomLeft.Area.Width * 0.5f, boxSize.Y * 0.5f) * generalScale, z),
            Scale = Vector2.One * generalScale,
            Tint = Color.White,
        });
        sprites.Enqueue(topRight, new SpriteData(topRight)
        {
            Position = new Vector3(start + new Vector2(topRight.Area.Width * 0.5f + boxSize.X, boxSize.Y * -0.5f) * generalScale, z),
            Scale = Vector2.One * generalScale,
            Tint = Color.White,
        });
        sprites.Enqueue(bottomRight, new SpriteData(bottomRight)
        {
            Position = new Vector3(start + new Vector2(bottomRight.Area.Width * 0.5f + boxSize.X, boxSize.Y * 0.5f) * generalScale, z),
            Scale = Vector2.One * generalScale,
            Tint = Color.White,
        });

        Vector2 center = start + new Vector2(bottomRight.Area.Width * 0.5f + boxSize.X * 0.5f, 0f) * generalScale;
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(center, z - 2f),
            Scale = Vector2.One * (boxSize + Vector2.UnitX * bottomRight.Area.Width) * generalScale,
            Tint = Color.White,
        });
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(center, z - 2f),
            Scale = Vector2.One * (boxSize + Vector2.UnitY * bottomRight.Area.Height) * generalScale,
            Tint = Color.White,
        });
    }

    public override string ToString()
    {
        return $"{Name.Value} ({HP}/{MaxHP})";
    }
}
