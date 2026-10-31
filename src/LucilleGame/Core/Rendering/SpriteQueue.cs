using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of a sprite queue, which
///     handles the preprocessing of sprite data for
///     ultimate rendering.
/// </summary>
public sealed class SpriteQueue : IDisposable
{
    private bool disposed;

    private MeshStream<SpriteQuadVertex> mesh;

    private VertexBuffer<SpriteInstance> instanceBuffer;

    private TransferBuffer<SpriteInstance> instanceStaging;

    private TransferBuffer<SpriteInstance>.MapScope instanceStagingMap;

    private readonly ConcurrentBag<SpriteInstance> instances = [];

    private readonly Action<FrameContext, RenderPass>? extraShaderPreparations;

    private uint spriteCountCache;

    /// <summary>
    ///     The amount of sprites currently contained
    ///     within this queue.
    /// </summary>
    public int ActiveSpriteCount => instances.Count;

    /// <summary>
    ///     The graphics device that owns this
    ///     queue.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    /// <summary>
    ///     The window that this queue
    ///     is attached to.
    /// </summary>
    public Window Window
    {
        get;
    }

    /// <summary>
    ///     The current capacity of this queue.
    /// </summary>
    public uint Capacity
    {
        get;
        private set;
    }

    /// <summary>
    ///     The texture to render the queue with.
    /// </summary>
    public Texture Texture
    {
        get;
    }

    /// <summary>
    ///     The sampler to render the queue with.
    /// </summary>
    public Sampler Sampler
    {
        get;
    }

    /// <summary>
    ///     The blend state used to render this queue.
    /// </summary>
    public BlendState BlendState
    {
        get;
    }

    /// <summary>
    ///     The graphics pipeline used for rendering
    ///     this queue. 
    /// </summary>
    public GraphicsPipeline Pipeline
    {
        get;
    }

    public SpriteQueue(GraphicsDevice device, Window window, Texture texture, SpriteQueueParameters parameters, Action<FrameContext, RenderPass>? extraShaderPreparations = null)
    {
        Sampler = parameters.Sampler;
        Texture = texture;
        Window = window;
        BlendState = parameters.BlendState;

        Device = device;
        Capacity = parameters.InitialCapacity;

        using CommandBuffer commandBuffer = device.GenerateCommandBuffer();
        if (instanceBuffer is null)
            InitializeInstanceBuffers(commandBuffer);
        if (mesh is null)
            InitializeMesh(commandBuffer);

        GPUVertexBufferDescription quadDescription = GPUVertexBufferDescription.From<SpriteQuadVertex>(SDL.GPUVertexInputRate.Vertex);
        GPUVertexBufferDescription instanceDescription = GPUVertexBufferDescription.From<SpriteInstance>(SDL.GPUVertexInputRate.Instance) with
        {
            Slot = 1
        };

        SDL.GPUColorTargetDescription colorDescription = parameters.ColorTargetDescription;
        colorDescription.BlendState = new SDL.GPUColorTargetBlendState()
        {
            EnableBlend = parameters.BlendState.Enabled,
            ColorBlendOp = parameters.BlendState.ColorBlendOperation,
            AlphaBlendOp = parameters.BlendState.AlphaBlendOperation,
            SrcColorBlendFactor = parameters.BlendState.SourceColorBlendFactor,
            DstColorBlendFactor = parameters.BlendState.DestinationColorBlendFactor,
            SrcAlphaBlendFactor = parameters.BlendState.SourceAlphaBlendFactor,
            DstAlphaBlendFactor = parameters.BlendState.DestinationAlphaBlendFactor
        };

        VertexAttributes attributes = new VertexAttributes([colorDescription], [quadDescription, instanceDescription]);

        Shader fragmentShader = parameters.OverridingFragmentShader ?? Shaders.BasicFrag;
        Pipeline = new GraphicsPipeline(Device, Shaders.SpriteVertexShader, fragmentShader, new GraphicsPipelineParameters()
        {
            VertexAttributes = attributes,
            Topology = GPUPrimitiveType.TriangleList,
            DepthStencil = Device.DepthBufferFormat == 0 ? DepthStencilState.None : parameters.DepthStencilState,
            DepthBufferFormat = Device.DepthBufferFormat,
        });

        this.extraShaderPreparations = extraShaderPreparations;
    }

    private void InitializeMesh(CommandBuffer commandBuffer)
    {
        SpriteQuadVertex[] vertices =
        [
            new SpriteQuadVertex(-0.5f, -0.5f),
            new SpriteQuadVertex(0.5f, -0.5f),
            new SpriteQuadVertex(-0.5f, 0.5f),
            new SpriteQuadVertex(0.5f, 0.5f),
        ];
        uint[] indices =
        [
            0, 1, 2,
            2, 1, 3
        ];
        mesh = new MeshStream<SpriteQuadVertex>(Device, commandBuffer, vertices, indices);
    }

