namespace Northernlight.Core.Resources;

/// <summary>
///     The set of data necessary for the construction of a compute shader.
/// </summary>
public readonly record struct ComputeShaderData
{
    /// <summary>
    ///     The amount of read-only storage buffers maintained
    ///     by the compute shader.
    /// </summary>
    public required uint ReadonlyStorageBufferCount
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of read-only storage textures maintained
    ///     by the compute shader.
    /// </summary>
    public required uint ReadonlyStorageTextureCount
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of read-write storage buffers maintained
    ///     by the compute shader.
    /// </summary>
    public required uint ReadwriteStorageBufferCount
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of read-write storage textures maintained
    ///     by the compute shader.
    /// </summary>
    public required uint ReadwriteStorageTextureCount
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of threads dispatched for
    ///     the X dimension in the shader.
    /// </summary>
    public required uint ThreadCountX
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of threads dispatched for
    ///     the Y dimension in the shader.
    /// </summary>
    public required uint ThreadCountY
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of threads dispatched for
    ///     the Z dimension in the shader.
    /// </summary>
    public required uint ThreadCountZ
    {
        get;
        init;
    }

    public ComputeShaderData() { }
}
