using System.Numerics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class BackgroundSword : Bullet
{
    // A value of zero equates to being in the background, a
    // value of one equates to being in the foreground.
    private float relativeZ;

    private float existenceTimer;

    private readonly BackgroundBarrage attack;

    private readonly OwnedMesh<StandardVertex> mesh;

    private float AbsoluteZ
    {
        get => float.Lerp(BackgroundZ, ForegroundZ, relativeZ);
        set => relativeZ = float.InverseLerp(BackgroundZ, ForegroundZ, value);
    }

    /// <summary>
    ///     The general scale factor of this sword.
    /// </summary>
    public float ScaleFactor
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The 3D position of this sword.
    /// </summary>
    /// <remarks>
    ///     For ease of use, the Z position of this value
    ///     exists as a 0-1 interpolant.
    /// </remarks>
    public Vector3 Position3D
    {
        get => new Vector3(Position, relativeZ);
        set
        {
            Position = new Vector2(value.X, value.Y);
            relativeZ = float.Clamp(value.Z, 0f, 1f);
        }
    }

    /// <summary>
    ///     The forward direction of this sword.
    /// </summary>
    public Vector2 Forward => Vector2.Transform(Vector2.UnitX, Rotation);

    /// <summary>
    ///     This sword's starting rotation.
    /// </summary>
    public Quaternion StartingRotation
    {
        get;
        private set;
    }

    /// <summary>
    ///     This sword's rotation.
    /// </summary>
    public Quaternion Rotation
    {
        get;
        set;
    } = Quaternion.Identity;

    // These values simultaneously dictate layering
    // and perspective and parallax.
    /// <summary>
    ///     The Z position of the background.
    /// </summary>
    public static float BackgroundZ => 10f;

    /// <summary>
    ///     The Z position of the foreground.
    /// </summary>
    public static float ForegroundZ => 150f;

    public BackgroundSword(BackgroundBarrage attack, KnightFightScene scene, int damage, float grazePoints, Vector2 spawnPosition, Quaternion startingRotation) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        this.attack = attack;
        relativeZ = 0f;
        mesh = new OwnedMesh<StandardVertex>(GPUPrimitiveType.TriangleList, 4, 6, 2);
        mesh.Indices[0] = 0;
        mesh.Indices[1] = 1;
        mesh.Indices[2] = 2;
        mesh.Indices[3] = 2;
        mesh.Indices[4] = 1;
        mesh.Indices[5] = 3;
        StartingRotation = Rotation = startingRotation;
    }

    protected override void UpdateInternal()
    {
        existenceTimer += Scene.Time.DeltaTime;
    }

    private static void PreparePipeline(FrameContext frame, RenderPass pass, Camera camera)
    {
        pass.BindFragmentSamplerTextures(0, new RenderPass.SamplerTexture(frame.Device.PointClamp, Atlases.KnightScene.Atlas.Value.BackingTexture));
        frame.CommandBuffer.BindVertexUniformData(0, camera.View * camera.Projection);
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        Color color = Color.Lerp(new Color(239, 199, 219), Color.White, relativeZ);
        color *= float.Lerp(0.425f, 1f, relativeZ);

        Vector4 colorVector = color.AsVector4();
        AtlasEntry sword = Atlases.KnightScene.BlackSword;

        Vector2 uvTopLeft = new Vector2(sword.Area.Left, sword.Area.Top) / sword.BackingTexture.Size;
        Vector2 uvBottomRight = new Vector2(sword.Area.Right, sword.Area.Bottom) / sword.BackingTexture.Size;

        MeshRenderer meshes = Scene.BattleLayer.OutputRenderer.Get<MeshRenderer>();

        Vector2 scale = Vector2.One * float.Clamp(ScaleFactor / (4f - relativeZ * 3.5f), 0f, 1f);
        scale.Y *= float.InverseLerp(0f, 0.3f, existenceTimer);

        Vector3 area = new Vector3(sword.Area.Size, 0f) * new Vector3(scale, 1f);
        Vector3 halfArea = area * 0.5f;
        Vector3 origin = Position3D;
        origin.Z = AbsoluteZ;

        Vector3 normal = Vector3.Transform(Vector3.UnitZ, Rotation);

        // Top left.
        ref StandardVertex topLeft = ref mesh.Vertices[0];
        topLeft.Position = origin + Vector3.Transform(halfArea * new Vector3(-1f, -1f, 0f), Rotation);
        topLeft.Normal = normal;
        topLeft.Color = colorVector;
        topLeft.Uvw = new Vector3(uvTopLeft.X, uvTopLeft.Y, 0f);

        // Top right.
        ref StandardVertex topRight = ref mesh.Vertices[1];
        topRight.Position = origin + Vector3.Transform(halfArea * new Vector3(1f, -1f, 0f), Rotation);
        topRight.Normal = normal;
        topRight.Color = colorVector;
        topRight.Uvw = new Vector3(uvBottomRight.X, uvTopLeft.Y, 0f);

        // Bottom left.
        ref StandardVertex bottomLeft = ref mesh.Vertices[2];
        bottomLeft.Position = origin + Vector3.Transform(halfArea * new Vector3(-1f, 1f, 0f), Rotation);
        bottomLeft.Normal = normal;
        bottomLeft.Color = colorVector;
        bottomLeft.Uvw = new Vector3(uvTopLeft.X, uvBottomRight.Y, 0f);

        // Bottom right.
        ref StandardVertex bottomRight = ref mesh.Vertices[3];
        bottomRight.Position = origin + Vector3.Transform(halfArea * new Vector3(1f, 1f, 0f), Rotation);
        bottomRight.Normal = normal;
        bottomRight.Color = colorVector;
        bottomRight.Uvw = new Vector3(uvBottomRight.X, uvBottomRight.Y, 0f);

        meshes.EnqueueMesh(mesh, context.CommandBuffer, new BasicMeshSettings()
        {
            FragmentShader = Shaders.BasicFrag,
            VertexShader = Shaders.MeshVertexShader,
            PreparationAction = PreparePipeline
        });
    }

    public override void OnKill()
    {
        Scene.Audio.Play(new Sound(AudioBuffer.FromWav("BattleBoxBreakBurst.wav"), 0.5f));
        Scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightDaggerDash.wav"), 0.4f));

        Scene.AddScreenShake(6f);
        Scene.BattleHandler.Box.Position += Forward.RotatedBy(Scene.Rng.NextFloat(-0.2f, 0.2f)) * 20f;
        Scene.BattleHandler.Box.Rotation += Forward.X * Scene.Rng.NextFloat(0.3f, 1f) * 0.2f;

        Vector2 impactPoint = Scene.BattleHandler.Box.KeepInside(Position, Vector2.One);
        for (int i = 0; i < 4; i++)
        {
            float spawnProgress = i / 3f;
            float maxScale = float.Lerp(112f, 500f, float.Pow(spawnProgress, 0.65f));

            attack.Bullets.Add(new ShockwaveParticle(Scene, 0.1f, maxScale, impactPoint));
        }

        float averageShardCount = attack.stats.AverageImpactShardCount;
        int loopCount = (int)float.Ceiling(attack.stats.AverageImpactShardCount);
        float chanceOfFinalShard = averageShardCount % 1f;
        float baseShardSpeed = attack.stats.ImpactShardBaseSpeed;
        bool randomLastShard = float.Abs(loopCount - averageShardCount) >= 0.01f;
        for (int i = 0; i < loopCount; i++)
        {
            bool createShard = true;
            if (randomLastShard && i == loopCount - 1)
                createShard = Scene.Rng.NextBool(chanceOfFinalShard);

            if (!createShard)
                continue;

            float angularVariance = Scene.Rng.NextFloat(-1f, 1f);
            float theta = angularVariance * 0.67f;
            Vector2 shardVelocity = Forward.RotatedBy(theta) * (1f - float.Abs(angularVariance) * 0.25f) * Scene.Rng.NextFloat(0.7f, 1.2f) * baseShardSpeed;

            attack.Bullets.Add(new BattleBoxShard(Scene, impactPoint, shardVelocity, Damage, GrazePoints)
            {
                Scale = Scene.Rng.NextFloat(0.9f, 1.1f),
                Wiggles = true,
                Acceleration = 2000f
            });
        }

        Angle rotation = Scene.BattleHandler.Box.Position - Position - Scene.BattleHandler.Box.Rotation;
        attack.AddDent(rotation, 0.05f);
    }

    public override bool CollidingWith(RectangleF box)
    {
        return false;
    }
}
