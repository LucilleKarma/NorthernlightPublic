using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Content.Borders;
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

public sealed class FallingStars : Attack<RoaringKnight>
{
    private bool shatteredBattleBoxYet;

    private float windIntensity;

    private float windTimer;

    [StructLayout(LayoutKind.Sequential)]
    public struct Uniforms
    {
        public Vector4 Time;

        public Vector4 OverlayColor;

        public Vector4 WindTextureBounds;
    }

    public readonly record struct Stats(float CutSpawnStartTime, float CutSpawnEndTime, float CutSpawnGraceTime, float StarSummonDelay,
        float StarSummonDuration, float GrazePoints, float DaggerSpawnRate, int ShardDamage, int DaggerDamage, int StarDamage, Vector2 BoxSize, bool Hard, bool HoverAtSidesCheeseAllowed);

    private readonly List<float> cutDirections = [];

    private readonly OwnedTimer starSpawnTimer;

    private readonly OwnedTimer daggerSpawnTimer;

    internal readonly Stats stats;

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => stats.Hard ? LocalizationEntries.BattleFlavorTextFallingStarsHard : LocalizationEntries.BattleFlavorTextFallingStars;

    public FallingStars(BattleHandler battle, Stats stats) : base(battle)
    {
        starSpawnTimer = Scene.Time.Rent();
        daggerSpawnTimer = Scene.Time.Rent();
        this.stats = stats;
    }

