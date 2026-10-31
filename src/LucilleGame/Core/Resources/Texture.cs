using System.Numerics;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of an SDL texture.
/// </summary>
public unsafe class Texture : IDisposable
{
    private bool disposed;

    private TransferBuffer<byte>? transientFileUploadBuffer;

    /// <summary>
    ///     The handle of this texture to its underlying
    ///     SDL resource.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    } = nint.Zero;

    /// <summary>
    ///     The width of this texture, in pixels.
    /// </summary>
    public uint Width
    {
        get;
        private set;
    }

    /// <summary>
    ///     The height of this texture, in pixels.
    /// </summary>
    public uint Height
    {
        get;
        private set;
    }

    /// <summary>
    ///     The name of this texture.
    /// </summary>
    public string Name
    {
        get;
        private set;
    } = string.Empty;

    /// <summary>
    ///     The size of this texture.
    /// </summary>
    public Vector2 Size => new Vector2(Width, Height);

    /// <summary>
    ///     The graphics device that owns this texture.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
        private set;
    }

    /// <summary>
    ///     The format of this texture.
    /// </summary>
    public SDL.GPUTextureFormat Format
    {
        get;
        private set;
    }

    public Texture(SDL.GPUTextureCreateInfo creationInfo, GraphicsDevice device)
    {
        Handle = SDL.CreateGPUTexture(device.Handle, creationInfo);
        Device = device;
        Width = creationInfo.Width;
        Height = creationInfo.Height;
        Format = creationInfo.Format;
    }

    public Texture(nint handle, uint width, uint height, GraphicsDevice device, SDL.GPUTextureFormat format)
    {
        Handle = handle;
        Device = device;
        Width = width;
        Height = height;
        Format = format;
    }

    internal Texture(string filePath, GraphicsDevice device)
    {
        filePath = ResourceMonitor.CheckForSubdirectoryEquivalent(filePath);

        Device = device;
        Name = Path.GetFileNameWithoutExtension(filePath);
        GenerateFromFilePath(filePath);
    }

    private void GenerateFromFilePath(string filePath)
    {
        nint surfaceHandle = SDL.LoadPNG(filePath);
        nint convertedSurfaceHandle = SDL.ConvertSurface(surfaceHandle, SDL.PixelFormat.ABGR8888);

        SDL.Surface surface =
            SDL.PointerToStructure<SDL.Surface>(convertedSurfaceHandle) ??
            throw new Exception("Could not create surface structure.");

        Width = (uint)surface.Width;
        Height = (uint)surface.Height;

        Format = SDL.GPUTextureFormat.R8G8B8A8Unorm;
        Handle = SDL.CreateGPUTexture(Device.Handle, new SDL.GPUTextureCreateInfo()
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = Format,
            Width = Width,
            Height = Height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.Sampler
        });

        uint totalPixels = Width * Height;
        uint totalBytes = totalPixels * 4;

        transientFileUploadBuffer = new TransferBuffer<byte>(Device, SDL.GPUTransferBufferUsage.Upload, totalBytes);
        {
            using var mapScope = transientFileUploadBuffer.Map();
            {
                byte* sourceBase = (byte*)surface.Pixels;
                byte* destinationBase = (byte*)mapScope.TransferHandle;

                for (uint y = 0; y < Height; y++)
                {
                    byte* sourceRow = sourceBase + y * surface.Pitch;
                    byte* destinationRow = destinationBase + y * Width * 4;
                    Buffer.MemoryCopy(sourceRow, destinationRow, surface.Pitch, surface.Pitch);
                }
            }
        }

        SDL.DestroySurface(surfaceHandle);
        SDL.DestroySurface(convertedSurfaceHandle);
    }

    internal void SubmitTransientUploadData(CommandBuffer.CopyPassScope copyScope)
    {
        if (transientFileUploadBuffer is null)
            return;

        SDL.GPUTextureTransferInfo transferData = new SDL.GPUTextureTransferInfo()
        {
            TransferBuffer = transientFileUploadBuffer.Handle,
            Offset = 0
        };
        SDL.GPUTextureRegion textureRegion = new SDL.GPUTextureRegion()
        {
            Texture = Handle,
            W = Width,
            H = Height,
            D = 1
        };

        SDL.UploadToGPUTexture(copyScope.PassHandle, transferData, textureRegion, false);

        transientFileUploadBuffer?.Dispose();
        transientFileUploadBuffer = null;
    }

    /// <summary>
    ///     Enters a scope for use with rendering
    ///     into this texture.
    /// </summary>
    public TextureWriteScope Scope(RendererPool pool, FrameContext frame, Sampler sampler, bool preservePreviousContents = false, Color? clearColor = null) =>
        new TextureWriteScope(pool, this, frame, sampler, preservePreviousContents, clearColor);

    /// <summary>
    ///     Copies the contents of this texture to
    ///     a region on another texture.
    /// </summary>
    public void CopyTo(CommandBuffer commandBuffer, Texture toCopyTo, uint toCopyToLeftX = 0, uint toCopyToTopY = 0, uint widthOverride = 0, uint heightOverride = 0)
    {
        if (widthOverride == 0)
            widthOverride = Width;
        if (heightOverride == 0)
            heightOverride = Height;

        using var copyScope = commandBuffer.Copy();
        {
            SDL.GPUTextureLocation selfLocation = new SDL.GPUTextureLocation()
            {
                Texture = Handle
            };
            SDL.GPUTextureLocation destinationLocation = new SDL.GPUTextureLocation()
            {
                Texture = toCopyTo.Handle,
                X = toCopyToLeftX,
                Y = toCopyToTopY
            };
            if (toCopyToLeftX + Width > toCopyTo.Width)
                throw new InvalidOperationException("Horizontal texture bounds exceeded.");
            if (toCopyToTopY + Height > toCopyTo.Height)
                throw new InvalidOperationException("Vertical texture bounds exceeded.");

            SDL.CopyGPUTextureToTexture(copyScope.PassHandle, selfLocation, destinationLocation, widthOverride, heightOverride, 0, false);
        }
    }

    public void Dispose()
    {
        if (disposed || Handle == nint.Zero)
            return;

        disposed = true;
        GC.SuppressFinalize(this);
        SDL.ReleaseGPUTexture(Device.Handle, Handle);
    }
}
