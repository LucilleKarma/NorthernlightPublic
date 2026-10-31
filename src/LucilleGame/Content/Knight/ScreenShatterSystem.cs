
using System.Numerics;
using DelaunatorSharp;
using Northernlight.Core;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;
using SDL3;
using Triangle = Northernlight.Core.Mathematics.Triangle;

namespace Northernlight.Content.Knight;

/// <summary>
///     The system responsible for creating screen shatter
///     visuals.
/// </summary>
public sealed class ScreenShatterSystem
{
    public sealed class Shard
    {
        /// <summary>
        ///     How long it's been since this shard was created.
        /// </summary>
        public float Timer
        {
            get;
            set;
        }

        /// <summary>
        ///     How long this shard should exist for.
        /// </summary>
        public float Lifetime
        {
            get;
            set;
        }

        /// <summary>
        ///     The gravity factor of this shard.
        /// </summary>
        public float GravityFactor
        {
            get;
            set;
        } = 1f;

        /// <summary>
        ///     The UVs of this shard.
        /// </summary>
        public Triangle UVs
        {
            get;
            set;
        }

        /// <summary>
        ///     The base triangle offsets of this shard.
        /// </summary>
        public Triangle BaseOffset
        {
            get;
            set;
        }

        /// <summary>
        ///     The position of this shard.
        /// </summary>
        public Vector3 Position
        {
            get;
            set;
        }

        /// <summary>
        ///     The velocity of this shard.
        /// </summary>
        public Vector3 Velocity
        {
            get;
            set;
        }

        /// <summary>
        ///     The rotation of this shard.
        /// </summary>
        public Quaternion Rotation
        {
            get;
            set;
        } = Quaternion.Identity;
    }

    private Texture texture;

    private readonly List<Shard> shards = [];

    /// <summary>
    ///     The scene responsible for this system.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
    }

    public ScreenShatterSystem(KnightFightScene scene) => Scene = scene;

    /// <summary>
    ///     Updates this system.
    /// </summary>
    public void Update()
    {
        if (shards.Count <= 0)
            return;

        for (int i = 0; i < shards.Count; i++)
        {
            Shard shard = shards[i];
            shard.Position += shard.Velocity * Scene.Time.DeltaTime;
            shard.Velocity = Vector3.LerpDt(shard.Velocity, shard.Velocity * new Vector3(0f, 1f, 1f), 3f, Scene.Time);
            shard.Velocity += Vector3.UnitY * Scene.Time.DeltaTime * shard.GravityFactor * 2400f;

            float angularVelocity = Scene.Time.DeltaTime * 0.008f;
            shard.Rotation *= Quaternion.CreateFromYawPitchRoll(shard.Velocity.X * angularVelocity, shard.Velocity.Y * angularVelocity * 1.5f, shard.Velocity.Z * angularVelocity);
            shard.Timer += Scene.Time.DeltaTime;
        }

        shards.RemoveAll(s => s.Timer >= s.Lifetime);
    }

    /// <summary>
    ///     Prepares this system for rendering.
    /// </summary>
    public void PrepareRender(FrameContext context, RenderLayer layer)
    {
        if (shards.Count <= 0 || texture is null)
            return;

        MeshRenderer meshes = layer.OutputRenderer.Get<MeshRenderer>();
        foreach (Shard shard in shards)
        {
            Matrix4x4 world =
                Matrix4x4.CreateFromQuaternion(shard.Rotation) *
                Matrix4x4.CreateTranslation(shard.Position);

            Vector3 positionA = Vector3.Transform(shard.BaseOffset.A.AsVector3(), world);
            Vector3 positionB = Vector3.Transform(shard.BaseOffset.B.AsVector3(), world);
            Vector3 positionC = Vector3.Transform(shard.BaseOffset.C.AsVector3(), world);

            StandardVertex a = new StandardVertex(positionA, Vector4.One, Vector3.UnitZ, shard.UVs.A);
            StandardVertex b = new StandardVertex(positionB, Vector4.One, Vector3.UnitZ, shard.UVs.B);
            StandardVertex c = new StandardVertex(positionC, Vector4.One, Vector3.UnitZ, shard.UVs.C);
            OwnedMesh<StandardVertex> mesh = new OwnedMesh<StandardVertex>(GPUPrimitiveType.TriangleList, [a, b, c], [0, 1, 2], 1);
            meshes.EnqueueMesh(mesh, context.CommandBuffer, new BasicMeshSettings()
            {
                FragmentShader = Shaders.BasicFrag,
                VertexShader = Shaders.MeshVertexShader,
                PreparationAction = PreparePipeline
            });
        }
    }

    private void PreparePipeline(FrameContext frame, RenderPass pass, Camera camera)
    {
        pass.BindFragmentSamplerTextures(0, new RenderPass.SamplerTexture(frame.Device.PointClamp, texture));
        frame.CommandBuffer.BindVertexUniformData(0, camera.View * camera.Projection);
    }

    /// <summary>
    ///     Creates a collection of shatter shards.
    /// </summary>
    public void Shatter(Texture textureToCopy, CommandBuffer commandBuffer, int pointCount)
    {
        texture?.Dispose();
        texture = new Texture(new SDL.GPUTextureCreateInfo()
        {
            Width = textureToCopy.Width,
            Height = textureToCopy.Height,
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
        }, Scene.Device);

        textureToCopy.CopyTo(commandBuffer, texture);

        List<Vector2> shatterPoints = [];
        Vector2 screenSize = NorthernlightGame.KnightFightResolution;
        shatterPoints.Add(Vector2.Zero);
        shatterPoints.Add(Vector2.UnitX * screenSize);
        shatterPoints.Add(Vector2.UnitY * screenSize);
        shatterPoints.Add(screenSize);

        float midpoint = float.Sqrt(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            float xInterpolant = i % midpoint / midpoint;
            float yInterpolant = i / midpoint / midpoint;

            Vector2 p = new Vector2(xInterpolant * screenSize.X, yInterpolant * screenSize.Y) + Scene.Rng.NextCircular(100f);
            shatterPoints.Add(p);
        }

        Delaunator delaunator = new Delaunator(shatterPoints.Select(p => (IPoint)new Point(p.X, p.Y)).ToArray());

        for (int i = 0; i < delaunator.Triangles.Length; i += 3)
        {
            Vector2 a = shatterPoints[delaunator.Triangles[i]];
            Vector2 b = shatterPoints[delaunator.Triangles[i + 1]];
            Vector2 c = shatterPoints[delaunator.Triangles[i + 2]];
            Triangle triangle = new Triangle(a, b, c);

            Vector2 center = (triangle.A + triangle.B + triangle.C) * 0.3333f;
            shards.Add(new Shard()
            {
                Position = center.AsVector3(),
                BaseOffset = new Triangle(triangle.A - center, triangle.B - center, triangle.C - center),
                Rotation = Quaternion.Identity,
                Velocity = new Vector3((center.X - screenSize.X * 0.5f) * 4f, -1000f, 0f),
                Lifetime = 4.7f,
                UVs = new Triangle(triangle.A / screenSize, triangle.B / screenSize, triangle.C / screenSize),
                GravityFactor = Scene.Rng.NextFloat(1f, 3f)
            });
        }
    }
}
