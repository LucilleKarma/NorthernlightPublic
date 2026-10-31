using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Dialogue;

/// <summary>
///     Represents an abstract instance of a dialogue box that can
///     be rendered.
/// </summary>
public abstract class LargeDialogueBoxUI
{
    /// <summary>
    ///     The color of the box's outline.
    /// </summary>
    public Color OutlineColor
    {
        get;
        set;
    } = Color.White;

    /// <summary>
    ///     The scene responsible for this box.
    /// </summary>
    public required KnightFightScene Scene
    {
        get;
        init;
    }

    /// <summary>
    ///     Renders the contents of the box.
    /// </summary>
    public abstract void RenderInsideBox(SpriteRenderer sprites, TextRenderer text, Rectangle bounds, float z);

    /// <summary>
    ///     Updates this box.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    ///     Renders this box.
    /// </summary>
    public void Render(SpriteRenderer sprites, TextRenderer text)
    {
        float width = NorthernlightGame.KnightFightResolution.X * 0.9f;
        float height = NorthernlightGame.KnightFightResolution.X * 0.25f;
        float borderSize = 12f;
        Vector2 center = NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 1f) - Vector2.UnitY * (height * 0.5f + borderSize);
        Vector2 area = new Vector2(width, height);
        Rectangle box = new Rectangle(center, area);

        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(box.Center, 400f),
            Scale = area,
            Tint = Color.Black
        }, BlendState.NonPremultiplied);

        // Left.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(box.Center - Vector2.UnitX * (area.X * 0.5f - borderSize * 0.5f), 450f),
            Scale = new Vector2(borderSize, area.Y),
            Tint = OutlineColor
        }, BlendState.NonPremultiplied);

        // Right.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(box.Center + Vector2.UnitX * (area.X * 0.5f - borderSize * 0.5f), 450f),
            Scale = new Vector2(borderSize, area.Y),
            Tint = OutlineColor
        }, BlendState.NonPremultiplied);

        // Up.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(box.Center - Vector2.UnitY * (area.Y * 0.5f - borderSize * 0.5f), 450f),
            Scale = new Vector2(area.X, borderSize),
            Tint = OutlineColor
        }, BlendState.NonPremultiplied);

        // Down.
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(box.Center + Vector2.UnitY * (area.Y * 0.5f - borderSize * 0.5f), 450f),
            Scale = new Vector2(area.X, borderSize),
            Tint = OutlineColor
        }, BlendState.NonPremultiplied);

        RenderInsideBox(sprites, text, box, 500f);
    }

    /// <summary>
    ///     Closes this dialogue box.
    /// </summary>
    public void Close() => Scene.DialogueBox = null;
}
