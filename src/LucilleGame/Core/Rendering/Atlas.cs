using System.Buffers;
using Northernlight.Core.Logging;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an atlas builder, which takes
///     a multitude of independent textures and
///     composites them into various atlas entry instances.
/// </summary>
public sealed class Atlas : IDisposable
{
    private bool disposed;

    public readonly record struct AtlasKey(string Name, int SourceId);

    /// <summary>
    ///     This atlas' name.
    /// </summary>
    public string Name
    {
        get;
    }

    /// <summary>
    ///     This atlas' backing texture.
    /// </summary>
    public Texture BackingTexture
    {
        get;
        private set;
    }

    /// <summary>
    ///     The source textures to compose into the
    ///     final atlas.
    /// </summary>
    /// <remarks>
    ///     Entries in this list are indexed in descending
    ///     order based on how tall they are.
    /// </remarks>
    internal readonly List<(Texture Texture, AtlasKey Key, uint SortHeight)> Sources = [];

    /// <summary>
    ///     The entries contained on this atlas.
    /// </summary>
    public readonly Dictionary<AtlasKey, AtlasEntry> Entries = [];

    /// <summary>
    ///     The entries contained on this atlas keyed
    ///     by name.
    /// </summary>
    public readonly Dictionary<string, AtlasEntry> EntriesByName = [];

    /// <summary>
    ///     The suffix required in a given atlas file name
    ///     in order to indicate that padding should be
    ///     omitted from said texture in the composed atlas.
    /// </summary>
    public const string OmitPaddingSuffix = "OmitPadding";

    public Atlas(string name, List<Texture> sources)
    {
        Name = name;
        Sources = sources.Select((t, i) =>
        {
            uint padding = t.Name.Contains(OmitPaddingSuffix) ? 0u : 2u;
            AtlasKey key = new AtlasKey(Name: t.Name, SourceId: i);

            return (Texture: t, Key: key, SortHeight: t.Height + padding);
        }).OrderByDescending(x => x.SortHeight).ToList();
    }

    /// <summary>
    ///     Uploads all texture sources if necessary.
    /// </summary>
    public void UploadSourcesIfNecessary(CommandBuffer.CopyPassScope copyScope)
    {
        foreach (var value in Sources)
            value.Texture.SubmitTransientUploadData(copyScope);
    }

    /// <summary>
    ///     Builds the atlas backing texture and entry references if necessary.
    /// </summary>
    public void BuildIfNecessary(GraphicsDevice device, FrameContext frame)
    {
        if (BackingTexture is not null)
            return;

        int atlasSize = 256;
        Rectangle[] dedicatedRegions = ArrayPool<Rectangle>.Shared.Rent(Sources.Count);
        try
        {
            FineTimeElapseScope scope = new FineTimeElapseScope();
            {

                while (!TryFitSourcesIntoSpace(atlasSize, atlasSize, dedicatedRegions))
                    atlasSize *= 2;
                if (atlasSize > 8192)
                    throw new InvalidOperationException("Atlas requirements exceed dimensions of 8192x8192. This atlas would be too big for various weaker devices.");

                BackingTexture = new Texture(new SDL.GPUTextureCreateInfo()
                {
                    Width = (uint)atlasSize,
                    Height = (uint)atlasSize,
                    Type = SDL.GPUTextureType.TextureType2D,
                    Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
                    LayerCountOrDepth = 1,
                    NumLevels = 1,
                    Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
                }, device);
                WriteSourcesToRegions(frame, dedicatedRegions);
                scope.Dispose();
            }

            Logger.Debug($"Created atlas '{Name}' with size {atlasSize}x{atlasSize} in {scope.Elapsed.TotalSeconds:n4}s.");

            foreach (var source in Sources)
                source.Texture.Dispose();
        }
        finally
        {
            ArrayPool<Rectangle>.Shared.Return(dedicatedRegions);
        }
    }

    private bool TryFitSourcesIntoSpace(int width, int height, Rectangle[] dedicatedRegions)
    {
        uint x = 0;
        uint y = 0;
        uint largestHeightInRow = 0;
        for (int i = 0; i < Sources.Count; i++)
        {
            Texture source = Sources[i].Texture;
            uint padding = 2;
            if (source.Name.Contains(OmitPaddingSuffix))
                padding = 0;

            Rectangle area = new Rectangle(0, 0, (int)(source.Width + padding), (int)(source.Height + padding));
            if (x + area.Width > width)
            {
                x = 0;
                y += largestHeightInRow;
                largestHeightInRow = 0;
            }

            if (y + area.Height > height)
                return false;

            largestHeightInRow = uint.Max(largestHeightInRow, (uint)int.Max(area.Height, 0));
            dedicatedRegions[i] = new Rectangle((int)x, (int)y, area.Width, area.Height);
            x += (uint)area.Width;
        }

        return x < width && y < height;
    }

    private void WriteSourcesToRegions(FrameContext frame, Rectangle[] dedicatedRegions)
    {
        for (int i = 0; i < Sources.Count; i++)
        {
            var (texture, key, _) = Sources[i];
            Rectangle region = dedicatedRegions[i];
            uint padding = 2;
            if (texture.Name.Contains(OmitPaddingSuffix))
                padding = 0;

            texture.CopyTo(frame.CommandBuffer, BackingTexture, (uint)region.Left + padding / 2, (uint)region.Top + padding / 2, texture.Width, texture.Height);

            AtlasEntry entry = new AtlasEntry()
            {
                Owner = this,
                BackingTexture = BackingTexture,
                Area = region,
                Name = key.Name
            };

            EntriesByName[entry.Name] = entry;
            Entries[key] = entry;
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        BackingTexture.Dispose();
    }
}
