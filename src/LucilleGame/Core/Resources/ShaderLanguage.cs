using Northernlight.Core.Utilities;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents a kind of language that a
///     shader can be written in.
/// </summary>
public static class ShaderLanguage
{
    public delegate SpirvBytecodeHandle Compiler(GraphicsDevice device, Shader shader, string shaderPath);

    /// <summary>
    ///     The HLSL language.
    /// </summary>
    public static Compiler Hlsl => CompileHlsl;

    private static SpirvBytecodeHandle CompileHlsl(GraphicsDevice device, Shader shader, string shaderPath)
    {
        bool success = false;
        ShaderCross.HLSLInfo info = default;
        while (!success)
        {
            try
            {
                info = new ShaderCross.HLSLInfo()
                {
                    ManagedSource = File.ReadAllText(shaderPath),
                    ManagedEntrypoint = shader.Data.Entrypoint,
                    // TODO: IncludeDir
                    // TODO: Defines
                    ShaderStage = GetShaderStage(shader),
                };
                success = true;
            }
            catch (IOException)
            {

            }
            catch
            {
                throw;
            }
        }

        using MemoryUtil.DeferFreeGlobalHandler a = MemoryUtil.DeferFreeGlobal(info.Source);
        using MemoryUtil.DeferFreeGlobalHandler b = MemoryUtil.DeferFreeGlobal(info.Entrypoint);

        nint spirv = ShaderCross.CompileSPIRVFromHLSL(ref info, out nuint size);
        if (spirv == 0)
            throw new Exception($"Failed to compile shader: {shaderPath}; SDL error: {SDL.GetError()}");

        SDL.ClearError();

        return new SpirvBytecodeHandle(spirv, size);
    }

    public static nint CompileSpirv(GraphicsDevice device, Shader shader, SpirvBytecodeHandle handle)
    {
        ShaderCross.SPIRVInfo info = new ShaderCross.SPIRVInfo
        {
            ByteCode = handle.Handle,
            ByteCodeSize = handle.Length,
            ManagedEntrypoint = shader.Data.Entrypoint,
            ShaderStage = GetShaderStage(shader),
        };
        using MemoryUtil.DeferFreeGlobalHandler a = MemoryUtil.DeferFreeGlobal(info.Entrypoint);

        ShaderCross.GraphicsShaderResourceInfo resourceInfo = new ShaderCross.GraphicsShaderResourceInfo
        {
            NumSamplers = shader.Data.SamplerCount,
            NumStorageTextures = shader.Data.StorageTextureCount,
            NumStorageBuffers = shader.Data.StorageBufferCount,
            NumUniformBuffers = shader.Data.UniformBufferCount,
        };

        if (shader is ComputeShader computeShader)
        {
            return CompileSpirvCompute(device, info, new ShaderCross.ComputePipelineMetadata()
            {
                NumUniformBuffers = shader.Data.UniformBufferCount,
                NumSamplers = shader.Data.SamplerCount,

                ThreadCountX = computeShader.ComputeData.ThreadCountX,
                ThreadCountY = computeShader.ComputeData.ThreadCountY,
                ThreadCountZ = computeShader.ComputeData.ThreadCountZ,

                NumReadOnlyStorageBuffers = computeShader.ComputeData.ReadonlyStorageBufferCount,
                NumReadOnlyStorageTextures = computeShader.ComputeData.ReadonlyStorageTextureCount,
                NumReadWriteStorageBuffers = computeShader.ComputeData.ReadwriteStorageBufferCount,
                NumReadWriteStorageTextures = computeShader.ComputeData.ReadwriteStorageTextureCount,
            });
        }

        return CompileSpirv(device, ref info, ref resourceInfo);
    }

    private static nint CompileSpirv(GraphicsDevice device, ref ShaderCross.SPIRVInfo info, ref ShaderCross.GraphicsShaderResourceInfo resourceInfo)
    {
        nint shader = ShaderCross.CompileGraphicsShaderFromSPIRV(device.Handle, ref info, ref resourceInfo, 0);
        if (shader == 0)
            throw new Exception($"Failed to compile shader: {SDL.GetError()}");

        SDL.ClearError();

        return shader;
    }

    private static nint CompileSpirvCompute(GraphicsDevice device, in ShaderCross.SPIRVInfo info, in ShaderCross.ComputePipelineMetadata metadata)
    {
        nint shader = ShaderCross.CompileComputePipelineFromSPIRV(device.Handle, info, metadata, 0);
        if (shader == 0)
            throw new Exception($"Failed to compile shader: {SDL.GetError()}");

        SDL.ClearError();

        return shader;
    }

    private static ShaderCross.ShaderStage GetShaderStage(Shader shader)
    {
        if (shader is ComputeShader)
        {
            return ShaderCross.ShaderStage.Compute;
        }

        return shader.Data.Stage switch
        {
            SDL.GPUShaderStage.Vertex => ShaderCross.ShaderStage.Vertex,
            SDL.GPUShaderStage.Fragment => ShaderCross.ShaderStage.Fragment,
            _ => throw new ArgumentOutOfRangeException(nameof(shader), $"shader.Data.Stage returned unknown stage: {shader.Data.Stage} ({(int)shader.Data.Stage})"),
        };
    }
}
