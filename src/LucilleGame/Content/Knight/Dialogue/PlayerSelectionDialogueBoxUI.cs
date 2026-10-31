using System.Numerics;
using Northernlight.Core.Localization;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Dialogue;

/// <summary>
///     Represents an instance of a dialogue box that contains
///     player selection options.
/// </summary>
public sealed class PlayerSelectionDialogueBoxUI : LargeDialogueBoxUI
{
    private int currentChoiceIndex = -1;

    private readonly List<Option> options = [];

    private static readonly Font font = new Font("8bitoperator.ttf", 64f);

    public record Option(LanguageAgnosticText Input, Action<PlayerSelectionDialogueBoxUI> OnSelect);

    public PlayerSelectionDialogueBoxUI(List<Option> options) => this.options = options;

    public override void RenderInsideBox(SpriteRenderer sprites, TextRenderer text, Rectangle bounds, float z)
    {
        List<Vector2> possibleSoulPositions = [];
        for (int i = 0; i < options.Count; i++)
        {
            Option option = options[i];
            string line = option.Input.Value;

            float scale = 1f;
            Vector3 textPosition = new Vector3(bounds.Center, z);
            Vector2 textSize = text.MeasureString(line, font);
            Vector2 normalizedOrigin = Vector2.One * 0.5f;
            if (options.Count >= 2)
            {
                float textProgress = i / (float)options.Count;
                float theta = float.Tau * textProgress;
                if (options.Count % 2 == 1)
                    theta += float.Pi / textProgress;

                Vector2 offset = theta.ToUnit() * new Vector2(0.573f, 0.7f) * bounds.Size * 0.5f;
                textPosition += offset.AsVector3();

                if (offset.X < 0f)
                    normalizedOrigin.X = 1f;
                else
                    normalizedOrigin.X = 0f;

                text.Enqueue(line, font, textPosition, Color.White, Vector2.One * scale, normalizedOrigin);
                possibleSoulPositions.Add(textPosition.AsVector2() - Vector2.UnitX * (textSize.X * normalizedOrigin.X + 24f));
            }
        }

        Vector2 soulPosition = bounds.Center;
        if (currentChoiceIndex != -1)
            soulPosition = possibleSoulPositions[currentChoiceIndex];

        AtlasEntry soul = Atlases.KnightScene.Soul;
        sprites.Enqueue(soul, new SpriteData(soul)
        {
            Position = new Vector3(soulPosition, z + 20f),
            Scale = Vector2.One * 2f,
            Tint = Color.White
        });
    }

    public override void Update()
    {
        if (options.Count == 1)
            currentChoiceIndex = 0;

        if (options.Count == 2)
        {
            if (ActiveKeymap.Left.WasJustPressed)
                currentChoiceIndex = 1;
            if (ActiveKeymap.Right.WasJustPressed)
                currentChoiceIndex = 0;
        }

        if (options.Count == 4)
        {
            if (ActiveKeymap.Left.WasJustPressed)
                currentChoiceIndex = 2;
            if (ActiveKeymap.Right.WasJustPressed)
                currentChoiceIndex = 0;
            if (ActiveKeymap.Up.WasJustPressed)
                currentChoiceIndex = 3;
            if (ActiveKeymap.Down.WasJustPressed)
                currentChoiceIndex = 1;
        }

        if (currentChoiceIndex != -1)
        {
            if (ActiveKeymap.Confirm.WasJustPressed)
                options[currentChoiceIndex].OnSelect(this);
        }
    }
}
