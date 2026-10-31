using System.Runtime.InteropServices;
using Northernlight.Core.Utilities;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a compute pipeline.
/// </summary>
public sealed class ComputePipeline : IDisposable, IPipeline
{
    private bool disposed;

    /// <summary>
    ///     The handle associated with this
    ///     pipeline.
    /// </summary>
    internal nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     This pipeline's compute shader.
    /// </summary>
    public ComputeShader Shader
    {
        get;
    }

    /// <summary>
    ///     The graphics device that owns this pipeline.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    public delegate void DispatchCallback(CommandBuffer commandBuffer, ComputePass computePass);

    public ComputePipeline(GraphicsDevice device, ComputeShader shader)
    {
        Device = device;
        Shader = shader;
        shader.AttachedPipelines.Add(new WeakReference<IPipeline>(this));
        Recreate();
    }

    /// <summary>
    ///     (Re)creates this pipeline.
    /// </summary>
    public unsafe void Recreate()
    {
        byte* entrypointBuffer = InteropUtils.EncodeToUTF8Buffer(Shader.Data.Entrypoint);
        try
        {
            SDL.GPUComputePipelineCreateInfo pipelineInfo = new SDL.GPUComputePipelineCreateInfo()
            {
                Code = Shader.CodeHandle.Handle,
                CodeSize = Shader.CodeHandle.Length,
                Entrypoint = (nint)entrypointBuffer,
                Format = Shader.Data.Format,
                NumSamplers = Shader.Data.SamplerCount,
                NumUniformBuffers = Shader.Data.UniformBufferCount,

                NumReadonlyStorageBuffers = Shader.ComputeData.ReadonlyStorageBufferCount,
                NumReadonlyStorageTextures = Shader.ComputeData.ReadonlyStorageTextureCount,
                NumReadwriteStorageBuffers = Shader.ComputeData.ReadwriteStorageBufferCount,
                NumReadwriteStorageTextures = Shader.ComputeData.ReadwriteStorageTextureCount,

                ThreadcountX = Shader.ComputeData.ThreadCountX,
                ThreadcountY = Shader.ComputeData.ThreadCountY,
                ThreadcountZ = Shader.ComputeData.ThreadCountZ
            };

            if (Handle != 0)
                SDL.ReleaseGPUComputePipeline(Device.Handle, Handle);
            Handle = SDL.CreateGPUComputePipeline(Device.Handle, pipelineInfo);
        }
        finally
        {
            NativeMemory.Free(entrypointBuffer);
        }
    }

    /// <summary>
    ///     Binds this pipeline to a given render pass.
    /// </summary>
    public void BindToPass(ComputePass pass) => SDL.BindGPUComputePipeline(pass.Handle, Handle);

    /// <summary>
    ///     Dispatches this pipeline for execution based on input data.
    /// </summary>
    public T[] Dispatch<T>(ReadOnlySpan<T> inputData, ComputeGroupCounts groupCounts, DispatchCallback? commandBufferPreparations = null) where T : unmanaged
    {
        using StorageBuffer<T> buffer = new StorageBuffer<T>(Device, inputData, true, true);
        return Dispatch(buffer, groupCounts, commandBufferPreparations);
    }

    /// <summary>
    ///     Dispatches this pipeline for execution based on a
    ///     read-write input buffer.
    /// </summary>
    public T[] Dispatch<T>(StorageBuffer<T> inputBuffer, ComputeGroupCounts groupCounts, DispatchCallback? commandBufferPreparations = null) where T : unmanaged
    {
        CommandBuffer commandBuffer = Device.GenerateCommandBuffer();
        using (ComputePass computePass = new ComputePass(commandBuffer, inputBuffer))
        {
            BindToPass(computePass);
            commandBufferPreparations?.Invoke(commandBuffer, computePass);
            computePass.Dispatch(groupCounts);
        }
        commandBuffer.Dispose();

        return inputBuffer[Device];
    }

    public void Dispose()
    {
        if (disposed || Handle == nint.Zero)
            return;

        disposed = true;
        SDL.ReleaseGPUComputePipeline(Device.Handle, Handle);
    }
}
