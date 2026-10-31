using System.Numerics;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;
using static Northernlight.Content.Knight.AttackPatterns.HowlingWinds;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class SwordTunnelAlt : Attack<RoaringKnight>
{
    public readonly record struct Stats(int SwordStandardDamage, int SwordDashDamage, float GrazePoints, float SwordGapSize, float AttackDuration, float TotalBobsToPerform, float SwordDashAnimationDuration, bool StrongerWind, Vector2 BoxSize);

    private int swordMoveDirection = -1;

    private float windIntensity;

    private float windTimer;

    private bool startedDashAnimations;

    private bool playedDashSound;

    private bool createdWindSound;

    private bool teleportedBack;

    private float reformTimer;

    private Sound windSound;

    private readonly Stats stats;

    private readonly OwnedTimer spawnTimer;

    private readonly OwnedTimer afterimageTimer;

    private readonly OwnedTimer vanishSoundTimer;

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextSwordTunnelAlt;

    public SwordTunnelAlt(BattleHandler battle, Stats stats) : base(battle)
    {
        spawnTimer = Scene.Time.Rent();
        afterimageTimer = Scene.Time.Rent();
        vanishSoundTimer = Scene.Time.Rent();
        this.stats = stats;

        Scene.DeathScreen.OnDeath += () => windSound?.Stop();
    }

    protected override void UpdateInternal()
    {
        Scene.ScreenDarkening = float.InverseLerp(0f, 1.5f, AttackTimer) * 0.8f;

        float afterimagesDuration = 1.2f;
        if (AttackTimer < afterimagesDuration)
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightPointAnimation, 5, loops: false, animatesInReverse: false);
            Attacker.EmitsAfterimages = false;
            EmitDisappearanceAfterimages(afterimagesDuration);

            if (vanishSoundTimer.Cycle(0.1f) && AttackTimer < afterimagesDuration * 0.8f)
                Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightVanish.wav"), 0.4f));
        }

        if (AttackTimer >= afterimagesDuration * 0.7f)
        {
            if (startedDashAnimations)
            {
                Attacker.Opacity = 1f;
                Attacker.Position = Attacker.SpawnPosition;
                if (!teleportedBack)
                {
                    Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightTeleport.wav"), 0.5f));
                    teleportedBack = true;
                }
            }
            else
                Attacker.Position = Attacker.SpawnPosition + Vector3.UnitY * 100000f;
            Attacker.Velocity = Vector3.Zero;
        }

        if (!startedDashAnimations)
        {
            if (!createdWindSound)
            {
                windSound = new Sound(AudioBuffer.FromWav("BlizzardWindLoop.wav"))
                {
                    Loop = true
                };
                Scene.Audio.Play(windSound);
                createdWindSound = true;
            }

            Vector2 origin = Battle.Box.Position;
            origin.X += NorthernlightGame.KnightFightResolution.X * swordMoveDirection * -0.6f;

            float attackProgress = float.InverseLerp(0f, stats.AttackDuration, AttackTimer);
            origin.Y += float.Sin(float.Tau * attackProgress * stats.TotalBobsToPerform) * stats.SwordGapSize * 1.2f;

            if (spawnTimer.Cycle(0.11f))
                SummonSwordWaveInstance(origin);

            MoveSwords();
            if (AttackTimer >= stats.AttackDuration)
            {
                startedDashAnimations = true;
                foreach (Bullet b in Bullets)
                {
                    if (b is BlackSword sword)
                        sword.StartDashAnimation();
                }
            }
            else
                windIntensity = float.InverseLerp(0f, 0.12f, attackProgress);
        }
        else
        {
            windIntensity = float.LerpDt(windIntensity, 0f, 7.5f, Scene.Time);

            if (reformTimer >= 0.1f)
                Attacker.RenderState = Attacker.StandardRenderState;
            else
            {
                Attacker.EmitsAfterimages = false;
                Attacker.SetTexture(Atlases.KnightScene.KnightBallFormTransition, 4, loops: false);
                if (Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
                    reformTimer += Scene.Time.DeltaTime;
            }
        }

        float windSpeed = float.Lerp(3f, 1f, float.Pow(windIntensity, 0.25f));
        windTimer += Scene.Time.DeltaTime * windSpeed;

        if (AttackTimer >= stats.AttackDuration + stats.SwordDashAnimationDuration && !playedDashSound)
        {
            playedDashSound = true;
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SwordDash.wav")));
        }

        Battle.Box.Rotation = windIntensity * swordMoveDirection * Scene.Rng.NextFloat(0.9f, 1.1f) * 0.06f;
        Battle.Box.Position += (Scene.Rng.NextCircular(windIntensity * 100f) + Vector2.UnitX * swordMoveDirection * windIntensity * 15f) * Scene.Time.DeltaTime;

        windSound.Stream.Volume = windIntensity * 0.56f;
        windSound.Stream.FrequencyRatio = 1f + windIntensity * 2f;

        MoveHeroes();

        if (startedDashAnimations && Bullets.Count <= 0)
        {
            windSound.Stop();
            End();
        }
    }

    private void MoveHeroes()
    {
        foreach (Hero hero in Scene.Heroes.All)
        {
            float windForce = Scene.Rng.NextFloat(6f, 7.2f) * windIntensity;
            if (hero.Defending)
                windForce *= 0.45f;
            if (hero.HP <= 0)
                windForce = 0f;

            float x = hero.StartingPosition.X * 0.1f + AttackTimer * 12.5f;
            float wave = float.Abs(float.Cos(x * 1.777f) + float.Cos(x * 1.097f)) * 0.5f;

            hero.Position = hero.StartingPosition - Vector3.UnitX * wave * windForce;
        }
    }

    private void EmitDisappearanceAfterimages(float afterimagesDuration)
    {
        int afterimageCountPerSecond = 20;
        float afterimageProgress = float.InverseLerp(0f, afterimagesDuration, AttackTimer);
        float afterimageLifetime = 2f;
        float afterimageTravelDistance = float.Lerp(132f, 550f, float.Sqrt(afterimageProgress));

        Attacker.Opacity = (1f - afterimageProgress).Cubed();
        Attacker.RepositionDelay = 0.1f;
        Attacker.Velocity = Vector3.Zero;

        if (afterimageTimer.Cycle(afterimageLifetime / afterimageCountPerSecond))
        {
            Attacker.Afterimages.Add(new KnightAfterimage()
            {
                RenderState = Attacker.RenderState,
                Lifetime = afterimageLifetime,
                Position = Attacker.Position + Vector3.UnitZ * (Attacker.Afterimages.Count * 2f + 10f),
                HorizontallyFlip = Attacker.HorizontallyFlip,
                Scale = Vector2.One * Attacker.Scale,
                Velocity = Vector3.UnitX * (Attacker.HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / afterimageLifetime * (1f - afterimageProgress)
            });

            Attacker.Afterimages.Add(new KnightAfterimage()
            {
                RenderState = Attacker.RenderState,
                Lifetime = afterimageLifetime,
                Position = Attacker.Position + Vector3.UnitZ * (Attacker.Afterimages.Count * 2f + 10f),
                HorizontallyFlip = Attacker.HorizontallyFlip,
                Scale = Vector2.One * Attacker.Scale,
                Velocity = Vector3.UnitX * (Attacker.HorizontallyFlip ? -1f : 1f) * afterimageTravelDistance / afterimageLifetime * -afterimageProgress * 0.4f
            });
        }
    }

    private void MoveSwords()
    {
        float swordFlySpeed = float.Smoothstep(0.3f, 1f, windIntensity) * 900f;
        foreach (Bullet b in Bullets)
        {
            if (b is BlackSword sword)
            {
                float acceleration = 1f + sword.ExistenceTimer * 0.9f;
                sword.PreviousPosition = sword.Position;
                sword.Position += Vector2.UnitX * swordMoveDirection * swordFlySpeed * acceleration * Scene.Time.DeltaTime * Scene.Rng.NextFloat(0.9f, 1f);
            }
        }
    }

    private void SummonSwordWaveInstance(Vector2 origin)
    {
        float swordHeight = Atlases.KnightScene.BlackSword.Area.Height * BlackSword.Scale;
        Vector2 topOffset = -Vector2.UnitY * (stats.SwordGapSize + swordHeight);
        Bullets.Add(new BlackSword(Scene, this, stats.SwordStandardDamage, stats.SwordDashDamage, stats.GrazePoints, stats.SwordDashAnimationDuration, true, origin + topOffset, Vector2.Zero, Angle.PiOver2));
        Bullets.Add(new BlackSword(Scene, this, stats.SwordStandardDamage, stats.SwordDashDamage, stats.GrazePoints, stats.SwordDashAnimationDuration, false, origin - topOffset, Vector2.Zero, Angle.PiOver2));
    }

    public override void ExtraPrepareRender(SpriteRenderer sprites)
    {
        RenderBlizzard(sprites);
        RenderWind(sprites);
    }

    private void RenderBlizzard(SpriteRenderer sprites)
    {
        Texture texture = Atlases.LargeTextures.Atlas.Value.BackingTexture;

        float baseZ = BattleBox.BaseZ;
        SpriteData topBlizzardData = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, baseZ + 20f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 2f,
            Tint = Color.White * windIntensity * (stats.StrongerWind ? 1f : 0.65f) * 0.06f,
            Rotation = Vector2.UnitX * swordMoveDirection
        };
        sprites.Enqueue(texture, topBlizzardData, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.BlizzardShader, PrepareBlizzardParameters));

        SpriteData bottomBlizzardData = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, baseZ - 20f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 2f,
            Tint = Color.White * windIntensity * (stats.StrongerWind ? 1f : 0.65f) * 0.4f,
            Rotation = Vector2.UnitX * swordMoveDirection
        };
        sprites.Enqueue(texture, bottomBlizzardData, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.BlizzardShader, PrepareBlizzardParameters));
    }

    private void RenderWind(SpriteRenderer sprites)
    {
        Texture texture = Atlases.LargeTextures.CloudyNoise.BackingTexture;
        SpriteData windData = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, BattleBox.BaseZ + 30f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 2f,
            Tint = Color.White * float.Pow(windIntensity, 0.75f) * (stats.StrongerWind ? 1f : 0.65f),
            Rotation = Vector2.UnitX * swordMoveDirection
        };

        sprites.Enqueue(texture, windData, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.WindLinesShader, PrepareWindParameters));
    }

    private void PrepareWindParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new FallingStars.Uniforms()
        {
            Time = new Vector4(windTimer * -1.8f, 0f, 0f, 0f),
            OverlayColor = Vector4.Zero,
            WindTextureBounds = Atlases.LargeTextures.WindLines.VectorBounds,
        });
    }

    private void PrepareBlizzardParameters(FrameContext frame, RenderPass pass)
    {
        float zoom = 0.8f;
        Vector2 windZoom = new Vector2(1.5f, 6f) * zoom;
        Vector2 snowZoom = new Vector2(0.25f, 1f) * zoom;

        frame.CommandBuffer.BindFragmentUniformData(0, new BlizzardUniforms()
        {
            Time = new Vector4(-windTimer, 0f, 0f, 0f),
            NoiseTextureBounds = Atlases.LargeTextures.SemiRandom_OmitPadding.VectorBounds,
            PerlinTextureBounds = Atlases.LargeTextures.CloudyNoise.VectorBounds,
            Zoom = new Vector4(windZoom.X, windZoom.Y, snowZoom.X, snowZoom.Y)
        });
        pass.BindFragmentSamplerTextures(1, new RenderPass.SamplerTexture(Scene.Device.LinearWrap, Atlases.LargeTextures.Atlas.Value.BackingTexture));
    }
}
