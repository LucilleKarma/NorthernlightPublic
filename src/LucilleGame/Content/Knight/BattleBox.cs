using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents an instance of the battle box.
/// </summary>
public sealed class BattleBox
{
    internal List<Afterimage> afterimages = [];

    internal record struct Afterimage(float Opacity, Vector2 CutDirection, float CutDistance, Vector2 Size, Angle Rotation, Vector2 Position);

    /// <summary>
    ///     The biasing direction for afterimage layering.
    /// </summary>
    public int AfterimageZDirection
    {
        get;
        set;
    } = 1;

    /// <summary>
    ///     The box's opacity.
    /// </summary>
    public float Opacity
    {
        get;
        set;
    }

    /// <summary>
    ///     The cut distance of the battle box.
    /// </summary>
    public float CutDistance
    {
        get;
        set;
    }

    /// <summary>
    ///     The cut direction of the battle box.
    /// </summary>
    public Vector2 CutDirection
    {
        get;
        set;
    } = Vector2.UnitX;

    /// <summary>
    ///     The size of the battle box.
    /// </summary>
    public Vector2 Size;

    /// <summary>
    ///     The rotation of the battle box.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }

    /// <summary>
    ///     The centered position of the battle box in
    ///     world space.
    /// </summary>
    public Vector2 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     How far along the battle box distance-wise
    ///     in terms of being shattered into pieces.
    /// </summary>
    public float ShatterDistance
    {
        get;
        set;
    }

    /// <summary>
    ///     The render target that contains this box.
    /// </summary>
    public Texture RenderTarget
    {
        get;
        set;
    }

    /// <summary>
    ///     The cut angles of the battle box.
    /// </summary>
    public readonly List<float> CutAngles = [];

    /// <summary>
    ///     The base Z position of the battle box.
    /// </summary>
    public static float BaseZ => 50f;

    /// <summary>
    ///     Updates this battle box.
    /// </summary>
    public void Update()
    {
        if (CutAngles.Count >= 1)
        {
            afterimages.Clear();
            return;
        }

        afterimages.Add(new Afterimage(Opacity, CutDirection, CutDistance, Size, Rotation, Position));
        while (afterimages.Count >= 50)
            afterimages.RemoveAt(0);
    }

    /// <summary>
    ///     Performs necessary cleanup work for when
    ///     the attack ends.
    /// </summary>
    public void OnAttackEnd()
    {
        CutAngles.Clear();
        ShatterDistance = 0f;
    }

    /// <summary>
    ///     Modifies a given input collision box in order
    ///     to keep it inside of this battle box.
    /// </summary>
    public Vector2 KeepInside(Vector2 collisionBoxCenter, Vector2 collisionBoxArea)
    {
        // OK rotation-based AABB logic is annoying.
        // So let's be clever!
        // Instead, we'll do the inverse rotation on the
        // actual point, clamp it, and then
        // go back to absolute space.

        // It's the PERFECT plan...

        Vector2 inverseFrame = Position + (collisionBoxCenter - Position).RotatedBy(-Rotation);
        inverseFrame = Vector2.Clamp(inverseFrame, Position - Size * 0.5f + collisionBoxArea * 0.5f, Position + Size * 0.5f - collisionBoxArea * 0.5f);

        return Position + (inverseFrame - Position).RotatedBy(Rotation);
    }

    /// <summary>
    ///     Modifies a given input collision box in order
    ///     to keep it inside of this battle box.
    /// </summary>
    public Vector3 KeepInside(Vector3 collisionBoxCenter, Vector2 collisionBoxArea)
    {
        Vector2 result = KeepInside(collisionBoxCenter.AsVector2(), collisionBoxArea);
        return new Vector3(result, collisionBoxCenter.Z);
    }

    /// <summary>
    ///     Clears all afterimages.
    /// </summary>
    public void ClearAfterimages(bool keepOne = true)
    {
        if (afterimages.Count <= 0)
            return;

        if (keepOne)
            afterimages = [afterimages[0]];
        else
            afterimages.Clear();
    }

