using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     The set of data necessary for the construction of a shader.
/// </summary>
public readonly record struct ShaderData
{
    /// <summary>
    ///     The name of the shader's entrypoint function.
    /// </summary>
    public required string Entrypoint
    {
        get;
        init;
    }

    /// <summary>
    ///     The format of the shader, dictated by the desired backend.
    /// </summary>
    public required SDL.GPUShaderFormat Format
    {
        get;
        init;
    } = SDL.GPUShaderFormat.SPIRV;

    /// <summary>
    ///     The stage at which this shader should be executed.
    /// </summary>
    public required SDL.GPUShaderStage Stage
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of samplers maintained by the shader.
    /// </summary>
    public uint SamplerCount
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of storage buffers maintained by the shader.
    /// </summary>
    public uint StorageBufferCount
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of storage textures maintained by the shader.
    /// </summary>
    public uint StorageTextureCount
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of uniforms maintained by the shader.
    /// </summary>
    public uint UniformBufferCount
    {
        get;
        init;
    }

    public ShaderData() { }
}
