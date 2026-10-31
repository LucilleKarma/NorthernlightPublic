using System.Numerics;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.ChapterSelect;

public sealed class UiVersion : DrawableObject
{
    private readonly SelectionMenu menu;

    public bool CopyrightEnabled { get; set; }

    public float Scale { get; set; } = 0.5f;

    public float Alpha { get; set; } = 1f;

    public Font Font { get; private set; } = new Font("8bitoperator.ttf", 16f);

    public UiVersion(SelectionMenu menu) => this.menu = menu;

    protected override void PrepareRenderSelf(RenderLayer layer)
    {
        base.PrepareRenderSelf(layer);

        TextRenderer text = layer.OutputRenderer.Get<TextRenderer>();
        Position = new Vector2(-8f, 185f) + menu.DrawOffset.AsVector2();

        float disappearanceProgress = float.InverseLerp(0f, SelectionMenu.DisappearanceDuration, menu.disappearanceTimer);

        if (CopyrightEnabled)
        {
            int currentYear = DateTime.Now.Year;
            LanguageAgnosticText copyright = LocalizationEntries.DeltaruneCopyrightText.WithArgs(currentYear.ToString());
            text.Enqueue(
                copyright.Value,
                Font,
                SelectionMenu.SquishDrawPosition(new Vector3(Position.X + 16f, Position.Y + 32f, 0f), disappearanceProgress),
                new Color(128f / 255f) * Alpha * (1f - disappearanceProgress).Squared() * menu.FadeInProgress,
                SelectionMenu.SquishDrawScale(Vector2.One * Scale, disappearanceProgress)
            );
        }

        text.Enqueue(
            LocalizationEntries.DeltaruneVersionText.Value,
            Font,
            SelectionMenu.SquishDrawPosition(new Vector3(Position.X + 16f, Position.Y + 40f, 400f), disappearanceProgress),
            new Color(128f / 255f) * Alpha * (1f - disappearanceProgress).Squared() * menu.FadeInProgress,
            SelectionMenu.SquishDrawScale(Vector2.One * Scale, disappearanceProgress)
        );
    }
}
