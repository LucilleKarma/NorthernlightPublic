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
using SDL3;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class BoxSplit : Attack<RoaringKnight>
{
    public readonly record struct Stats(int SplitCount, int ShardCount, int ShardDamage, int MaxRepeatSplitDirections, float ShardGrazePoints, float StartingTelegraphDuration,
        float SliceDuration, float SplitHoldDuration, float DiagonalSliceConsiderationProbability, float MaxSplitDistance, float ShardSpeedFactor, bool CanCreateShardWalls, Vector2 BoxSize);

    private int splitCounter;

    private int sameSplitDirectionCounter;

    private bool attackStarted;

    private bool boxInitialized;

    private bool regenerateBoxTexture;

    private float currentSplitDistance;

    private float splitTimer;

    private float splitCountdown;

    private float afterimageVisualsProgress;

    private Vector2 previousSplitDirection;

    private Vector2 nextSplitDirection;

    private readonly Stats stats;

    private static Texture sourceSurface;

    private static Texture halfBoxA;

    private static Texture halfBoxB;

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextBoxSplit;

    public BoxSplit(BattleHandler battle, Stats stats) : base(battle)
    {
        this.stats = stats;
    }

    internal static void LoadEarly(KnightFightScene scene, GraphicsDevice device)
    {
        Vector2 textureSize = Vector2.One * 400f;

        SDL.GPUTextureCreateInfo textureCreationInfo = new SDL.GPUTextureCreateInfo()
        {
            Width = (uint)textureSize.X,
            Height = (uint)textureSize.X,
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
        };

        sourceSurface ??= new Texture(textureCreationInfo, device);
        halfBoxA ??= new Texture(textureCreationInfo, device);
        halfBoxB ??= new Texture(textureCreationInfo, device);

        scene.Game.EnqueueForBeforeNextRender(f =>
        {
            BoxSplit weird = new BoxSplit(scene.BattleHandler, new Stats()
            {
                BoxSize = Vector2.One * 300f
            });
            weird.InitializeBoxTexture(f);
            weird.RegenerateBoxTexture(f);
            weird.OverrideBattleBoxRender(scene.Renderer, f);
        });
    }

    protected override void UpdateInternal()
    {
        if (!attackStarted)
        {
            Battle.Box.Rotation = 0f;
            attackStarted = true;
            BeginNextSplit();
        }

        if (splitCountdown > 0f)
        {
            if (splitCounter < stats.SplitCount - 1)
                Scene.ScreenDarkening = float.Clamp(Scene.ScreenDarkening + Scene.Time.DeltaTime * 2.3f, 0f, 0.8f);

            float previousSplitCountdown = splitCountdown;
            splitCountdown -= Scene.Time.DeltaTime;

            Attacker.SlashAnimationSpeedup = 2f;
            float sliceThreshold = 0.4f / Attacker.SlashAnimationSpeedup;
            if (previousSplitCountdown >= sliceThreshold && splitCountdown < sliceThreshold)
                PerformKnightTeleport();

            if (splitCountdown <= 0f)
            {
                Cut();
                splitCountdown = 0f;
            }
        }

        float previousSplitDistance = currentSplitDistance;
        Attacker.Velocity = Vector3.LerpDt(Attacker.Velocity, Vector3.Zero, 50f, Scene.Time);
        Attacker.RepositionDelay = 0.25f;

        if (splitTimer > 0f)
        {
            Battle.Box.Rotation = nextSplitDirection + Angle.PiOver2;
            Battle.Box.Size = Vector2.One * 999999f;

            ManuallyKeepSoulInBox();

            splitTimer += Scene.Time.DeltaTime;

            float splitDelay = 0.1f;
            float splitDuration = 0.16f;
            float holdDuration = stats.SplitHoldDuration;
            float outwardProgress = float.InverseLerp(0f, splitDuration, splitTimer - splitDelay);
            float reconvergenceProgress = float.InverseLerp(0f, splitDuration, splitTimer - splitDelay - splitDuration - holdDuration);

            MoveBoxPartsApart(outwardProgress, reconvergenceProgress);

            float holdTimer = float.Max(0f, splitTimer - splitDelay - splitDuration);
            currentSplitDistance += holdTimer * (1f - reconvergenceProgress) * stats.MaxSplitDistance * 0.45f;

            if (splitCountdown <= 0f && reconvergenceProgress > 0f)
            {
                if (splitCounter < stats.SplitCount)
                    BeginNextSplit();
            }

            if (reconvergenceProgress >= 1f)
            {
                if (splitCounter >= stats.SplitCount)
                {
                    Battle.Box.ClearAfterimages(true);
                    for (int i = 0; i < Battle.Box.afterimages.Count; i++)
                    {
                        Battle.Box.afterimages[i] = Battle.Box.afterimages[i] with
                        {
                            Size = stats.BoxSize
                        };
                    }

                    Battle.Box.Size = stats.BoxSize;
                    End();

                    Attacker.Position = new Vector3(Attacker.Position.AsVector2(), Attacker.SpawnPosition.Z);
                    foreach (KnightAfterimage afterimage in Attacker.Afterimages)
                        afterimage.Position = new Vector3(afterimage.Position.AsVector2(), Attacker.Position.Z);

                    return;
                }

                splitTimer = 0f;
            }
        }
        else
        {
            Battle.Box.Rotation = 0f;
            Battle.Box.Size = stats.BoxSize;
        }

        if (afterimageVisualsProgress > 0f)
        {
            afterimageVisualsProgress += Scene.Time.DeltaTime * 2.5f;
            if (afterimageVisualsProgress >= 1f)
                afterimageVisualsProgress = 0f;
        }

        // Move the soul along with the box as it
        // splits.
        if (currentSplitDistance - previousSplitDistance >= 9f)
        {
            Soul soul = Battle.Soul;
            Vector2 soulPosition = soul.Position.AsVector2();

            int side = -float.Sign(Vector2.Dot(Battle.Box.Position - soulPosition, previousSplitDirection.RotatedBy(Angle.PiOver2)));
            float splitDistanceChange = currentSplitDistance - previousSplitDistance;
            soul.Position += previousSplitDirection.RotatedBy(side * Angle.PiOver2).AsVector3() * splitDistanceChange;

            ManuallyKeepSoulInBox();
        }
    }

    private void ManuallyKeepSoulInBox()
    {
        Soul soul = Battle.Soul;
        Angle theta = previousSplitDirection + Angle.PiOver2;
        bool diagonal = float.Abs(previousSplitDirection.X * previousSplitDirection.Y) >= 0.4f;

        float splitDistance = currentSplitDistance;
        Vector2 position = Battle.Box.Position;
        Vector2 collisionBoxCenter = soul.Position.AsVector2();
        Vector2 size = new Vector2(stats.BoxSize.X + splitDistance, stats.BoxSize.Y);
        if (diagonal)
        {
            theta = 0f;
            size = stats.BoxSize + Vector2.One * splitDistance / float.Sqrt(2f);
        }

        Vector2 inverseFrame = position + (collisionBoxCenter - position).RotatedBy(-theta);
        Vector2 collisionBoxArea = Vector2.One * Soul.Scale * 20f;
        inverseFrame = Vector2.Clamp(inverseFrame, position - size * 0.5f + collisionBoxArea * 0.5f, position + size * 0.5f - collisionBoxArea * 0.5f);

        Vector2 lockedSoulPosition = position + (inverseFrame - position).RotatedBy(theta);

        soul.Position = new Vector3(lockedSoulPosition, soul.Position.Z);
    }

    private void BeginNextSplit()
    {
        splitCounter++;

        float splitProgress = splitCounter / (float)(stats.SplitCount - 1f);
        float telegraphDuration = stats.StartingTelegraphDuration * float.Lerp(1f, 0.45f, float.Pow(splitProgress, 0.75f));
        if (splitCounter <= 1)
            telegraphDuration *= 1.75f;

        splitCountdown = telegraphDuration;

        int totalDirections = 4;
        if (Scene.Rng.NextBool(stats.DiagonalSliceConsiderationProbability))
            totalDirections = 8;

        int tries = 0;
        Vector2 soulDirectionFromBoxCenter = (Battle.Soul.Position.AsVector2() - Battle.Box.Position).SafeNormalize(Vector2.UnitY);

        do
        {
            int cardinal = Scene.Rng.Next(totalDirections);
            Angle randomAngularVariance = Scene.Rng.NextFloat(-0.06f, 0.06f);
            nextSplitDirection = (Angle.Tau * cardinal / totalDirections + randomAngularVariance).Unit;
            tries++;
        }
        while (ShouldChooseNewSplitDirection(soulDirectionFromBoxCenter) && tries < 100);

        if (float.Abs(Vector2.Dot(nextSplitDirection, previousSplitDirection)) >= 0.95f)
            sameSplitDirectionCounter++;
        else
            sameSplitDirectionCounter = 0;

        Bullets.Add(new AltBoxCutTelegraph(this, Scene, Battle.Box.Position, nextSplitDirection, telegraphDuration, stats.SliceDuration));
    }

    private bool ShouldChooseNewSplitDirection(Vector2 soulDirectionFromBoxCenter)
    {
        // When the telegraphs get faster and faster, it gets more
        // and more important that the telegraph line not
        // end up slicing right down the soul, as that creates
        // nigh-unreactable situations that are not fun.
        if (float.Abs(Vector2.Dot(soulDirectionFromBoxCenter, nextSplitDirection)) >= 0.75f)
            return true;

        // Stop doing the same split over and over, please!
        int maxSplitsToDoInARow = stats.MaxRepeatSplitDirections;
        if (float.Abs(Vector2.Dot(nextSplitDirection, previousSplitDirection)) >= 0.95f && sameSplitDirectionCounter >= maxSplitsToDoInARow - 1)
            return true;

        return false;
    }

    private void MoveBoxPartsApart(float outwardProgrees, float reconvergenceProgress)
    {
        float maxSplitDistance = stats.MaxSplitDistance;
        if (reconvergenceProgress > 0f && reconvergenceProgress <= 1f)
            currentSplitDistance = (1f - reconvergenceProgress.Cubed()) * maxSplitDistance;

        else if (outwardProgrees <= 1f)
            currentSplitDistance = (1f - (1f - outwardProgrees).Cubed()) * maxSplitDistance;
    }

    /// <summary>
    ///     Performs the Knight's teleport.
    /// </summary>
    public void PerformKnightTeleport()
    {
        Attacker.RenderState = new RoaringKnight.KnightRenderState(Atlases.KnightScene.KnightSlashAnimation, 3, loops: false);
        Attacker.HorizontallyFlip = splitCounter % 2 == 0;

        float direction = Attacker.HorizontallyFlip ? 1f : -1f;
        Vector2 teleportPosition = Battle.Box.Position + Vector2.UnitX * (stats.BoxSize.X * 0.5f + 180f) * direction;
        Attacker.Position = new Vector3(teleportPosition, Attacker.SpawnPosition.Z + 785f);
        Attacker.Velocity = Vector3.UnitX * Attacker.Velocity.Length() * direction;
    }

    /// <summary>
    ///     Performs a cut on the battle box.
    /// </summary>
    public void Cut()
    {
        Scene.Audio.Play(new Sound(AudioBuffer.FromWav("BattleBoxBreakBurst.wav"), 0.8f));

        Sound pitchedUpBoxBreak = new Sound(AudioBuffer.FromWav("BoxBreak.wav"), 0.5f);
        pitchedUpBoxBreak.Stream.FrequencyRatio = 1f;
        Scene.Audio.Play(pitchedUpBoxBreak);

        previousSplitDirection = nextSplitDirection;
        Scene.AddScreenShake(10f);
        splitTimer = 0.0001f;
        regenerateBoxTexture = true;
        afterimageVisualsProgress = 0.0001f;

        int shardCount = stats.ShardCount;
        int shardDamage = stats.ShardDamage;
        float wallIndex = -50f;
        if (splitCounter % 2 == 0)
            wallIndex = Scene.Rng.NextFloat(1.333f, shardCount - 2.333f);

        bool usingWall = wallIndex >= 0;
        float scale = 1.5f;
        float glowCountdown = 0.09f;
        Vector2 forward = nextSplitDirection.RotatedBy(Angle.PiOver2);
        Vector2 side = nextSplitDirection;
        bool diagonalCut = float.Abs(nextSplitDirection.X - float.Sqrt(2f) * 0.5f) <= 0.25f ||
                           float.Abs(nextSplitDirection.Y - float.Sqrt(2f) * 0.5f) <= 0.25f;

        for (int i = 0; i < shardCount; i++)
        {
            float shardSpawnProgress = i / (float)(shardCount - 1f);
            Vector2 spawnPosition = Battle.Box.Position + side * (shardSpawnProgress - 0.5f) * stats.BoxSize.X;
            Vector2 velocity = forward * Scene.Rng.NextFloat(224f, 236f) * stats.ShardSpeedFactor;

            if (usingWall && stats.CanCreateShardWalls)
            {
                bool partOfWall = float.Abs(i - wallIndex) <= 1.05f;
                if (partOfWall)
                    velocity *= 1.05f;
                else
                    velocity *= 0.5f;
            }
            else if (i % 2 == 0)
                velocity *= 0.5f;

            if (diagonalCut)
                velocity *= 0.8f;

            Bullets.Add(new BattleBoxShard(Scene, spawnPosition, -velocity, shardDamage, stats.ShardGrazePoints)
            {
                Scale = scale,
                Wiggles = true,
                GlowCountdown = glowCountdown,
                Acceleration = 24000f,
                AccelerationDelay = 0.33f
            });
            Bullets.Add(new BattleBoxShard(Scene, spawnPosition, velocity, shardDamage, stats.ShardGrazePoints)
            {
                Scale = scale,
                Wiggles = true,
                GlowCountdown = glowCountdown,
                Acceleration = 24000f,
                AccelerationDelay = 0.33f
            });
        }
    }

    /// <summary>
    ///     Initializes the box texture, effectively
    ///     copying over the contents of a normal battle box
    ///     for subsequent cuts by the Knight.
    /// </summary>
    private void InitializeBoxTexture(FrameContext frame)
    {
        using TextureWriteScope scope = sourceSurface.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp);

        MeshRenderer scopedMeshes = scope.Renderer.Get<MeshRenderer>();
        SpriteRenderer scopedSprites = scope.Renderer.Get<SpriteRenderer>();

        Vector2 previousBoxPosition = Battle.Box.Position;
        Vector2 previousBoxSize = Battle.Box.Size;
        Battle.Box.Rotation = 0f;
        Battle.Box.Position = sourceSurface.Size * 0.5f;
        Battle.Box.Size = stats.BoxSize;
        Battle.Box.PrepareRender(scopedSprites, scopedMeshes, frame, 1);
        Battle.Box.Position = previousBoxPosition;
        Battle.Box.Size = previousBoxSize;
    }

    private void RegenerateBoxTexture(FrameContext frame)
    {
        // This takes advantage of a subtractive blend
        // state to effectively cut out one triangular half of the
        // battle box on each side and emit their
        // outputs to the final render target.
        AtlasEntry triangle = Atlases.KnightScene.Triangle;
        Vector3 drawPosition = new Vector3(halfBoxA.Size * 0.5f, 0f);
        Vector2 triangleScale = halfBoxA.Size / triangle.Area.Size * float.Sqrt(2f);

        using (TextureWriteScope scope = halfBoxA.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp))
        {
            SpriteRenderer scopedSprites = scope.Renderer.Get<SpriteRenderer>();
            scopedSprites.Enqueue(sourceSurface, new SpriteData(sourceSurface)
            {
                Position = drawPosition,
                Scale = Vector2.One
            });
            scopedSprites.Enqueue(triangle, new SpriteData(triangle)
            {
                Position = drawPosition + Vector3.UnitZ * 50f,
                Scale = triangleScale,
                Rotation = previousSplitDirection + Angle.PiOver4 + Angle.Pi,
                Tint = Color.White * 10f,
            }, blendState: BlendState.Subtractive);
        }

        using (TextureWriteScope scope = halfBoxB.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp))
        {
            SpriteRenderer scopedSprites = scope.Renderer.Get<SpriteRenderer>();
            scopedSprites.Enqueue(sourceSurface, new SpriteData(sourceSurface)
            {
                Position = drawPosition,
                Scale = Vector2.One
            });
            scopedSprites.Enqueue(triangle, new SpriteData(triangle)
            {
                Position = drawPosition + Vector3.UnitZ * 50f,
                Scale = triangleScale,
                Tint = Color.White * 10f,
                Rotation = previousSplitDirection + Angle.PiOver4
            }, blendState: BlendState.Subtractive);
        }

        // Feed back the new split boxes back into
        // the surface target, so that splits can be remembered
        // as the Knight repeatedly cuts the battle box.
        // This incurs a tiny offset across the box
        // as to give the impression of a slightly imperfect cut.
        using (TextureWriteScope scope = sourceSurface.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp))
        {
            SpriteRenderer scopedSprites = scope.Renderer.Get<SpriteRenderer>();

            Vector3 splitOffset = previousSplitDirection.AsVector3() * Scene.Rng.NextFloat(1f, 2f) * (Scene.Rng.NextBool(2) ? 1f : -1f);
            Vector3 drawOrigin = new Vector3(sourceSurface.Size * 0.5f + Scene.Rng.NextCircular(6f), 0f);
            scopedSprites.Enqueue(halfBoxA, new SpriteData(halfBoxA)
            {
                Position = drawOrigin - splitOffset,
                Scale = Vector2.One
            }, translucent: true);
            scopedSprites.Enqueue(halfBoxB, new SpriteData(halfBoxB)
            {
                Position = drawOrigin + splitOffset,
                Scale = Vector2.One
            });

            // Also, while we're here, leave an imprint
            // of a cut in the box.

            AtlasEntry line = Atlases.KnightScene.Pixel;
            Vector2 cutScale = new Vector2((stats.BoxSize.X - 10f) * BattleBox.AngleToSquare(new Angle(previousSplitDirection)).Length(), 3f);
            scopedSprites.Enqueue(line, new SpriteData(line)
            {
                Position = drawOrigin + Vector3.UnitZ * 150f,
                Scale = cutScale,
                Tint = new Color(0f, 0.1f, 0f),
                Rotation = previousSplitDirection,
            }, BlendState.NonPremultiplied);
        }
    }

    private void PrepareTextureInitializations()
    {
        Scene.Game.EnqueueForBeforeNextRender(f =>
        {
            if (!boxInitialized)
            {
                boxInitialized = true;
                InitializeBoxTexture(f);
            }

            if (regenerateBoxTexture)
            {
                regenerateBoxTexture = false;
                RegenerateBoxTexture(f);
            }
        });
    }

    public override bool OverrideBattleBoxRender(Renderer renderer, FrameContext frame)
    {
        if (Battle.Box.Size.X < stats.BoxSize.X || Battle.Box.Size.Y < stats.BoxSize.Y)
            return false;

        if (!boxInitialized || regenerateBoxTexture)
            PrepareTextureInitializations();

        if (!boxInitialized)
            return true;

        SpriteRenderer sprites = renderer.Get<SpriteRenderer>();

        Vector3 halfOffset = previousSplitDirection.RotatedBy(Angle.PiOver2).AsVector3() * currentSplitDistance * 0.5f;
        if (currentSplitDistance > 0f || (splitTimer <= 0.05f && splitTimer > 0f))
        {
            sprites.Enqueue(halfBoxA, new SpriteData(halfBoxA)
            {
                Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ) + halfOffset,
                Scale = Vector2.One,
                Tint = Color.White
            });
            sprites.Enqueue(halfBoxB, new SpriteData(halfBoxB)
            {
                Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ) - halfOffset,
                Scale = Vector2.One,
                Tint = Color.White
            });

            AtlasEntry blackFlames = Atlases.KnightScene.BattleBoxFlames;
            int flamesFrame = (int)(AttackTimer * 14f % 6f);
            float battleBoxCutLength = BattleBox.AngleToSquare(new Angle(previousSplitDirection)).Length();
            float flamesScale = stats.BoxSize.X / blackFlames.Area.Width * battleBoxCutLength;
            Color flamesColor = new Color(128, 128, 128);

            SpriteData flamesData = new SpriteData(blackFlames, 0, flamesFrame, 1, 6)
            {
                Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ - 5f) + halfOffset,
                Scale = Vector2.One * flamesScale,
                Tint = flamesColor,
                Rotation = previousSplitDirection
            };
            sprites.Enqueue(blackFlames, flamesData);

            flamesData = new SpriteData(blackFlames, 0, flamesFrame, 1, 6)
            {
                Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ - 5f) - halfOffset,
                Scale = Vector2.One * flamesScale,
                Tint = flamesColor,
                Rotation = previousSplitDirection + Angle.Pi
            };
            sprites.Enqueue(blackFlames, flamesData);
        }
        else
        {
            sprites.Enqueue(sourceSurface, new SpriteData(sourceSurface)
            {
                Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ),
                Scale = Vector2.One,
                Tint = Color.White
            });
        }

        if (afterimageVisualsProgress > 0f)
        {
            for (int i = 3; i >= 0; i--)
            {
                float afterimageProgress = i / 3f;
                float opacity = float.InverseLerp(1f, 0.4f, afterimageVisualsProgress) * 0.9f;
                float afterimageSplitDistance = afterimageVisualsProgress.Squared() * 120f;
                Vector3 afterimageOffset = previousSplitDirection.AsVector3() * afterimageSplitDistance;
                Vector2 scale = new Vector2(1f + afterimageProgress * afterimageVisualsProgress * 0.5f, 1f);

                sprites.Enqueue(halfBoxA, new SpriteData(halfBoxA)
                {
                    Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ + 10f - afterimageProgress * 6f) + afterimageOffset,
                    Scale = scale,
                    Tint = Color.White * opacity
                });
                sprites.Enqueue(halfBoxB, new SpriteData(halfBoxB)
                {
                    Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ + 10f - afterimageProgress * 6f) - afterimageOffset,
                    Scale = scale,
                    Tint = Color.White * opacity
                });
            }
        }

        return true;
    }

    public override void OnEnd() => Attacker.HorizontallyFlip = false;
}
