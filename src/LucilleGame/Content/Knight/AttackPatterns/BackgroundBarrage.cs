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
using SDL3;

namespace Northernlight.Content.Knight.AttackPatterns;

public sealed class BackgroundBarrage : Attack<RoaringKnight>
{

    [StructLayout(LayoutKind.Sequential)]
    private struct Uniforms
    {
        public Vector4 NormalizedBoxBounds;
    }

    public readonly record struct Stats(int BarrageCount, int ShardDamage, int TotalSwordsOnEachSide, float AverageImpactShardCount, float GrazePoints, float GeneralSpeedupFactor, float ImpactShardBaseSpeed, Vector2 BoxSize);

    private int barrageCounter;

    private int hitCounter;

    private bool startedTeleport;

    private bool readyToAttack;

    private bool startedAttackEnd;

    private bool boxInitialized;

    private float previousAttackTimer;

    private float postAttackTimer;

    internal readonly Stats stats;

    private readonly OwnedTimer soundTimer;

    private static Texture sourceSurface;

    private static Texture dentDistanceMap;

    private static Vector2 activeBoxSize;

    private readonly Queue<(float Angle, float DentDistance)> Dents = [];

    public override Vector2 StartingBoxSize => stats.BoxSize;

    public override LanguageAgnosticText MenuIndicatorFlavorText => LocalizationEntries.BattleFlavorTextBackgroundBarrage;

    public BackgroundBarrage(BattleHandler battle, Stats stats) : base(battle)
    {
        soundTimer = Scene.Time.Rent();
        this.stats = stats;
    }

    public override void OnStart() => boxInitialized = false;

    protected override void UpdateInternal()
    {
        Attacker.RepositionDelay = 0.2f;
        Attacker.Velocity = Vector3.Zero;
        if (barrageCounter >= stats.BarrageCount)
        {
            postAttackTimer += Scene.Time.DeltaTime;
            if (postAttackTimer >= 1.5f)
                EndAttack();

            return;
        }

        if (readyToAttack)
        {
            float wave = float.Sin(float.Tau * AttackTimer * 0.24f) * Attacker.Scale * 27.5f;
            Attacker.Velocity = Vector3.UnitY * wave;
            Attack();
        }
        else
        {
            AttackTimer.Reset();
            PerformAttackStartupStuff();
        }

        previousAttackTimer = AttackTimer;
        activeBoxSize = stats.BoxSize;
    }

    private void PerformAttackStartupStuff()
    {
        AtlasEntry startingTeleport = Atlases.KnightScene.KnightBallFormTransition;
        AtlasEntry backgroundTeleport = Atlases.KnightScene.KnightBallFormTransitionForward;
        AtlasEntry forwardPoseWithOpenEye = Atlases.KnightScene.KnightOpenEyeForward;

        // Start in the foreground.
        if (!startedTeleport)
        {
            startedTeleport = true;
            Attacker.InBackground = false;
            Attacker.SetTexture(startingTeleport, 4, loops: false, animatesInReverse: true);
        }

        // Teleport into the background.
        if (Attacker.RenderState.Texture == startingTeleport && Attacker.RenderState.CurrentFrame == 0)
        {
            Attacker.InBackground = true;
            Attacker.Position = new Vector3(NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 0.085f), 7.5f);
            Attacker.SetTexture(backgroundTeleport, 5, loops: false);
        }

