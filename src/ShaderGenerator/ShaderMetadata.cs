using System.Text.RegularExpressions;

namespace ShaderGenerator;

/// <summary>
///     Represents metadata for a general-purpose shader.
/// </summary>
public sealed partial class ShaderMetadata
{
    private readonly string text;

    /// <summary>
    ///     The amount of uniform buffers contained within the shader.
    /// </summary>
    public int UniformBufferCount
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of texture samplers contained within the shader.
    /// </summary>
    public int SamplerCount
    {
        get;
        private set;
    }

    /// <summary>
    ///     The file name of the shader.
    /// </summary>
    public string FileName
    {
        get;
    }

    /// <summary>
    ///     The file name of the shader.
    /// </summary>
    public string ShaderName
    {
        get;
        private set;
    }

    /// <summary>
    ///     The name of the entrypoint of the shader.
    /// </summary>
    public string Entrypoint
    {
        get;
        private set;
    } = "main";

    /// <summary>
    ///     The kind of shader format that this shader has.
    /// </summary>
    public ShaderFormat Format
    {
        get;
    }

    /// <summary>
    ///     All metadata associated exclusively with compute shaders.
    /// </summary>
    /// <remarks>
    ///     This member is <see langword="null"/> if the shader is not a compute shader.
    /// </remarks>
    public ComputeShaderMetadata? ComputeMetadata
    {
        get;
    }

    private static readonly Regex samplerRegexA = GenerateSampleRegexA();

    private static readonly Regex samplerRegexB = GenerateSampleRegexB();

    private static readonly Regex uniformBufferRegex = GenerateUniformBufferRegex();

    private static readonly Regex oneLineCommentRegex = GenerateOneLineCommentRegex();

    private static readonly Regex blockCommentRegex = GenerateBlockLineCommentRegex();

    public ShaderMetadata(string path, ShaderFormat format)
    {
        text = File.ReadAllText(path);

        // Strip away comments before doing any further regex work.
        // They do not affect compilation and will serve only
        // to potentially confuse the search if someone has
        // commented out an old buffers/thread count attribute
        // or the like.
        text = oneLineCommentRegex.Replace(text, string.Empty);
        text = blockCommentRegex.Replace(text, string.Empty);

        FileName = Path.GetFileName(path);
        ShaderName = Path.GetFileNameWithoutExtension(path);
        Format = format;

        SamplerCount = RegexUtils.CountUniqueFromRegex(text, 1, samplerRegexA, samplerRegexB);
        UniformBufferCount = RegexUtils.CountUniqueFromRegex(text, 1, uniformBufferRegex);

        if (Format == ShaderFormat.Compute)
            ComputeMetadata = new ComputeShaderMetadata(text);
    }

    /// <summary>
    ///     Emits basic XML documentation text for this shader.
    /// </summary>
    public void EmitXml(ScopedStringBuilder sb)
    {
        string formatName = Format.ToString().ToLowerInvariant();
        string xml = $"The auto-generated {formatName} shader read from the {FileName} file.";
        string? remarks = null;
        if (Format == ShaderFormat.Unknown)
        {
            xml = $"The auto-generated shader read from the {FileName} file.";
            remarks = "This shader is of an unknown format and may not work correctly when used.";
        }

        sb.Write("/// <summary>");
        sb.Write($"///     {xml}");
        sb.Write("/// </summary>");
        if (!string.IsNullOrEmpty(remarks))
        {
            sb.Write("/// <remarks>");
            sb.Write($"///     {remarks}");
            sb.Write("/// </remarks>");
        }
    }

    /// <summary>
    ///     Emits definition text for this shader metadata into valid C#.
    /// </summary>
    public void EmitDefinitionText(ScopedStringBuilder sb)
    {
        ScopedStringBuilder inner = new ScopedStringBuilder
        {
            IndentNesting = sb.IndentNesting
        };

        inner.Write("new ShaderData()");
        using (inner.IndentScope())
        {
            inner.Write($"Entrypoint = \"{Entrypoint}\",");
            inner.Write($"Format = device.ShaderFormat,");

            string stageDefinition = "SDL.GPUShaderStage.Fragment";
            if (Format == ShaderFormat.Vertex)
                stageDefinition = "SDL.GPUShaderStage.Vertex";

            inner.Write($"Stage = {stageDefinition},");

            inner.Write($"SamplerCount = {SamplerCount},");
            inner.Write($"UniformBufferCount = {UniformBufferCount},");
        }

        string path = $@"Path.Combine(""chapter6_windows"", ""{FileName}"")";
        if (Format == ShaderFormat.Compute && ComputeMetadata is not null)
        {
            ScopedStringBuilder computeInner = new ScopedStringBuilder
            {
                IndentNesting = sb.IndentNesting
            };
            computeInner.Write("new ComputeShaderData()");
            using (computeInner.IndentScope())
                ComputeMetadata.EmitDefinitionText(computeInner);

            sb.Write($"new ComputeShader({path}, ShaderLanguage.Hlsl, device, {inner.ToString().Trim()}, {computeInner.ToString().Trim()});", newline: false, skipIndent: true);
        }
        else
            sb.Write($"new Shader({path}, ShaderLanguage.Hlsl, device, {inner.ToString().Trim()});", newline: false, skipIndent: true);

        sb.Write(skipIndent: true);
    }

    [GeneratedRegex(@"Texture.+ .+ : register\(t(\d+), space2\);")]
    private static partial Regex GenerateSampleRegexA();

    [GeneratedRegex(@"Texture.+ .+ : register\(t(\d+)\);")]
    private static partial Regex GenerateSampleRegexB();

    [GeneratedRegex(@"cbuffer .+ : register\(b(\d+).*\)")]
    private static partial Regex GenerateUniformBufferRegex();

    [GeneratedRegex(@"//.*")]
    private static partial Regex GenerateOneLineCommentRegex();

    [GeneratedRegex(@"/\*[\s\S]*?\*/")]
    private static partial Regex GenerateBlockLineCommentRegex();
}
