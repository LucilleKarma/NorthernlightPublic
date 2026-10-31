namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a compute shader.
/// </summary>
public class ComputeShader : Shader
{
    /// <summary>
    ///     The composite data that determines the structure
    ///     of this compute shader.
    /// </summary>
    public ComputeShaderData ComputeData
    {
        get;
    }

    /// <summary>
    ///     The multiplied amount of thread counts across all dimensions.
    /// </summary>
    public uint InnerThreadSize => ComputeData.ThreadCountX * ComputeData.ThreadCountY * ComputeData.ThreadCountZ;

    public ComputeShader(string filePath, ShaderLanguage.Compiler shaderCompiler, GraphicsDevice device, ShaderData data, ComputeShaderData computeData) :
        base(filePath)
    {
        ComputeData = computeData;
        RunConstructor(filePath, shaderCompiler, device, data);
    }

    /// <summary>
    ///     Dispatches a pipeline for execution based on input data for this
    ///     compute shader.
    /// </summary>
    public T[] Dispatch<T>(ReadOnlySpan<T> inputData, ComputeGroupCounts groupCounts = default, ComputePipeline.DispatchCallback? commandBufferPreparations = null) where T : unmanaged
    {
        using ComputePipeline pipeline = new ComputePipeline(Device, this);
        if (groupCounts == default)
            groupCounts = ComputeGroupCounts.FromExpected(this, inputData.Length);

        return pipeline.Dispatch(inputData, groupCounts, commandBufferPreparations);
    }
}
