using System.Numerics;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     A service responsible for mapping various simple types
///     to corresponding vertex mappings.
/// </summary>
public static class TypeVertexMappings
{
    public readonly record struct Result(uint Size, SDL.GPUVertexElementFormat Format);

    /// <summary>
    ///     Evaluates a given type and determines
    ///     its size and format, throwing an exception
    ///     if anything goes wrong.
    /// </summary>
    public static Result Evaluate(Type type)
    {
        if (type == typeof(uint))
            return new Result(sizeof(uint), SDL.GPUVertexElementFormat.Uint);
        if (type == typeof(int))
            return new Result(sizeof(int), SDL.GPUVertexElementFormat.Int);

        if (type == typeof(float))
            return new Result(sizeof(float), SDL.GPUVertexElementFormat.Float);
        if (type == typeof(Vector2))
            return new Result(sizeof(float) * 2, SDL.GPUVertexElementFormat.Float2);
        if (type == typeof(Vector3))
            return new Result(sizeof(float) * 3, SDL.GPUVertexElementFormat.Float3);
        if (type == typeof(Vector4))
            return new Result(sizeof(float) * 4, SDL.GPUVertexElementFormat.Float4);
        if (type == typeof(Matrix4x4))
            return new Result(sizeof(float) * 16, SDL.GPUVertexElementFormat.Float4);

        throw new Exception("Invalid type.");
    }
}
