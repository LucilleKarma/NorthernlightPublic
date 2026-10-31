using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of an SDL-TTF text
///     engine.
/// </summary>
public sealed unsafe class TextEngineQueue : IDisposable
{
    /// <summary>
    ///     Represents an instance of packed sprite data
    ///     that is to be sent to the GPU for vertex
    ///     creation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FontAtlasVertex
    {
        public Vector3 Position;

        public Vector2 UV;

        public Vector4 Color;
    }

    internal record TextKey(string TextString, Font Font);

    internal class TextData
    {
        public nint Handle
        {
            get;
            set;
        }

        public TextData(nint handle)
        {
            Handle = handle;
        }
    }

    private bool disposed;

    private Texture atlas;

    private VertexBuffer<FontAtlasVertex> vertexBuffer;

    private IndexBuffer indexBuffer;

    private TransferBuffer<FontAtlasVertex> vertexStaging;

    private TransferBuffer<FontAtlasVertex>.MapScope vertexStagingMap;

    private TransferBuffer<uint> indexStaging;

    private TransferBuffer<uint>.MapScope indexStagingMap;

    private readonly List<uint> indices = [];

    private readonly List<FontAtlasVertex> vertices = [];

    private readonly Dictionary<TextKey, TextData> textDataCache = [];

    /// <summary>
    ///     The handle of this engine to its underlying
    ///     GPU resource.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The current capacity of this engine.
    /// </summary>
    public uint Capacity
    {
        get;
        private set;
    }

    /// <summary>
    ///     The graphics device that owns this
    ///     engine.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    /// <summary>
    ///     This engine's font to render.
    /// </summary>
    public Font Font
    {
        get;
    }

    /// <summary>
    ///     The graphics pipeline used for rendering
    ///     this engine. 
    /// </summary>
    public GraphicsPipeline Pipeline
    {
        get;
    }

    /// <summary>
    ///     The sampler to render the engine with.
    /// </summary>
    public Sampler Sampler
    {
        get;
    }

    public TextEngineQueue(GraphicsDevice device, Font font, SpriteQueueParameters parameters)
    {
        Capacity = 256;

        Font = font;
        Sampler = parameters.Sampler;
        Handle = TTF.CreateGPUTextEngine(device.Handle);
        Device = device;

        using CommandBuffer commandBuffer = device.GenerateCommandBuffer();
        InitializeVertexBuffers(commandBuffer);

        Shader fragmentShader = parameters.OverridingFragmentShader ?? Shaders.BasicFrag;
        GPUVertexBufferDescription vertexDescription = GPUVertexBufferDescription.From<FontAtlasVertex>(SDL.GPUVertexInputRate.Vertex);
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

        VertexAttributes attributes = new VertexAttributes([colorDescription], [vertexDescription]);
        Pipeline = new GraphicsPipeline(Device, Shaders.FontVertexShader, fragmentShader, new GraphicsPipelineParameters()
        {
            VertexAttributes = attributes,
            Topology = GPUPrimitiveType.TriangleList,
            DepthStencil = Device.DepthBufferFormat == 0 ? DepthStencilState.None : DepthStencilState.Default,
            DepthBufferFormat = Device.DepthBufferFormat,
        });
    }

    private void InitializeVertexBuffers(CommandBuffer commandBuffer)
    {
        vertexBuffer?.Dispose();
        vertexBuffer = new VertexBuffer<FontAtlasVertex>(Device, commandBuffer, new FontAtlasVertex[Capacity * 4]);
        vertexStaging?.Dispose();
        vertexStaging = new TransferBuffer<FontAtlasVertex>(Device, SDL.GPUTransferBufferUsage.Upload, Capacity * (uint)sizeof(FontAtlasVertex) * 4);
        vertexStagingMap.Dispose();
        vertexStagingMap = vertexStaging.Map();

        indexBuffer?.Dispose();
        indexBuffer = new IndexBuffer(Device, commandBuffer, new uint[Capacity * 6]);
        indexStaging?.Dispose();
        indexStaging = new TransferBuffer<uint>(Device, SDL.GPUTransferBufferUsage.Upload, Capacity * (uint)sizeof(uint) * 6);
        indexStagingMap.Dispose();
        indexStagingMap = indexStaging.Map();
    }

    internal TextData GetOrCreateTextData(TextKey key)
    {
        TextData data;
        if (textDataCache.TryGetValue(key, out TextData? d))
            data = d;
        else
        {
            nint handle = TTF.CreateText(Handle, Font.Handle, key.TextString, 0);
            textDataCache[key] = data = new TextData(handle);
        }
        return data;
    }

    /// <summary>
    ///     Returns the size of a given string for
    ///     this engine, in pixels.
    /// </summary>
    public Vector2 MeasureString(string text)
    {
        TextKey key = new TextKey(text, Font);
        nint handle = GetOrCreateTextData(key).Handle;
        TTF.GetTextSize(handle, out int width, out int height);

        return new Vector2(width, height);
    }

