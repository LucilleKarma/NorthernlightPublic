namespace Northernlight.Core.Resources;

/// <summary>
///     Represents a collection of compute shader group
///     partition counts.
/// </summary>
public readonly record struct ComputeGroupCounts(uint X, uint Y, uint Z)
{
    /// <summary>
    ///     Generates a group count quantity from an
    ///     expected dimensional partition.
    /// </summary>
    /// <remarks>
    ///     This method will throw an exception if data size
    ///     values are negative.
    /// </remarks>
    public static ComputeGroupCounts FromExpected(ComputeShader shader, int dataSizeX, int dataSizeY = 0, int dataSizeZ = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dataSizeX, nameof(dataSizeX));
        ArgumentOutOfRangeException.ThrowIfNegative(dataSizeY, nameof(dataSizeY));
        ArgumentOutOfRangeException.ThrowIfNegative(dataSizeZ, nameof(dataSizeZ));

        return FromExpected(shader, (uint)dataSizeX, (uint)dataSizeY, (uint)dataSizeZ);
    }

    /// <summary>
    ///     Generates a group count quantity from an
    ///     expected dimensional partition.
    /// </summary>
    public static ComputeGroupCounts FromExpected(ComputeShader shader, uint dataSizeX, uint dataSizeY = 0, uint dataSizeZ = 0)
    {
        uint groupCountX = uint.Max(1, (dataSizeX + shader.ComputeData.ThreadCountX) / shader.ComputeData.ThreadCountX);
        uint groupCountY = uint.Max(1, (dataSizeY + shader.ComputeData.ThreadCountY) / shader.ComputeData.ThreadCountY);
        uint groupCountZ = uint.Max(1, (dataSizeZ + shader.ComputeData.ThreadCountZ) / shader.ComputeData.ThreadCountZ);
        return new ComputeGroupCounts(groupCountX, groupCountY, groupCountZ);
    }
}
