using System.Numerics;
using Northernlight.Core.Rendering;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents a kind of overlay that can display
///     at the bottomside of the battle menu, such as
///     dialogue, the item use menu, the attack critical
///     hit minigame, etc.
/// </summary>
public abstract class BattleMenuBottomsideOverlay
{
    protected bool justShowedUp = true;

    /// <summary>
    ///     This overlay's associated menu.
    /// </summary>
    public BattleMenu Menu
    {
        get;
        private set;
    }

    /// <summary>
    ///     Whether this menu requires player input
    ///     before they can advance to the hero
    ///     to decide what they should do.
    /// </summary>
    public virtual bool RequiresInput => false;

    public BattleMenuBottomsideOverlay(BattleMenu menu) => Menu = menu;

    /// <summary>
    ///     Pops this overlay off the battle menu
    ///     candidate list, assuming it is the one
    ///     presently in use.
    /// </summary>
    public void Pop(bool moveToNextInterfaceIndex = false)
    {
        if (Menu.OverridingBottomsideOverlayCandidates.TryPeek(out BattleMenuBottomsideOverlay? top) &&
            top == this && !justShowedUp)
        {
            Menu.OverridingBottomsideOverlayCandidates.Pop();

            if (moveToNextInterfaceIndex && Menu.OverridingBottomsideOverlayCandidates.Count <= 0)
                Menu.AdvanceInterfaceIndex();
        }

        if (justShowedUp)
            justShowedUp = false;
    }

    /// <summary>
    ///     Updates this overlay.
    /// </summary>
    public abstract void Update();

    /// <summary>
    ///     Prepares this overlay for rendering.
    /// </summary>
    public abstract void PrepareRender(SpriteRenderer sprites, TextRenderer text, Vector3 center, Vector2 size);
}