    /// <summary>
    ///     Enqueues text for rendering on the screen.
    /// </summary>
    public void Enqueue(string text, Vector3 position, Color color, Vector2 scale, Angle rotation)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text))
            return;

        TextKey key = new TextKey(text, Font);
        TextData data = GetOrCreateTextData(key);

        TTF.GPUAtlasDrawSequence* sequence = (TTF.GPUAtlasDrawSequence*)TTF.GetGPUTextDrawData(textDataCache[key].Handle);
        if (sequence is null)
            return;

        uint baseVertex = (uint)vertices.Count;
        for (int i = 0; i < sequence->NumVertices; i++)
        {
            Vector2 vertexPosition = ((Vector2*)sequence->XY)[i];
            vertexPosition.Y *= -1f;

            if (rotation != 0f)
                vertexPosition = Vector2.Transform(vertexPosition, Matrix3x2.CreateRotation(rotation));

            Vector2 uv = ((Vector2*)sequence->UV)[i];
            vertices.Add(new FontAtlasVertex()
            {
                Position = new Vector3(vertexPosition * scale, 0f) + position,
                UV = uv,
                Color = color.AsVector4()
            });
        }

        for (int i = 0; i < sequence->NumIndices; i++)
        {
            uint index = ((uint*)sequence->Indices)[i] + baseVertex;
            indices.Add(index);
        }
    }

    /// <summary>
    ///     Prepares this engine for rendering.
    /// </summary>
    public void Prepare(FrameContext frame)
    {
        if (disposed)
            return;

        int vertexCount = vertices.Count;
        int indexCount = indices.Count;
        if (vertexCount == 0 || indexCount == 0)
            return;

        int instanceCount = vertexCount / 4;
        if (instanceCount > Capacity)
        {
            Capacity = BitOperations.RoundUpToPowerOf2((uint)instanceCount);
            InitializeVertexBuffers(frame.CommandBuffer);
        }

        int i = 0;
        FontAtlasVertex* vertexPtr = (FontAtlasVertex*)vertexStagingMap.TransferHandle;
        foreach (FontAtlasVertex vertex in vertices)
        {
            vertexPtr[i] = vertex;
            i++;
        }

        i = 0;
        uint* indexPtr = (uint*)indexStagingMap.TransferHandle;
        foreach (uint index in indices)
        {
            indexPtr[i] = index;
            i++;
        }

        using var copyScope = frame.CommandBuffer.Copy();
        copyScope.UploadToGPUBuffer(vertexStaging, vertexBuffer, (uint)(vertexCount * sizeof(FontAtlasVertex)), true);
        copyScope.UploadToGPUBuffer(indexStaging, indexBuffer, (uint)(indexCount * sizeof(uint)), true);

        while (textDataCache.Count >= 128)
        {
            var kv = textDataCache.First();
            TTF.DestroyText(kv.Value.Handle);

            textDataCache.Remove(kv.Key);
        }
    }

    /// <summary>
    ///     Renders the contents of this engine.
    /// </summary>
    public void Render(FrameContext frame, RenderPass pass, Camera camera)
    {
        if (disposed)
            return;

        int vertexCount = vertices.Count;
        int indexCount = indices.Count;
        if (vertexCount == 0 || indexCount == 0 || textDataCache.Count <= 0)
            return;

        Pipeline.BindToPass(pass);

        if (atlas is null)
        {
            TTF.GPUAtlasDrawSequence* sequence = (TTF.GPUAtlasDrawSequence*)TTF.GetGPUTextDrawData(textDataCache.First(t => !string.IsNullOrEmpty(t.Key.TextString)).Value.Handle);
            if (sequence is null)
                throw new Exception(SDL.GetError());

            atlas = new Texture(sequence->AtlasTexture, 1, 1, Device, SDL.GPUTextureFormat.R8G8B8A8Unorm);
        }

        pass.BindFragmentSamplerTextures(0, new RenderPass.SamplerTexture(Sampler, atlas));
        frame.CommandBuffer.BindVertexUniformData(0, camera.View * camera.Projection);

        SDL.GPUBufferBinding[] sdlBindings = new SDL.GPUBufferBinding[1]
        {
            new SDL.GPUBufferBinding()
            {
                Buffer = vertexBuffer.Handle,
                Offset = 0
            }
        };

        pass.BindVertexBuffers(0, sdlBindings);
        pass.BindIndexBuffer(indexBuffer);
        pass.RenderIndexed((uint)indices.Count, 1);

        vertices.Clear();
        indices.Clear();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        foreach (TextData data in textDataCache.Values)
            TTF.DestroyText(data.Handle);

        GC.SuppressFinalize(this);
        TTF.DestroyGPUTextEngine(Handle);

        indexStagingMap.Dispose();
        indexStaging.Dispose();
        indexBuffer.Dispose();

        vertexStagingMap.Dispose();
        vertexStaging.Dispose();
        vertexBuffer.Dispose();
    }
}
