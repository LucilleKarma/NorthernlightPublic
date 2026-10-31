using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.Knight.Bullets;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class HowlingWinds : Attack<RoaringKnight>
{
    private bool createdWindSound;

    private float windIntensity;

    private float windTimer;

    private float boxFreezeProgress;

    private Sound windSound;

    private readonly Stats stats;

    private readonly OwnedTimer iceShardSpawnTimer;

    [StructLayout(LayoutKind.Sequential)]
    public struct BlizzardUniforms
    {
        public Vector4 Time;

        public Vector4 PerlinTextureBounds;

        public Vector4 NoiseTextureBounds;

        public Vector4 Zoom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BoxFreezeUniforms
    {
        public Vector4 MaxFreezeDistanceAndFreezeProgress;

        public Vector4 FreezeOriginDirection;

        public Vector4 NoiseUvBounds;

        public Vector4 FrostBounds;
    }

    public readonly record struct Stats(Vector2 BoxSize, Vector2 WindDirection, float AttackDuration, float ShardSpawnRate, int IceShardDamage,
        float IceShardGrazePoints, float IceShardBaseSpeedFactor, float SoulPushSpeed, int BattleBoxFreezeTouchDamage, float BattleBoxFreezeTouchReboundSpeed);

    /// <summary>
    ///     The direction of the wind.
    /// </summary>
    private Vector2 WindDirection => stats.WindDirection;

    /// <summary>
    ///     The forward-facing direction of the freeze
    ///     visual at the back of the battle box.
    /// </summary>
    private Vector2 FreezeDirection
    {
        get
        {
            float bestScore = float.NegativeInfinity;
            Vector2 result = Vector2.UnitX;
            foreach (Vector2 direction in boxDirectionsToConsider)
            {
                float score = Vector2.Dot(direction, WindDirection);
                if (score > bestScore)
                {
                    bestScore = score;
                    result = direction;
                }
            }

            return result;
        }
    }

    private static readonly Vector2[] boxDirectionsToConsider =
    [
        -Vector2.UnitX,
        Vector2.UnitX,
        -Vector2.UnitY,
        Vector2.UnitY
    ];

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextHowlingWinds;

    public HowlingWinds(BattleHandler battle, Stats stats) : base(battle)
    {
        this.stats = stats;
        iceShardSpawnTimer = Scene.Time.Rent();
        Scene.DeathScreen.OnDeath += () => windSound?.Stop();
    }

    protected override void UpdateInternal()
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

        bool done = AttackTimer >= stats.AttackDuration;
        float maxWindIntensity = float.Lerp(1f, 2f, (AttackTimer / stats.AttackDuration).Squared());
        windIntensity = float.Clamp(windIntensity + Scene.Time.DeltaTime * (done ? -1.9f : 1f) * 0.6f, 0f, maxWindIntensity);

        float windSpeed = float.Lerp(3f, 1f, float.Pow(windIntensity, 0.25f));
        windTimer += Scene.Time.DeltaTime * windSpeed;

        if (done)
        {
            boxFreezeProgress = float.Clamp(boxFreezeProgress - Scene.Time.DeltaTime * 0.8f, 0f, 1f);
            if (boxFreezeProgress <= 0f && windIntensity <= 0f)
            {
                windSound.Stop();
                End();
                return;
            }
        }
        else
        {
            boxFreezeProgress = float.Clamp(boxFreezeProgress + windIntensity.Cubed() * Scene.Time.DeltaTime * 0.25f, 0f, 1f);
            PerformBoxFreezeCheck();
        }
        MoveHeroes();

        Vector2 correctiveBoxForce = (BattleHandler.StartingBoxCenter - Battle.Box.Position).SafeNormalize(Vector2.Zero) * 95f;
        Vector2 boxMovementStep = (correctiveBoxForce + WindDirection * Scene.Rng.NextFloat(200f)) * windIntensity;
        Battle.Box.Position += boxMovementStep * Scene.Time.DeltaTime;
        Battle.Box.Rotation += float.Sign(WindDirection.X) * Scene.Time.DeltaTime * Scene.Rng.NextFloat(-0.8f, 1f) * 0.125f;

        Scene.RoaringKnight.SetTexture(Atlases.KnightScene.KnightPointAnimation, 5, loops: false, animatesInReverse: done);

        windSound.Stream.Volume = boxFreezeProgress * 0.5f;
        windSound.Stream.FrequencyRatio = 1f + boxFreezeProgress * 2f;

        Battle.Soul.Position += WindDirection.AsVector3() * windIntensity * Scene.Time.DeltaTime * stats.SoulPushSpeed;

        if (windIntensity >= 0.56f && iceShardSpawnTimer.Cycle(stats.ShardSpawnRate / windIntensity))
            SpawnShard();
    }

    private void PerformBoxFreezeCheck()
    {
        float distanceFromCenter = Vector2.Dot(FreezeDirection, Battle.Soul.Position.AsVector2() - Battle.Box.Position);
        float distanceFromEdge = Battle.Box.Size.X * 0.5f - distanceFromCenter;

        float hurtDistanceThreshold = float.Min(Battle.Box.Size.X, Battle.Box.Size.Y) * boxFreezeProgress * 0.3f;
        if (distanceFromEdge <= hurtDistanceThreshold && Battle.Soul.ImmunityFramesCountdown <= 0f)
        {
            Battle.Soul.Velocity = FreezeDirection.AsVector3() * -stats.BattleBoxFreezeTouchReboundSpeed;
            Battle.Hurt(stats.BattleBoxFreezeTouchDamage, false);
        }
    }

    private void SpawnShard()
    {
        int shardDamage = stats.IceShardDamage;
        float grazePoints = stats.IceShardGrazePoints;
        float scale = Scene.Rng.NextFloat(1.1f, 1.5f);
        Vector2 forward = WindDirection;
        Vector2 orthogonal = forward.RotatedBy(Angle.PiOver2);

        float sideOffset = Scene.Rng.NextFloat(-0.65f, 0.65f);
        Vector2 spawnPosition = (-forward * 0.6f + orthogonal * sideOffset) * NorthernlightGame.KnightFightResolution + Battle.Box.Position;
        Vector2 velocity = forward * Scene.Rng.NextFloat(280f, 750f) * stats.IceShardBaseSpeedFactor / scale * float.Pow(windIntensity, 0.4f);
        if (float.Abs(sideOffset) >= 0.35f)
            velocity *= Scene.Rng.NextFloat(1f, 3f);

        Bullets.Add(new IceShard(Scene, spawnPosition, velocity, shardDamage, grazePoints)
        {
            ScaleFactor = scale,
            StretchCoefficient = 1f / stats.IceShardBaseSpeedFactor
        });
    }

    private void MoveHeroes()
    {
        foreach (Hero hero in Scene.Heroes.All)
        {
            float windForce = Scene.Rng.NextFloat(5f, 6.5f) * windIntensity;
            if (hero.Defending)
                windForce *= 0.45f;
            if (hero.HP <= 0)
                windForce = 0f;

            float x = hero.StartingPosition.X * 0.1f + AttackTimer * 12.5f;
            float wave = float.Abs(float.Cos(x * 1.9574f) + float.Cos(x * 1.1023f)) * 0.5f;

            hero.Position = hero.StartingPosition - Vector3.UnitX * wave * windForce;
        }
    }

    public override void ExtraPrepareRender(SpriteRenderer sprites)
    {
        RenderBlizzard(sprites);
        RenderWind(sprites);
        RenderBoxFreeze(sprites);
    }

    private void RenderBlizzard(SpriteRenderer sprites)
    {
        Texture texture = Atlases.LargeTextures.Atlas.Value.BackingTexture;

        float baseZ = BattleBox.BaseZ;
        SpriteData topBlizzardData = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, baseZ + 20f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 2f,
            Tint = Color.White * windIntensity * 0.06f,
            Rotation = WindDirection
        };
        sprites.Enqueue(texture, topBlizzardData, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.BlizzardShader, PrepareBlizzardParameters));

        SpriteData bottomBlizzardData = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, baseZ - 20f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 2f,
            Tint = Color.White * windIntensity * 0.4f,
            Rotation = WindDirection
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
            Tint = Color.White * float.Pow(windIntensity, 0.75f),
            Rotation = WindDirection
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

    private void RenderBoxFreeze(SpriteRenderer sprites)
    {
        BattleBox box = Battle.Box;
        Texture texture = Atlases.LargeTextures.Atlas.Value.BackingTexture;
        sprites.Enqueue(texture, new SpriteData(texture)
        {
            Position = new Vector3(box.Position, BattleBox.BaseZ + 106f),
            Scale = box.Size / texture.Size,
            Rotation = box.Rotation,
            Tint = Color.White * box.Opacity * float.InverseLerp(0f, 0.4f, boxFreezeProgress)
        }, BlendState.AlphaBlend, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.BattleBoxFreezeShader, PrepareBoxFreezeParameters));
    }

    private void PrepareBoxFreezeParameters(FrameContext frame, RenderPass pass)
    {
        float freezeDistance = 0.75f;
        Vector2 freezeOrigin = Vector2.One * 0.5f - FreezeDirection * 0.4f;

        frame.CommandBuffer.BindFragmentUniformData(0, new BoxFreezeUniforms()
        {
            MaxFreezeDistanceAndFreezeProgress = new Vector4(freezeDistance, boxFreezeProgress, -windTimer * 0.03f, 0f),
            FreezeOriginDirection = new Vector4(freezeOrigin.X, freezeOrigin.Y, FreezeDirection.X, FreezeDirection.Y),
            NoiseUvBounds = Atlases.LargeTextures.SemiRandom_OmitPadding.VectorBounds,
            FrostBounds = Atlases.LargeTextures.FrostNoise_OmitPadding.VectorBounds,
        });
        pass.BindFragmentSamplerTextures(1, new RenderPass.SamplerTexture(Scene.Device.LinearWrap, Atlases.LargeTextures.Atlas.Value.BackingTexture));
    }
}