    protected override void UpdateInternal()
    {
        float telegraphSpawnRate = 0.04f;
        float cutSpawnStartTime = stats.CutSpawnStartTime;
        float cutSpawnEndTime = stats.CutSpawnEndTime;
        float cutSpawnGraceTime = stats.CutSpawnGraceTime;
        float cutSpawnProgress = float.InverseLerp(cutSpawnStartTime, cutSpawnEndTime, AttackTimer);
        float telegraphDuration = cutSpawnEndTime + cutSpawnGraceTime;

        if (cutSpawnProgress > 0f && cutSpawnProgress < 1f && starSpawnTimer.Cycle(telegraphSpawnRate))
        {
            float remainingTime = cutSpawnEndTime - AttackTimer;
            Vector2 cutSpawnPosition = Battle.Box.Position + Scene.Rng.NextCircular(Battle.Box.Size.X * 0.25f);
            float theta = Scene.Rng.NextFloat(-0.5f, 0.5f);

            Bullets.Add(new BoxCutTelegraph(this, Scene, cutSpawnPosition, theta.ToUnit(), remainingTime + cutSpawnGraceTime, cutSpawnGraceTime));

            cutDirections.Add(theta);
            cutDirections.Add(theta + float.Pi);

            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("SliceTelegraph.wav"), 0.2f));
        }

        float postShatterTimer = AttackTimer - telegraphDuration;
        if (AttackTimer < telegraphDuration)
        {
            float waveProgress = AttackTimer / telegraphDuration;
            float wave = float.Bump(waveProgress);
            Vector2 center = BattleHandler.StartingBoxCenter;
            Battle.Box.Position = center + Vector2.UnitX * wave * (1f - waveProgress) * 50f;
            Battle.Box.Position += Scene.Rng.NextCircular(waveProgress.Cubed() * 12f);

            Attacker.RestingPosition = Attacker.SpawnPosition + Vector3.UnitX * 250f;
        }
        else
        {
            if (!shatteredBattleBoxYet)
            {
                ShatterBattleBox();
                shatteredBattleBoxYet = true;
            }

            Battle.Box.ShatterDistance = postShatterTimer * float.Pow(float.InverseLerp(0f, 0.2f, postShatterTimer), 1.75f) * float.Pow(float.InverseLerp(0f, 0.25f, postShatterTimer), 1.5f) * 9000f;
            Battle.Box.Size = Vector2.One * 999999f;
            Scene.ScreenDarkening = float.InverseLerp(0f, 0.02f, postShatterTimer) * 0.5f;
        }

        float postSlashAnimationTimer = postShatterTimer - stats.StarSummonDelay - 0.25f;
        if (AttackTimer < telegraphDuration - 0.4f)
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightSlashAnimation, 3, true);
            Attacker.LimitTextureFrame(1);
        }
        else if (postSlashAnimationTimer <= 0f)
        {
            Attacker.SetTexture(Atlases.KnightScene.KnightSlashAnimation, 3, true);
            Attacker.LimitTextureFrame(2);
        }
        else
        {
            int ballFrames = 5;
            int ballTransitionFrames = 4;
            int ballAnimationFramesToPlay = ballFrames * 2;

            if (postSlashAnimationTimer >= RoaringKnight.StandardFrameUpdateRate * (ballAnimationFramesToPlay + ballTransitionFrames + 0.5f))
                Attacker.SetTexture(Atlases.KnightScene.KnightAuraFarmPose, 1);
            else if (postSlashAnimationTimer >= RoaringKnight.StandardFrameUpdateRate * ballAnimationFramesToPlay)
                Attacker.SetTexture(Atlases.KnightScene.KnightBallFormTransition, ballTransitionFrames, false);
            else
                Attacker.SetTexture(Atlases.KnightScene.KnightBallFormAnimation, ballFrames, true);
        }

        float daggerSpawnProgress = float.InverseLerp(0f, stats.StarSummonDuration, postShatterTimer - stats.StarSummonDelay);
        float starSpawnRate = float.Lerp(0.3f, 0.18f, daggerSpawnProgress);
        if (AttackTimer >= telegraphDuration + stats.StarSummonDelay)
        {
            float relativeTimer = AttackTimer - (telegraphDuration + stats.StarSummonDelay);

            bool done = relativeTimer >= stats.StarSummonDuration * 0.85f;
            if (done)
                windIntensity = float.Clamp(windIntensity - Scene.Time.DeltaTime * 1.5f, 0f, 1f);
            else
                windIntensity = float.Clamp(windIntensity + Scene.Time.DeltaTime * 2f, 0f, 1f);

            windTimer += windIntensity * Scene.Time.DeltaTime;

            float borderStarsWindup = float.InverseLerp(0f, 0.05f, relativeTimer / stats.StarSummonDuration);
            KnightBorder.Instance.TwinkleSpeedupFactor = float.Smoothstep(1f, 2.81f, borderStarsWindup);
            KnightBorder.Instance.ShineIntensityFactor = float.Smoothstep(1f, 1.67f, borderStarsWindup);

            if (!done)
            {
                if (starSpawnTimer.Cycle(starSpawnRate))
                {
                    int starDamage = stats.StarDamage;
                    float scale = Scene.Rng.NextFloat(0.8f, 1.51f);
                    Vector2 spawnPosition = new Vector2(Scene.Rng.NextFloat(NorthernlightGame.KnightFightResolution.X), -50f);
                    Vector2 velocity = Vector2.UnitY * Scene.Rng.NextFloat(350f, 500f) / scale;
                    Bullets.Add(new LargeStar(Scene, this, spawnPosition, velocity, starDamage, stats.GrazePoints, scale, 3f));
                }

                float daggerSpawnRate = stats.DaggerSpawnRate;
                if (daggerSpawnTimer.Cycle(daggerSpawnRate))
                {
                    int daggerDamage = stats.DaggerDamage;
                    float padding = stats.HoverAtSidesCheeseAllowed ? 0f : 56f;

                    // This is mostly just done to relax the RNG and
                    // prevent cases where entire wall sections of
                    // the bullet box get covered in daggers.
                    int tries = 0;
                    float daggerTelegraphDuration = 0.45f;

                    Vector2 spawnPosition;
                    Vector2 aimDirection = Vector2.UnitX;
                    do
                    {
                        spawnPosition = TargetPosition + new Vector2(-150f, Scene.Rng.NextFloat(-240f, 240f));
                        if (Scene.Rng.NextBool(2))
                            spawnPosition.X = TargetPosition.X * 2f - spawnPosition.X;
                        if (Scene.Rng.NextBool(3))
                            spawnPosition.Y = TargetPosition.Y;

                        if (stats.Hard)
                        {
                            aimDirection = (Angle.Tau * Scene.Rng.Next(8) / 8f).ToUnit();
                            spawnPosition = TargetPosition - aimDirection * 180f;
                        }
                        else
                        {
                            float directionToCenterX = float.Sign(NorthernlightGame.KnightFightResolution.X * 0.5f - spawnPosition.X);
                            aimDirection.X = directionToCenterX;
                        }

                        tries++;
                    }
                    while (KeepPlayerInsideGame(Scene, spawnPosition, Vector2.One, padding) != spawnPosition && tries < 50);

                    Bullets.Add(new BlackDagger(this, Scene, daggerDamage, stats.GrazePoints, spawnPosition, aimDirection, 50f, telegraphDuration: daggerTelegraphDuration)
                    {
                        IncludeWhiteLine = true,
                        Scale = 0.9f
                    });
                }
            }
        }

        if (shatteredBattleBoxYet)
        {
            Vector3 soulPosition = Battle.Soul.Position;
            Vector2 restrictedSoulPosition = KeepPlayerInsideGame(Scene, soulPosition.AsVector2(), Vector2.One * Soul.Scale * 20f);
            Battle.Soul.Position = new Vector3(restrictedSoulPosition, Battle.Soul.Position.Z);
        }

        if (AttackTimer >= telegraphDuration + stats.StarSummonDelay + stats.StarSummonDuration)
        {
            Battle.Box.CutAngles.Clear();
            Battle.Box.ClearAfterimages(false);
            Battle.Box.Size = Vector2.Zero;
            End();
        }
    }

    internal static Vector2 KeepPlayerInsideGame(KnightFightScene scene, Vector2 position, Vector2 area, float sidePadding = 0f)
    {
        // OK look I know I promised being
        // able to leave the battle box (you're welcome)
        // but this is a bit ridiculous...
        Vector2 bottom = new Vector2(0.5f, 1f) * NorthernlightGame.KnightFightResolution;
        Vector2 size = new Vector2(NorthernlightGame.KnightFightResolution.X, scene.battleMenu.DedicatedTextSpaceHeight + 76f);
        Vector2 topOfGameMenu = bottom - Vector2.UnitY * size;
        Rectangle rectangle = new Rectangle(position, area);

        if (rectangle.Left < sidePadding)
            position = new Vector2(area.X * 0.5f, position.Y);
        if (rectangle.Right > NorthernlightGame.KnightFightResolution.X + sidePadding)
            position = new Vector2(NorthernlightGame.KnightFightResolution.X - area.X * 0.5f, position.Y);
        if (rectangle.Top < 0f)
            position = new Vector2(position.X, area.Y * 0.5f);
        if (rectangle.Bottom > topOfGameMenu.Y)
            position = new Vector2(position.X, topOfGameMenu.Y - area.Y * 0.5f);

        return position;
    }

    private void ShatterBattleBox()
    {
        Scene.AddScreenShake(45f);

        int shardCount = 32;
        for (int i = 0; i < shardCount; i++)
        {
            Vector2 direction = (float.Tau * (i + Scene.Rng.NextFloat(-0.3f, 0.3f)) / shardCount * 2f).ToUnit();
            Vector2 velocity = direction * Scene.Rng.NextFloat(185f, 212f);
            if (Scene.Rng.NextBool(2))
                velocity *= 0.45f;

            Bullets.Add(new BattleBoxShard(Scene, Battle.Box.Position, velocity, stats.ShardDamage, stats.GrazePoints)
            {
                Wiggles = true
            });
        }

        for (int i = 0; i < 5; i++)
        {
            float spawnProgress = i / 4f;
            float maxScale = float.Lerp(750f, 3000f, float.Pow(spawnProgress, 0.65f));
            Bullets.Add(new ShockwaveParticle(Scene, 0.4f, maxScale, Battle.Box.Position));
        }

        for (int i = 0; i < 40; i++)
        {
            float maxScale = Scene.Rng.NextFloat(0.8f, 1.2f);
            float residueLifetime = Scene.Rng.NextFloat(0.3f, 0.95f) * maxScale;
            Vector2 residueSpawnPosition = Battle.Box.Position + Scene.Rng.NextCircular(Battle.Box.Size.X * 0.45f);
            Bullets.Add(new BattleBoxResidueParticle(Scene, residueLifetime, maxScale, residueSpawnPosition));
        }

        Battle.Box.CutAngles.AddRange(cutDirections);
    }

    public override void ExtraPrepareRender(SpriteRenderer sprites)
    {
        Texture texture = Atlases.LargeTextures.CloudyNoise.BackingTexture;

        SpriteData windData = new SpriteData(texture)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * 0.5f, -50f),
            Scale = NorthernlightGame.KnightFightResolution / texture.Size * 2f,
            Tint = Color.White * windIntensity,
            Rotation = Angle.PiOver2
        };

        sprites.Enqueue(texture, windData, default, true, new SpriteRenderer.ShaderWithPreparations(Shaders.WindLinesShader, PrepareWindParameters));
    }

    private void PrepareWindParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            Time = new Vector4(windTimer * -1.1f, 0f, 0f, 0f),
            OverlayColor = new Vector4(0.2f, 0f, 0.16f, 1f) * windIntensity * 0.5f,
            WindTextureBounds = Atlases.LargeTextures.WindLines.VectorBounds,
        });
    }
}
