using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using Northernlight.Core.Rendering;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a mesh renderer.
/// </summary>
public sealed class MeshRenderer : IRenderSubsystem, IDisposable
{
    public record class MeshWithSettings(ICpuMesh<StandardVertex> Mesh, IMeshSettings Settings);

    public sealed class MeshCache : IDisposable
    {
        public readonly ConcurrentQueue<MeshWithSettings> Meshes;

        public readonly MeshStream<StandardVertex> Mesh;

        public readonly TransferBuffer<StandardVertex> VertexTransferBuffer;

        public readonly TransferBuffer<uint> IndexTransferBuffer;

        public readonly GraphicsPipeline Pipeline;

        public MeshCache(MeshStream<StandardVertex> mesh, TransferBuffer<StandardVertex> vertexTransfer, TransferBuffer<uint> indexTransfer, GraphicsPipeline pipeline)
        {
            Meshes = [];
            Mesh = mesh;
            VertexTransferBuffer = vertexTransfer;
            IndexTransferBuffer = indexTransfer;
            Pipeline = pipeline;
        }

        public void Dispose()
        {
            Pipeline.Dispose();
            VertexTransferBuffer.Dispose();
            IndexTransferBuffer.Dispose();
            Mesh.Dispose();
            Meshes.Clear();
        }
    }

    /// <summary>
    ///     Represents an instance of a cacheable mesh's key.
    /// </summary>
    public record MeshKey(Shader VertexShader, Shader FragmentShader);

    private bool disposed;

    private readonly ConcurrentDictionary<MeshKey, MeshCache> meshCache = [];

    /// <summary>
    ///     The amount of vertices supported in a single
    ///     batched draw call by this renderer.
    /// </summary>
    public uint VertexCapacity
    {
        get;
    }

    /// <summary>
    ///     The amount of indices supported in a single
    ///     batched draw call by this renderer.
    /// </summary>
    public uint IndexCapacity
    {
        get;
    }

