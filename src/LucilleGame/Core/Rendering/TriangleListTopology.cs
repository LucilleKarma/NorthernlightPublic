using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

public sealed class TriangleListTopology : ITrailTopologySetting
{
    public static GPUPrimitiveType Type => GPUPrimitiveType.TriangleList;

    public static int CalculateIndexCount(int pointCount) => (pointCount - 2) * 6;

    public static int CalculatePrimitiveCount(int pointCount) => (pointCount - 2) * 2;

    public static void LoadData(CommandBuffer commandBuffer, out uint[] indicesCache, out MeshStream<StandardVertex> meshGpu)
    {
        int maxSupportedTrailPoints = 1 << 12;
        int vertexCapacity = maxSupportedTrailPoints * 2;
        int indexCapacity = CalculateIndexCount(maxSupportedTrailPoints);

        int indicesIndex = 0;
        indicesCache = new uint[indexCapacity];
        for (uint i = 0; indicesIndex + 5 < indexCapacity; i++)
        {
            uint connectToIndex = i * 2;
            indicesCache[indicesIndex++] = connectToIndex;
            indicesCache[indicesIndex++] = connectToIndex + 1;
            indicesCache[indicesIndex++] = connectToIndex + 2;
            indicesCache[indicesIndex++] = connectToIndex + 2;
            indicesCache[indicesIndex++] = connectToIndex + 1;
            indicesCache[indicesIndex++] = connectToIndex + 3;
        }

        meshGpu = new MeshStream<StandardVertex>(commandBuffer.Device, commandBuffer, new StandardVertex[vertexCapacity], indicesCache);
    }
}
