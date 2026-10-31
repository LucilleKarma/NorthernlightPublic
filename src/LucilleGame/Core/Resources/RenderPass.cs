using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a render pass.
/// </summary>
public sealed class RenderPass : IDisposable
{
    private static bool anyActive;

    [StructLayout(LayoutKind.Sequential)]
    public struct PassTargetInfo
    {
        internal nint TextureHandle;

        /// <summary>
        ///     The mip level to use as a color target.
        /// </summary>
        public uint MipLevel;

        /// <summary>
        ///     The layer index or depth plane to use as a color target.
        ///     This value is treated as a layer index on 2D array and cube textures, and
        ///     as a depth plane on 3D textures.
        /// </summary>
        public uint LayerOrDepthPlane;

        /// <summary>
        ///     The color to clear the color target to at the start of the render pass.
        ///     Ignored if public GPU_LOADOP_CLEAR is not used.
        /// </summary>
        public SDL.FColor ClearColor;

        /// <summary>
        ///     What is done with the contents of the color target at the beginning of the render pass.
        /// </summary>
        public SDL.GPULoadOp LoadOp;

        /// <summary>
        ///     What is done with the results of the render pass.
        /// </summary>
        public SDL.GPUStoreOp StoreOp;

        internal nint ResolveTextureHandle;

        /// <summary>
        ///     The mip level of the resolve texture to use for the resolve operation.
        ///     Ignored if a RESOLVE* store_op is not used.
        /// </summary>
        public uint ResolveMipLevel;

        /// <summary>
        ///     The layer index of the resolve texture to use for the resolve operation.
        ///     Ignored if a RESOLVE* store_op is not used.
        /// </summary>
        public uint ResolveLayer;

        private byte cycle;

        private byte cycleResolveTexture;

        private readonly byte padding1;

        private readonly byte padding2;

        /// <summary>
        ///     The texture that will be used as a color target by a render pass.
        /// </summary>
        public Texture Texture
        {
            set => TextureHandle = value.Handle;
        }

        /// <summary>
        ///     The texture that will receive the results of a multisample resolve operation.
        ///     Ignored if a RESOLVE* store_op is not used.
        /// </summary>
        public Texture ResolveTexture
        {
            set => ResolveTextureHandle = value.Handle;
        }

        /// <summary>
        ///     True cycles the texture if the texture is bound and load_op is not LOAD.
        /// </summary>
        public bool Cycle
        {
            readonly get => cycle != 0;
            set => cycle = value ? (byte)1 : (byte)0;
        }

        /// <summary>
        ///     True cycles the resolve texture if the resolve texture is bound.
        ///     Ignored if a RESOLVE* store_op is not used.
        /// </summary>
        public bool CycleResolveTexture
        {
            readonly get => cycleResolveTexture != 0;
            set => cycleResolveTexture = value ? (byte)1 : (byte)0;
        }
    }

    public readonly record struct VertexBufferBinding<T>(VertexBuffer<T> Buffer, uint Offset) where T : unmanaged;

    public readonly record struct SamplerTexture(Sampler Sampler, Texture Texture)
    {
        public SamplerTexture(Sampler sampler, AtlasEntry texture) : this(sampler, texture.BackingTexture)
        {
        }
    }

    private bool disposed;

    /// <summary>
    ///     The handle associated with this
    ///     render pass.
    /// </summary>
    internal nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The height of this pass' viewport.
    /// </summary>
    public uint ViewportWidth
    {
        get;
    }

    /// <summary>
    ///     The height of this pass' viewport.
    /// </summary>
    public uint ViewportHeight
    {
        get;
    }

    /// <summary>
    ///     The depth stencil info of this pass.
    /// </summary>
    public SDL.GPUDepthStencilTargetInfo DepthStencilInfo
    {
        get;
        private set;
    }

    public RenderPass(CommandBuffer commandBuffer, Texture backingTexture, PassTargetInfo passTargetInfo, SDL.GPUDepthStencilTargetInfo depthStencilInfo = default)
    {
        if (anyActive)
            throw new InvalidOperationException("A second render pass cannot be started when one is already in use.");
        if (backingTexture.Width == 0 || backingTexture.Height == 0)
            throw new InvalidOperationException("Zero-sized backing textures are not allowed for render passes.");
        if (backingTexture.Handle != passTargetInfo.TextureHandle)
            throw new InvalidOperationException("Texture handles are misaligned.");

        ViewportWidth = backingTexture.Width;
        ViewportHeight = backingTexture.Height;

        SDL.GPUColorTargetInfo targetInfo = new SDL.GPUColorTargetInfo()
        {
            Texture = passTargetInfo.TextureHandle,
            MipLevel = passTargetInfo.MipLevel,
            LayerOrDepthPlane = passTargetInfo.LayerOrDepthPlane,
            ClearColor = passTargetInfo.ClearColor,
            LoadOp = passTargetInfo.LoadOp,
            StoreOp = passTargetInfo.StoreOp,
            ResolveTexture = passTargetInfo.ResolveTextureHandle,
            ResolveLayer = passTargetInfo.ResolveLayer,
            ResolveMipLevel = passTargetInfo.ResolveMipLevel,
            Cycle = passTargetInfo.Cycle,
            CycleResolveTexture = passTargetInfo.CycleResolveTexture
        };

        DepthStencilInfo = depthStencilInfo;
        if (DepthStencilInfo.Texture != nint.Zero)
            Handle = SDL.BeginGPURenderPass(commandBuffer.Handle, [targetInfo], 1, DepthStencilInfo);
        else
            Handle = SDL.BeginGPURenderPass(commandBuffer.Handle, [targetInfo], 1, nint.Zero);

        anyActive = true;
    }