    /// <summary>
    ///     The graphics device that owns this renderer.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
        private set;
    }

    /// <summary>
    ///     The window that owns this renderer.
    /// </summary>
    public Window Window
    {
        get;
        private set;
    }

    public MeshRenderer(GraphicsDevice device, Window window, uint vertexCapacity = 8192 * 2, uint indexCapacity = 8192 * 6)
    {
        Device = device;
        Window = window;
        VertexCapacity = vertexCapacity;
        IndexCapacity = indexCapacity;
    }

    /// <summary>
    ///     Enqueues a trail to this system for future rendering.
    /// </summary>
    public void EnqueueTrail<TTopology>(ReadOnlySpan<Vector3> controlPoints, CommandBuffer commandBuffer, TrailSettings<TTopology> settings) where TTopology : ITrailTopologySetting =>
        EnqueueMesh(TrailBuilder.BuildOwnedMesh(controlPoints, settings), commandBuffer, settings);

    /// <summary>
    ///     Enqueues a mesh to this system for future rendering.
    /// </summary>
    public void EnqueueMesh(ICpuMesh<StandardVertex> mesh, CommandBuffer commandBuffer, IMeshSettings settings)
    {
        MeshKey key = new MeshKey(settings.VertexShader, settings.FragmentShader);

        if (!meshCache.TryGetValue(key, out MeshCache? value))
        {
            MeshStream<StandardVertex> meshGpu = new MeshStream<StandardVertex>(
                Device,
                commandBuffer,
                vertices: new StandardVertex[VertexCapacity],
                indices: new uint[IndexCapacity]);

            TransferBuffer<StandardVertex> vertexTransferBuffer = new TransferBuffer<StandardVertex>(
                Device,
                SDL.GPUTransferBufferUsage.Upload,
                (uint)(Unsafe.SizeOf<StandardVertex>() * VertexCapacity));

            TransferBuffer<uint> indexTransferBuffer = new TransferBuffer<uint>(
                Device,
                SDL.GPUTransferBufferUsage.Upload,
                sizeof(uint) * IndexCapacity);

            GPUVertexBufferDescription quadDescription = GPUVertexBufferDescription.From<StandardVertex>(SDL.GPUVertexInputRate.Vertex);
            SDL.GPUColorTargetDescription colorDescription = Device.GetSwapchainColorTargetDescription(Window);
            colorDescription.BlendState = new SDL.GPUColorTargetBlendState()
            {
                EnableBlend = settings.BlendState.Enabled,
                ColorBlendOp = settings.BlendState.ColorBlendOperation,
                AlphaBlendOp = settings.BlendState.AlphaBlendOperation,
                SrcColorBlendFactor = settings.BlendState.SourceColorBlendFactor,
                DstColorBlendFactor = settings.BlendState.DestinationColorBlendFactor,
                SrcAlphaBlendFactor = settings.BlendState.SourceAlphaBlendFactor,
                DstAlphaBlendFactor = settings.BlendState.DestinationAlphaBlendFactor
            };

            VertexAttributes attributes = new VertexAttributes([colorDescription], [quadDescription]);
            GraphicsPipeline pipeline = new GraphicsPipeline(Device, key.VertexShader, key.FragmentShader, new GraphicsPipelineParameters()
            {
                VertexAttributes = attributes,
                Topology = GPUPrimitiveType.TriangleList,
                DepthStencil = Device.DepthBufferFormat == 0 ? DepthStencilState.None : DepthStencilState.Default,
                DepthBufferFormat = Device.DepthBufferFormat,
            });

            value = new MeshCache(meshGpu, vertexTransferBuffer, indexTransferBuffer, pipeline);
            meshCache[key] = value;
        }

        value.Meshes.Enqueue(new MeshWithSettings(mesh, settings));
    }

    private unsafe void UploadDataToTransferBuffers(MeshCache cache, out uint vertexOffset, out uint indexOffset)
    {
        using var vertexMap = cache.VertexTransferBuffer.Map();
        using var indexMap = cache.IndexTransferBuffer.Map();

        StandardVertex* vertexPtr = (StandardVertex*)vertexMap.TransferHandle;
        uint* indexPtr = (uint*)indexMap.TransferHandle;

        vertexOffset = 0;
        indexOffset = 0;

        foreach (var meshSettings in cache.Meshes)
        {
            ICpuMesh<StandardVertex> mesh = meshSettings.Mesh;
            uint vertexCount = (uint)mesh.Vertices.Length;
            uint indexCount = (uint)mesh.Indices.Length;

            if (vertexOffset + vertexCount > VertexCapacity)
                throw new InvalidOperationException("Vertex batch overflow.");
            if (indexOffset + indexCount > IndexCapacity)
                throw new InvalidOperationException("Index batch overflow.");

            fixed (StandardVertex* trailVerticesPtr = &mesh.Vertices.GetPinnableReference())
                Buffer.MemoryCopy(trailVerticesPtr, vertexPtr + vertexOffset, mesh.Vertices.Length * sizeof(StandardVertex), vertexCount * sizeof(StandardVertex));

            fixed (uint* trailIndicesPtr = &mesh.Indices.GetPinnableReference())
                Buffer.MemoryCopy(trailIndicesPtr, indexPtr + indexOffset, mesh.Indices.Length * sizeof(uint), indexCount * sizeof(uint));

            // Ensure that all index data is offset
            // from relative to absolute indices by
            // adding on the moving vertex start.
            Vector<uint> offsetVector = new Vector<uint>(vertexOffset);
            int simdSpacing = Vector<uint>.Count;
            int simdCount = (int)(indexCount - indexCount % simdSpacing);
            for (int i = 0; i < simdCount; i += simdSpacing)
            {
                Vector<uint> sourceVector = new Vector<uint>(mesh.Indices.Slice(i, simdSpacing));
                (sourceVector + offsetVector).CopyTo(new Span<uint>(indexPtr + indexOffset + i, simdSpacing));
            }

            for (int i = simdCount; i < indexCount; i++)
                indexPtr[indexOffset + i] += vertexOffset;

            vertexOffset += vertexCount;
            indexOffset += indexCount;
        }
    }

    public void Prepare(FrameContext frame)
    {
        uint vertexSize = (uint)Unsafe.SizeOf<StandardVertex>();
        uint indexSize = sizeof(uint);

        foreach (var kv in meshCache)
        {
            MeshCache cache = kv.Value;
            if (cache.Meshes.IsEmpty)
                continue;

            UploadDataToTransferBuffers(cache, out uint vertexOffset, out uint indexOffset);

            using var copyScope = frame.CommandBuffer.Copy();

            uint vertexBytes = vertexOffset * vertexSize;
            uint indexBytes = indexOffset * indexSize;

            if (vertexBytes >= 1)
                copyScope.UploadToGPUBuffer(cache.VertexTransferBuffer, cache.Mesh.VertexBuffer, vertexBytes, true);
            if (indexBytes >= 1)
                copyScope.UploadToGPUBuffer(cache.IndexTransferBuffer, cache.Mesh.IndexBuffer, indexBytes, true);
        }
    }

    public void Render(FrameContext frame, RenderPass pass, Camera camera)
    {
        foreach (var kv in meshCache)
        {
            MeshCache cache = kv.Value;
            if (!cache.Meshes.TryPeek(out var firstMesh))
                continue;

            cache.Pipeline.BindToPass(pass);
            firstMesh.Settings.PreparationAction(frame, pass, camera);

            uint totalIndexCount = 0;
            foreach (var trail in cache.Meshes)
                totalIndexCount += (uint)trail.Mesh.Indices.Length;

            if (totalIndexCount > 0)
                cache.Mesh.Render(pass, totalIndexCount, 1);

            cache.Meshes.Clear();
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        foreach (var kv in meshCache)
        {
            kv.Value.Pipeline.Dispose();
            kv.Value.VertexTransferBuffer.Dispose();
            kv.Value.IndexTransferBuffer.Dispose();
            kv.Value.Mesh.Dispose();
            kv.Value.Meshes.Clear();
        }
        meshCache.Clear();
    }
}