        // Then open the Knight's eye and begin attacking.
        if (!readyToAttack && Attacker.RenderState.Texture == backgroundTeleport && Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1)
        {
            Attacker.SetTexture(forwardPoseWithOpenEye, 6, loops: false);
            readyToAttack = true;

            SummonSwords();
        }
    }

    private void SummonSwords()
    {
        int swordCount = stats.TotalSwordsOnEachSide * 2;
        int patternType = Scene.Rng.Next(3);
        for (int i = 0; i < swordCount; i++)
        {
            float swordSpawnProgress = i / (float)(swordCount - 1f);
            Angle theta = float.Lerp(-0.76f, 0.76f, swordSpawnProgress);

            // Zero is the standard upward pattern.
            // Side pattern.
            if (patternType == 1)
            {
                if (swordSpawnProgress < 0.5f)
                    theta += Angle.PiOver2;
                else
                    theta -= Angle.PiOver2;
            }

            // Radial pattern.
            if (patternType == 2)
                theta = Angle.Tau * swordSpawnProgress;

            Vector2 spawnPosition = Attacker.Position.AsVector2() + (Vector2.UnitY.RotatedBy(theta) * -120f - Vector2.UnitX * 20f) * Attacker.Scale;
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0f, 0f, theta - Angle.PiOver2);
            Bullets.Add(new BackgroundSword(this, Scene, stats.ShardDamage, stats.GrazePoints, spawnPosition, rotation));
        }
    }

    private void Attack()
    {
        AtlasEntry slashPose = Atlases.KnightScene.KnightOpenEyeForwardSlash;

        // Ensure the battle box doesn't go too far down or
        // up.
        float battleBoxCenterY = Battle.Box.Position.Y;
        float battleBoxTop = battleBoxCenterY - Battle.Box.Size.Y * 0.5f;
        float battleBoxBottom = battleBoxCenterY + Battle.Box.Size.Y * 0.5f;
        if (battleBoxTop < NorthernlightGame.KnightFightResolution.Y * 0.25f)
            Battle.Box.Position += Vector2.UnitY * Scene.Time.DeltaTime * 30f;
        if (battleBoxBottom > NorthernlightGame.KnightFightResolution.Y * 0.6f)
            Battle.Box.Position -= Vector2.UnitY * Scene.Time.DeltaTime * 30f;

        int swordCounter = 0;
        float barragesProgress = barrageCounter / (float)stats.BarrageCount;
        float speedupFactor = float.Max(stats.GeneralSpeedupFactor, 0.01f);
        float hoverDuration = float.Lerp(1.2f, 0.6f, barragesProgress) / speedupFactor;
        float spinDuration = float.Lerp(0.5f, 0.4f, barragesProgress) / speedupFactor;

        float slashAnimationThreshold = hoverDuration + spinDuration * 0.2f;
        if (previousAttackTimer < slashAnimationThreshold && AttackTimer >= slashAnimationThreshold)
            Attacker.SetTexture(slashPose, 6, loops: false);
        if (Attacker.RenderState.Texture == slashPose && Attacker.RenderState.CurrentFrame >= Attacker.RenderState.TotalFrames - 1 && Scene.Rng.NextBool(4))
        {
            Attacker.RenderState = new RoaringKnight.KnightRenderState(Atlases.KnightScene.KnightOpenEyeForward, 6, false)
            {
                CurrentFrame = 5
            };
        }

        for (int i = 0; i < Bullets.Count; i++)
        {
            Bullet bullet = Bullets[i];
            if (bullet is not BackgroundSword sword)
                continue;

            float relativeTimer = AttackTimer;
            float hoverProgress = float.InverseLerp(0f, hoverDuration, relativeTimer);
            if (hoverProgress > 0f && hoverProgress < 1f)
                sword.Velocity = sword.Forward * float.Sin(float.Tau * hoverProgress * 0.6f) * (1f - hoverProgress) * 20f;
            relativeTimer -= hoverDuration;

            float spinProgress = float.InverseLerp(0f, spinDuration, relativeTimer);
            if (spinProgress > 0f && spinProgress < 1f)
            {
                Angle spinAngle = float.Smoothstep(0f, float.Tau * 2f, spinProgress);
                sword.Rotation = sword.StartingRotation * Quaternion.CreateFromYawPitchRoll(0f, spinAngle, 0f);
            }
            relativeTimer -= spinDuration;

            float dashDelay = swordCounter * 0.11f;
            float dashSpeedupDuration = 1f;
            float dashProgress = float.InverseLerp(0f, dashSpeedupDuration, relativeTimer - dashDelay);
            if (dashProgress > 0f)
            {
                float spinAccelerationCoefficient = 32f;
                float rotationEase = 1f - float.Pow(0.5f, Scene.Time.DeltaTime * spinAccelerationCoefficient);

                // Recover the original firing angle in 2D space
                // from the starting angle quaternion.
                Quaternion startingRotation = sword.StartingRotation;
                float yawDirectionY = (startingRotation.W * startingRotation.Z + startingRotation.X * startingRotation.Y) * 2f;
                float yawDirectionX = 1f - (startingRotation.Y.Squared() + startingRotation.Z.Squared()) * 2f;
                float yaw = float.Atan2(yawDirectionY, yawDirectionX);

                // Then use it to determine the starting X direction.
                float horizontalBiasing = float.Cos(yaw) * float.CosPi(dashProgress);

                Quaternion spinVariance = Quaternion.CreateFromYawPitchRoll(0f, swordCounter + AttackTimer * 30f, 0f);
                Quaternion towardsScreen = Quaternion.CreateFromYawPitchRoll(0f, Angle.PiOver2 * 0.95f, Angle.PiOver2 - horizontalBiasing) * spinVariance;
                sword.Rotation = Quaternion.Slerp(sword.Rotation, towardsScreen, rotationEase);

                float reorientAccelerationCoefficient = 17.25f;
                Vector2 forwardVelocity = sword.Forward * 1000f;
                Vector2 boxDestination = Battle.Box.Position + Vector2.UnitY * float.Sin(swordCounter) * 80f;
                Vector2 toBoxDestination = (boxDestination - sword.Position).SafeNormalize(Vector2.UnitY) * 1600f;
                Vector2 idealVelocity = Vector2.Lerp(forwardVelocity, toBoxDestination, dashProgress);

                sword.Velocity = Vector2.LerpDt(sword.Velocity, idealVelocity, reorientAccelerationCoefficient, Scene.Time);
                sword.Position3D += Vector3.UnitZ * Scene.Time.DeltaTime * 2f;

                if (dashProgress <= 0.05f)
                {
                    Scene.AddScreenShake(1.25f);
                    if (soundTimer.Cycle(0.06f))
                        Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightRadialSplit.wav"), 0.24f));
                }
                if (dashProgress <= 0.5f)
                    sword.Velocity += Vector2.UnitX * Scene.Time.DeltaTime * float.Sign(horizontalBiasing) * 16000f;

                if (dashProgress >= 0.75f && sword.Position3D.Z >= 1f && float.Abs(sword.Position.X - Battle.Box.Position.X) <= Battle.Box.Size.X * 1.15f)
                {
                    hitCounter++;
                    Battle.Box.Size.X -= 3.2f;
                    sword.Kill();
                }
            }

            swordCounter++;
        }

        if (swordCounter <= 0)
        {
            barrageCounter++;
            if (barrageCounter < stats.BarrageCount)
                SummonSwords();
            AttackTimer.Set(hoverDuration * 0.3f);
        }
    }

    private void EndAttack()
    {
        AtlasEntry backgroundReturnTeleport = Atlases.KnightScene.KnightBallFormTransitionForward;
        AtlasEntry endingTeleport = Atlases.KnightScene.KnightBallFormTransition;
        if (!startedAttackEnd)
        {
            Attacker.SetTexture(backgroundReturnTeleport, 5, loops: false, animatesInReverse: true);
            startedAttackEnd = true;
        }

        // Teleport into the background.
        if (Attacker.RenderState.Texture == backgroundReturnTeleport && Attacker.RenderState.CurrentFrame == 0)
        {
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightTeleport.wav"), 0.7f));

            Attacker.InBackground = false;
            Attacker.Position = Attacker.SpawnPosition;
            Attacker.SetTexture(endingTeleport, 4, loops: false);
        }

        // End attack.
        if (Attacker.RenderState.Texture == endingTeleport)
        {
            if (Attacker.RenderState.CurrentFrame != Attacker.RenderState.TotalFrames - 1)
                AttackTimer.Reset();

            if (AttackTimer.Cycle(0.1f))
                End();
        }
    }

    /// <summary>
    ///     Adds a new dent to the battle box.
    /// </summary>
    public void AddDent(Angle relativeAngle, float pierceDistance)
    {
        Dents.Enqueue(new(relativeAngle, pierceDistance));
    }

    /// <summary>
    ///     Initializes the render target textures, effectively
    ///     copying over the contents of a normal battle box
    ///     for subsequent dents by the Knight's swords.
    /// </summary>
    private void InitializeTextures(FrameContext frame)
    {
        Vector2 textureSize = Vector2.One * 450f;

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

        sourceSurface?.Dispose();
        dentDistanceMap?.Dispose();

        sourceSurface = new Texture(textureCreationInfo, Scene.Device);
        dentDistanceMap = new Texture(textureCreationInfo with
        {
            Width = 4096,
            Height = 1,

            // OK so this used to be R32Float, but that caused
            // mysterious bugs with the additive blend state for
            // some users but not others, causing extremely strange
            // values in the dent buffer and immediately breaking
            // the look of the box entirely.

            // Maybe this is an SDL-GPU bug? That certainly wasn't on
            // my bingo card for this project.
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
        }, Scene.Device);

        {
            // Basically just clears the dent map.
            using TextureWriteScope _ = dentDistanceMap.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp);
        }

        {
            using TextureWriteScope scope = sourceSurface.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp);

            SpriteRenderer scopedSprites = scope.Renderer.Get<SpriteRenderer>();
            MeshRenderer scopedMeshes = scope.Renderer.Get<MeshRenderer>();

            Vector2 previousBoxPosition = Battle.Box.Position;
            Vector2 previousBoxSize = Battle.Box.Size;
            Angle previousBoxRotation = Battle.Box.Rotation;
            Battle.Box.Rotation = 0f;
            Battle.Box.Position = sourceSurface.Size * 0.5f;
            Battle.Box.Size = stats.BoxSize - Vector2.One * 2f;
            Battle.Box.PrepareRender(scopedSprites, scopedMeshes, frame, 1, borderWidthFactor: 2f);
            Battle.Box.Position = previousBoxPosition;
            Battle.Box.Size = previousBoxSize;
            Battle.Box.Rotation = previousBoxRotation;
        }
    }

    /// <summary>
    ///     Adds all dents to the dent profile.
    /// </summary>
    private void AddNewDents(FrameContext frame)
    {
        using TextureWriteScope scope = dentDistanceMap.Scope(Scene.SharedRenderers, frame, Scene.Device.PointClamp);

        SpriteRenderer scopedSprites = scope.Renderer.Get<SpriteRenderer>();

        AtlasEntry profile = Atlases.KnightScene.DentProfiles;
        while (Dents.TryDequeue(out var dent))
        {
            int frameY = Scene.Rng.Next(4);
            float x = dent.Angle / Angle.Tau * dentDistanceMap.Width;
            float scale = Scene.Rng.NextFloat(1.8f, 8.6f);
            scopedSprites.Enqueue(profile, new SpriteData(profile, 0, frameY, 1, 4)
            {
                Position = new Vector3(x, 0.5f, 0f),
                Scale = new Vector2(scale, 1f),
                Tint = new Color(dent.DentDistance / scale, 0f, 0f)
            }, BlendState.Additive);
        }
    }

    private void PrepareTextureInitializations()
    {
        Scene.Game.EnqueueForBeforeNextRender(f =>
        {
            if (!boxInitialized)
            {
                boxInitialized = true;
                InitializeTextures(f);
            }

            if (Dents.Count >= 1)
                AddNewDents(f);
        });
    }

    public override bool OverrideBattleBoxRender(Renderer renderer, FrameContext frame)
    {
        if (!startedTeleport)
            return false;

        if (!boxInitialized || Dents.Count >= 1)
            PrepareTextureInitializations();
        if (!boxInitialized || hitCounter <= 0)
            return false;

        Vector2 scale = Battle.Box.Size / stats.BoxSize;
        renderer.Get<SpriteRenderer>().Enqueue(sourceSurface, new SpriteData(sourceSurface)
        {
            Position = new Vector3(Battle.Box.Position, BattleBox.BaseZ),
            Scale = scale,
            Tint = Color.White,
            Rotation = Battle.Box.Rotation
        }, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.DentShader, PrepareDentShader));

        return true;
    }

    private static void PrepareDentShader(FrameContext frame, RenderPass pass)
    {
        Vector2 normalizedBoxBounds = (activeBoxSize + Vector2.One * 8f) / sourceSurface.Size;
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            NormalizedBoxBounds = new Vector4(normalizedBoxBounds, 0f, 0f)
        });
        pass.BindFragmentSamplerTextures(0,
        [
            new RenderPass.SamplerTexture(frame.Device.LinearWrap, sourceSurface),
            new RenderPass.SamplerTexture(frame.Device.LinearWrap, dentDistanceMap)
        ]);
    }
}