    /// <summary>
    ///     Binds vertex buffers to this 
    ///     render pass.
    /// </summary>
    public void BindVertexBuffers<T>(uint firstSlot, params VertexBuffer<T>[] buffers) where T : unmanaged
    {
        SDL.GPUBufferBinding[] bufferBindings = new SDL.GPUBufferBinding[buffers.Length];

        uint offset = 0;
        for (int i = 0; i < bufferBindings.Length; i++)
        {
            bufferBindings[i] = new SDL.GPUBufferBinding()
            {
                Buffer = buffers[i].Handle,
                Offset = offset
            };
            offset += (uint)Unsafe.SizeOf<T>();
        }

        SDL.BindGPUVertexBuffers(Handle, firstSlot, bufferBindings, (uint)bufferBindings.Length);
    }

    /// <summary>
    ///     Binds vertex buffers to this 
    ///     render pass.
    /// </summary>
    public void BindVertexBuffers(uint firstSlot, params SDL.GPUBufferBinding[] bufferBindings)
    {
        SDL.BindGPUVertexBuffers(Handle, firstSlot, bufferBindings, (uint)bufferBindings.Length);
    }

    /// <summary>
    ///     Binds an index buffer to this 
    ///     render pass.
    /// </summary>
    public void BindIndexBuffer(IndexBuffer buffer)
    {
        SDL.BindGPUIndexBuffer(Handle, new SDL.GPUBufferBinding()
        {
            Buffer = buffer.Handle,
            Offset = 0
        }, buffer.SizeFormat);
    }

    /// <summary>
    ///     Binds texture sampler bindings to this 
    ///     render pass.
    /// </summary>
    public void BindFragmentSamplerTextures(uint firstSlot, params SamplerTexture[] samplerTextures)
    {
        SDL.GPUTextureSamplerBinding[] sdlBindings = new SDL.GPUTextureSamplerBinding[samplerTextures.Length];
        for (int i = 0; i < sdlBindings.Length; i++)
        {
            sdlBindings[i] = new SDL.GPUTextureSamplerBinding()
            {
                Sampler = samplerTextures[i].Sampler.Handle,
                Texture = samplerTextures[i].Texture.Handle
            };
        }

        SDL.BindGPUFragmentSamplers(Handle, firstSlot, sdlBindings, (uint)samplerTextures.Length);
    }

    /// <summary>
    ///     Sets the viewport for this pass.
    /// </summary>
    public void SetViewport(Rectangle viewportArea)
    {
        SDL.SetGPUViewport(Handle, new SDL.GPUViewport()
        {
            X = viewportArea.X,
            Y = viewportArea.Y,
            W = viewportArea.Width,
            H = viewportArea.Height
        });
    }

    /// <summary>
    ///     Renders the contents of this render
    ///     pass.
    /// </summary>
    public void Render(uint vertexCount, uint instanceCount, uint firstVertex = 0, uint firstInstance = 0)
    {
        SDL.DrawGPUPrimitives(Handle, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    /// <summary>
    ///     Renders the contents of this render
    ///     pass with an index buffer.
    /// </summary>
    public void RenderIndexed(uint indexCount, uint instanceCount, int vertexOffset = 0, uint firstIndex = 0, uint firstInstance = 0)
    {
        SDL.DrawGPUIndexedPrimitives(Handle, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    /// <summary>
    ///     Sets the scissor rectangle for this pass.
    /// </summary>
    public void SetScissorRectangle(Rectangle scissorRectangle)
    {
        if (scissorRectangle.Left < 0)
            scissorRectangle = new Rectangle(0, scissorRectangle.Y, scissorRectangle.Width + scissorRectangle.Left, scissorRectangle.Height);
        if (scissorRectangle.Top < 0)
            scissorRectangle = new Rectangle(scissorRectangle.X, 0, scissorRectangle.Width, scissorRectangle.Height + scissorRectangle.Top);

        SDL.SetGPUScissor(Handle, new SDL.Rect()
        {
            X = scissorRectangle.X,
            Y = scissorRectangle.Y,
            W = scissorRectangle.Width,
            H = scissorRectangle.Height
        });
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        anyActive = false;

        SDL.EndGPURenderPass(Handle);
    }
}