    private static void RenderAfterimage(SpriteRenderer sprites, Afterimage afterimage, float zOffset = 0f, float borderWidthFactor = 1f)
    {
        if (afterimage.Size == Vector2.Zero)
            return;

        float z = zOffset + BaseZ;
        float borderWidth = borderWidthFactor * 4f;
        Color borderColor = new Color(0, 192, 0);
        Vector2 forward = afterimage.CutDirection;
        Vector2 up = forward.RotatedBy(Angle.PiOver2);
        Vector2 splitSize = afterimage.Size * new Vector2(1f, 0.501f);
        Vector3 downwardOffset = new Vector3(up.RotatedBy(afterimage.Rotation + float.Pi) * afterimage.Size.Y * 0.5f, 0f);

        Vector2 origin = afterimage.Position - downwardOffset.AsVector2() * 0.5f;
        Vector3 left = new Vector3(origin - (forward * splitSize.X * 0.5f).RotatedBy(afterimage.Rotation), z);
        Vector3 right = new Vector3(origin + (forward * splitSize.X * 0.5f).RotatedBy(afterimage.Rotation), z);
        Vector3 top = new Vector3(origin + (up * splitSize.Y * 0.5f).RotatedBy(afterimage.Rotation), z);
        Vector3 bottom = new Vector3(origin - (up * splitSize.Y * 0.5f).RotatedBy(afterimage.Rotation), z);
        Vector3 splitOffset = new Vector3(forward * afterimage.CutDistance * 0.5f, 0f);

        AtlasEntry pixel = Atlases.KnightScene.Pixel;

        // Top half.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(origin, z - 10f) + splitOffset,
            Scale = splitSize,
            Rotation = afterimage.Rotation + forward,
            Tint = Color.Black * afterimage.Opacity,
        }, BlendState.NonPremultiplied);
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = left + splitOffset,
            Scale = new Vector2(borderWidth, splitSize.Y + borderWidth),
            Rotation = afterimage.Rotation + forward,
            Tint = borderColor * afterimage.Opacity
        }, BlendState.NonPremultiplied);
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = right + splitOffset,
            Scale = new Vector2(borderWidth, splitSize.Y + borderWidth),
            Rotation = afterimage.Rotation + forward,
            Tint = borderColor * afterimage.Opacity
        }, BlendState.NonPremultiplied);
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = top + splitOffset,
            Scale = new Vector2(splitSize.X + borderWidth, borderWidth),
            Rotation = afterimage.Rotation + forward,
            Tint = borderColor * afterimage.Opacity
        }, BlendState.NonPremultiplied);

        // Bottom half.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(origin, z - 10f) - splitOffset + downwardOffset,
            Scale = splitSize,
            Rotation = afterimage.Rotation + forward,
            Tint = Color.Black * afterimage.Opacity,
        }, BlendState.NonPremultiplied);
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = left - splitOffset + downwardOffset,
            Scale = new Vector2(borderWidth, splitSize.Y + borderWidth),
            Rotation = afterimage.Rotation + forward,
            Tint = borderColor * afterimage.Opacity
        }, BlendState.NonPremultiplied);
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = right - splitOffset + downwardOffset,
            Scale = new Vector2(borderWidth, splitSize.Y + borderWidth),
            Rotation = afterimage.Rotation + forward,
            Tint = borderColor * afterimage.Opacity
        }, BlendState.NonPremultiplied);
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = bottom - splitOffset + downwardOffset,
            Scale = new Vector2(splitSize.X + borderWidth, borderWidth),
            Rotation = afterimage.Rotation + forward,
            Tint = borderColor * afterimage.Opacity
        }, BlendState.NonPremultiplied);
    }

    /// <summary>
    ///     Converts a given angle to a position on a unit
    ///     square's edge via internal polar coordinate
    ///     calculations.
    /// </summary>
    /// <param name="theta"></param>
    /// <returns></returns>
    internal static Vector2 AngleToSquare(float theta)
    {
        theta = theta.Modulo(float.Tau);
        float cosTheta = float.Cos(theta);
        float sinTheta = float.Sin(theta);

        float r;
        if (theta < Angle.PiOver4)
            r = 1f / cosTheta;
        else if (theta < Angle.PiOver4 * 3f)
            r = 1f / sinTheta;
        else if (theta < Angle.PiOver4 * 5f)
            r = -1f / cosTheta;
        else if (theta < Angle.PiOver4 * 7f)
            r = -1f / sinTheta;
        else
            r = 1f / cosTheta;

        return new Vector2(cosTheta, sinTheta) * r;
    }

    private static void PrepareShatteredBoxPipeline(FrameContext frame, RenderPass pass, Camera camera)
    {
        GraphicsDevice gd = frame.Device;
        pass.BindFragmentSamplerTextures(0, new RenderPass.SamplerTexture(gd.PointWrap, Atlases.KnightScene.Atlas.Value.BackingTexture));
        frame.CommandBuffer.BindVertexUniformData(0, camera.View * camera.Projection);
    }

    private void RenderShatteredVersion(MeshRenderer trails, FrameContext context)
    {
        List<float> orderedCutAngles = CutAngles.OrderBy(c => c.Modulo(float.Tau)).ToList();

        float z = 50f;

        StandardVertex[] vertices = new StandardVertex[orderedCutAngles.Count * 3];
        uint[] indices = new uint[orderedCutAngles.Count * 3];

        AtlasEntry box = Atlases.KnightScene.BattleBoxForShatter;
        Vector4 boxBounds = box.VectorBounds;
        Vector2 boxTopLeft = new Vector2(boxBounds.X, boxBounds.Y);
        Vector2 boxBottomRight = new Vector2(boxBounds.Z, boxBounds.W);

        Vector2 boxArea = Vector2.One * 300f;

        for (int i = 0; i < orderedCutAngles.Count; i++)
        {
            float leftTheta = orderedCutAngles[i];
            float rightTheta = orderedCutAngles[(i + 1) % orderedCutAngles.Count];
            Vector2 leftSquare = AngleToSquare(leftTheta);
            Vector2 rightSquare = AngleToSquare(rightTheta);

            float distanceVariance = 1f - float.Cos01(leftTheta * 150.547f) * 0.7f;
            Vector2 forwardOffset = rightSquare * ShatterDistance * distanceVariance;
            Vector2 center = Position + forwardOffset;

            StandardVertex centerVertex = new StandardVertex(new Vector3(center, z), Vector4.One, Vector3.UnitZ, (boxTopLeft + boxBottomRight) * 0.5f);

            Vector2 t = leftSquare * 0.5f + Vector2.One * 0.5f;
            Vector2 leftUv = Vector2.Lerp(boxTopLeft, boxBottomRight, t);

            t = rightSquare * 0.5f + Vector2.One * 0.5f;
            Vector2 rightUv = Vector2.Lerp(boxTopLeft, boxBottomRight, t);

            StandardVertex leftVertex = new StandardVertex(new Vector3(center + leftSquare * boxArea * 0.5f, z), Vector4.One, Vector3.UnitZ, leftUv);
            StandardVertex rightVertex = new StandardVertex(new Vector3(center + rightSquare * boxArea * 0.5f, z), Vector4.One, Vector3.UnitZ, rightUv);

            int baseVertexIndex = i * 3;

            vertices[baseVertexIndex] = centerVertex;
            vertices[baseVertexIndex + 1] = leftVertex;
            vertices[baseVertexIndex + 2] = rightVertex;

            indices[baseVertexIndex] = (uint)baseVertexIndex;
            indices[baseVertexIndex + 1] = (uint)(baseVertexIndex + 1);
            indices[baseVertexIndex + 2] = (uint)(baseVertexIndex + 2);
        }

        OwnedMesh<StandardVertex> mesh = new OwnedMesh<StandardVertex>(GPUPrimitiveType.TriangleList, vertices, indices, indices.Length / 3);
        trails.EnqueueMesh(mesh, context.CommandBuffer, new BasicMeshSettings()
        {
            FragmentShader = Shaders.BasicFrag,
            VertexShader = Shaders.BasicVert,
            PreparationAction = PrepareShatteredBoxPipeline
        });
    }

    /// <summary>
    ///     Prepares this battle box for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, MeshRenderer trails, FrameContext context, int maxAfterimageCount, float borderWidthFactor = 1f)
    {
        if (CutAngles.Count >= 3)
        {
            RenderShatteredVersion(trails, context);
            return;
        }

        if (afterimages.Count <= 0)
            return;

        if (maxAfterimageCount == 1)
        {
            RenderAfterimage(sprites, new Afterimage(Opacity, CutDirection, CutDistance, Size, Rotation, Position), 0f, borderWidthFactor);
            return;
        }

        for (int i = 0; i < int.Min(afterimages.Count, maxAfterimageCount); i++)
        {
            float opacityFactor = float.Lerp(i / (float)afterimages.Count, 1f, Opacity);
            RenderAfterimage(sprites, afterimages[i] with
            {
                Opacity = afterimages[i].Opacity * opacityFactor
            }, i * AfterimageZDirection * 0.1f, borderWidthFactor);
        }
    }
}