    private void InitializeInstanceBuffers(CommandBuffer commandBuffer)
    {
        instanceBuffer?.Dispose();
        instanceBuffer = new VertexBuffer<SpriteInstance>(Device, commandBuffer, new SpriteInstance[Capacity]);

        instanceStaging?.Dispose();
        instanceStaging = new TransferBuffer<SpriteInstance>(Device, SDL.GPUTransferBufferUsage.Upload, Capacity * (uint)Unsafe.SizeOf<SpriteInstance>());

        instanceStagingMap.Dispose();
        instanceStagingMap = instanceStaging.Map();
    }

    /// <summary>
    ///     Enqueues a sprite to the queue.
    /// </summary>
    public void Enqueue(in SpriteData sprite)
    {
        Vector2 inverseTextureSize = Vector2.One / Texture.Size;
        Vector2 normalizedTopLeft = new Vector2(sprite.Frame.Left, sprite.Frame.Top) * inverseTextureSize;
        Vector2 normalizedBottomRight = new Vector2(sprite.Frame.Right, sprite.Frame.Bottom) * inverseTextureSize;
        Vector2 normalizedOrigin = sprite.Origin / sprite.Frame.Size;
        Vector2 size = sprite.Frame.Size * sprite.Scale;
        Color color = sprite.Tint;

        instances.Add(new SpriteInstance()
        {
            PositionXYSizeXY = new Vector4(sprite.Position.X, sprite.Position.Y, size.X, size.Y),
            UVRectangle = new Vector4(normalizedTopLeft.X, normalizedTopLeft.Y, normalizedBottomRight.X, normalizedBottomRight.Y),
            Color = color.AsVector4(),
            RotationOriginPositionZ = new Vector4(sprite.Rotation, normalizedOrigin.X, normalizedOrigin.Y, sprite.Position.Z + instances.Count * 0.01f)
        });
    }

    /// <summary>
    ///     Processes the contents of this queue.
    /// </summary>
    public unsafe void Prepare(FrameContext frame)
    {
        if (disposed || ActiveSpriteCount <= 0)
            return;

        if (ActiveSpriteCount > Capacity)
        {
            Capacity = BitOperations.RoundUpToPowerOf2((uint)ActiveSpriteCount);
            InitializeInstanceBuffers(frame.CommandBuffer);
        }

        spriteCountCache = (uint)ActiveSpriteCount;

        int i = 0;
        SpriteInstance* ptr = (SpriteInstance*)instanceStagingMap.TransferHandle;
        foreach (SpriteInstance instance in instances)
        {
            ptr[i] = instance;
            i++;
        }

        using var copyScope = frame.CommandBuffer.Copy();
        copyScope.UploadToGPUBuffer(instanceStaging, instanceBuffer, (uint)(spriteCountCache * sizeof(SpriteInstance)), true);
    }

    /// <summary>
    ///     Renders this queue.
    /// </summary>
    public void Render(FrameContext frame, RenderPass pass, Camera camera, bool flush = true)
    {
        if (disposed || spriteCountCache <= 0)
            return;

        Pipeline.BindToPass(pass);

        pass.BindFragmentSamplerTextures(0, new RenderPass.SamplerTexture(Sampler, Texture));
        frame.CommandBuffer.BindVertexUniformData(0, camera.View * camera.Projection);
        extraShaderPreparations?.Invoke(frame, pass);

        SDL.GPUBufferBinding[] sdlBindings = new SDL.GPUBufferBinding[2]
        {
            new SDL.GPUBufferBinding()
            {
                Buffer = mesh.VertexBuffer.Handle,
                Offset = 0
            },
            new SDL.GPUBufferBinding()
            {
                Buffer = instanceBuffer.Handle,
                Offset = 0
            }
        };

        pass.BindVertexBuffers(0, sdlBindings);
        pass.BindIndexBuffer(mesh.IndexBuffer);
        pass.RenderIndexed(6, spriteCountCache);

        if (flush)
            Flush();
    }

    /// <summary>
    ///     Flushes the contents of this queue.
    /// </summary>
    public void Flush()
    {
        instances.Clear();
        spriteCountCache = 0;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        mesh.Dispose();
        instanceBuffer.Dispose();
        instanceStagingMap.Dispose();
        instanceStaging.Dispose();
        Pipeline.Dispose();
    }
}
