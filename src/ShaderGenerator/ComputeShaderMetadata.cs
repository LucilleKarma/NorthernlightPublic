using System.Globalization;
using System.Text.RegularExpressions;

namespace ShaderGenerator;

/// <summary>
///     Represents metadata for a compute shader.
/// </summary>
public sealed partial class ComputeShaderMetadata
{
    private readonly string text;

    /// <summary>
    ///     The amount of read-only storage buffers maintained
    ///     by the shader.
    /// </summary>
    public uint ReadonlyStorageBufferCount
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of read-only storage textures maintained
    ///     by the shader.
    /// </summary>
    public uint ReadonlyStorageTextureCount
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of read-write storage buffers maintained
    ///     by the shader.
    /// </summary>
    public uint ReadwriteStorageBufferCount
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of read-write storage textures maintained
    ///     by the shader.
    /// </summary>
    public uint ReadwriteStorageTextureCount
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of threads dispatched for
    ///     the X dimension in the shader.
    /// </summary>
    public uint ThreadCountX
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of threads dispatched for
    ///     the Y dimension in the shader.
    /// </summary>
    public uint ThreadCountY
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of threads dispatched for
    ///     the Z dimension in the shader.
    /// </summary>
    public uint ThreadCountZ
    {
        get;
        private set;
    }

    private static readonly Regex threadCountRegex = GenerateThreadCountRegex();

    private static readonly Regex readonlyStorageTextureRegex = GenerateReadonlyStorageTextureRegex();

    private static readonly Regex readonlyStorageBufferRegex = GenerateReadonlyStorageBufferRegex();

    private static readonly Regex readwriteStorageTextureRegex = GenerateReadwriteStorageTextureRegex();

    private static readonly Regex readwriteStorageBufferRegex = GenerateReadwriteStorageBufferRegex();

    public ComputeShaderMetadata(string text)
    {
        this.text = text;

        CalculateThreadCounts();

        ReadonlyStorageTextureCount = (uint)RegexUtils.CountUniqueFromRegex(text, 1, readonlyStorageTextureRegex);
        ReadonlyStorageBufferCount = (uint)RegexUtils.CountUniqueFromRegex(text, 1, readonlyStorageBufferRegex);
        ReadwriteStorageTextureCount = (uint)RegexUtils.CountUniqueFromRegex(text, 1, readwriteStorageTextureRegex);
        ReadwriteStorageBufferCount = (uint)RegexUtils.CountUniqueFromRegex(text, 1, readwriteStorageBufferRegex);
    }

    private void CalculateThreadCounts()
    {
        Match match = threadCountRegex.Match(text);
        if (!match.Success)
            throw new InvalidOperationException("Compute shaders require a valid numthreads attribute header.");

        ThreadCountX = GetThreadGroupCount(match.Groups[1].Value, "X");
        ThreadCountY = GetThreadGroupCount(match.Groups[2].Value, "Y");
        ThreadCountZ = GetThreadGroupCount(match.Groups[3].Value, "Z");
    }

    private static uint GetThreadGroupCount(string inputText, string dimensionName)
    {
        if (int.TryParse(inputText, CultureInfo.InvariantCulture, out int valueUnsafe))
        {
            if (valueUnsafe <= 0)
                throw new InvalidOperationException($"The specified thread count in the {dimensionName} dimension is invalid. A value of one or greater is required.");

            return (uint)valueUnsafe;
        }
        else
            throw new InvalidOperationException($"The specified thread count in the {dimensionName} dimension could not be meaningfully casted to an integer.");
    }

    // The .* suffixes exist to catch edge cases where a developer
    // uses an integral numerical suffix, such as 8u or whatever.
    // Quite rare in practice, most people just write out standard
    // numerals in those headers, but better to be safe than sorry.

    // Also the -? qualifiers exist to catch negative numbers, naturally.
    // The construction of the metadata will throw its own exception if
    // anyone actually tries this, so as to inform developers of
    // mistakes there at build time.
    [GeneratedRegex(@"\[numthreads\((-?\d+).*, (-?\d+).*, (-?\d+).*\)\]")]
    private static partial Regex GenerateThreadCountRegex();

    [GeneratedRegex(@"Texture2D<.+> .+ : register\(t(\d+), space0\);")]
    private static partial Regex GenerateReadonlyStorageTextureRegex();

    [GeneratedRegex(@"StructuredBuffer<.+> .+ : register\(t(\d+), space0\);")]
    private static partial Regex GenerateReadonlyStorageBufferRegex();

    [GeneratedRegex(@"RWTexture2D<.+> .+ : register\(u(\d+), space1\);")]
    private static partial Regex GenerateReadwriteStorageTextureRegex();

    [GeneratedRegex(@"RWStructuredBuffer<.+> .+ : register\(u(\d+), space1\);")]
    private static partial Regex GenerateReadwriteStorageBufferRegex();

    /// <summary>
    ///     Emits definition text for this compute shader metadata into valid C#.
    /// </summary>
    public void EmitDefinitionText(ScopedStringBuilder sb)
    {
        sb.Write($"ThreadCountX = {ThreadCountX},");
        sb.Write($"ThreadCountY = {ThreadCountY},");
        sb.Write($"ThreadCountZ = {ThreadCountZ},");
        sb.Write($"ReadonlyStorageTextureCount = {ReadonlyStorageTextureCount},");
        sb.Write($"ReadonlyStorageBufferCount = {ReadonlyStorageBufferCount},");
        sb.Write($"ReadwriteStorageTextureCount = {ReadwriteStorageTextureCount},");
        sb.Write($"ReadwriteStorageBufferCount = {ReadwriteStorageBufferCount},");
    }
}
