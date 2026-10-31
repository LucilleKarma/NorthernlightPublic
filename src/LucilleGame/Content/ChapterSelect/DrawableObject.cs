using System.Numerics;
using Northernlight.Core.Rendering;

namespace Northernlight.Content.ChapterSelect;

public abstract class DrawableObject
{
    public Vector2 Position { get; set; }

    public List<DrawableObject> Children { get; private set; } = [];

    public void Update()
    {
        UpdateSelf();
        UpdateChildren();
    }

    protected virtual void UpdateSelf() { }

    protected virtual void UpdateChildren()
    {
        foreach (var child in Children)
        {
            child.Update();
        }
    }

    public void PrepareRender(RenderLayer layer)
    {
        PrepareRenderSelf(layer);
        PrepareRenderChildren(layer);
    }

    protected virtual void PrepareRenderSelf(RenderLayer layer) { }

    protected virtual void PrepareRenderChildren(RenderLayer layer)
    {
        foreach (var child in Children)
        {
            child.PrepareRender(layer);
        }
    }
}
