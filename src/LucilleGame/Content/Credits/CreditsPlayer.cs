using System.Numerics;
using Northernlight.Content.ChapterSelect;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Credits;

public sealed class CreditsPlayer : DrawableObject
{
    /// <summary>
    ///     Represents an instance of displayable line in the
    ///     credits sequence.
    /// </summary>
    public record Line(LanguageAgnosticText Text, bool Title = false)
    {
        /// <summary>
        ///     A newline.
        /// </summary>
        public static readonly Line Newline = new Line(LocalizationEntries.Newline);
    }

    /// <summary>
    ///     Represents a displayable collection of lines that can
    ///     be shown on the screen in bulk.
    /// </summary>
    public record Display(Line[] Lines);

    private int displayIndex;

    private readonly CreditsScene scene;

    private readonly OwnedTimer displayChangeTimer;

    private readonly Display[] displays = [];

    private static readonly Font font = new Font("8bitoperator.ttf", 64f);

    /// <summary>
    ///     Whether the credits have fully played out or not.
    /// </summary>
    public bool Done => displayIndex >= displays.Length;

    public CreditsPlayer(CreditsScene scene, params Display[] displays)
    {
        this.displays = displays;
        this.scene = scene;
        displayChangeTimer = scene.Time.Rent();

        scene.Audio.Play(new Sound(AudioBuffer.FromWav("CreditsCutSound.wav"), 0.6f));
        scene.Music.Add(new Sound(AudioBuffer.FromWav("SilentNight.wav", musicDirectory: true)));
    }

    protected override void UpdateSelf()
    {
        base.UpdateSelf();
        if (Done)
            return;

        float cycleRate = scene.CloseGameAtEnd ? 5f : 4.8f;
        if (displayChangeTimer.Cycle(cycleRate))
        {
            displayIndex++;
            if (Done && scene.CloseGameAtEnd)
                return;

            Sound sound = new Sound(AudioBuffer.FromWav("CreditsCutSound.wav"), 0.6f);
            sound.Stream.FrequencyRatio = 1f + displayIndex * 0.002f;
            if (Done)
                sound.Stream.FrequencyRatio *= 1.2f;

            scene.Audio.Play(sound);
        }
    }

    protected override void PrepareRenderSelf(RenderLayer layer)
    {
        base.PrepareRenderSelf(layer);

        if (Done)
            return;

        TextRenderer text = layer.OutputRenderer.Get<TextRenderer>();
        float scale = 0.25f;
        float spacingPerLine = font.Size * scale * 1.4f;

        Display currentDisplay = displays[displayIndex];
        Vector2 center = NorthernlightGame.BaseResolution * 0.25f;
        Vector2 top = center - Vector2.UnitY * spacingPerLine * currentDisplay.Lines.Length * 0.5f;
        Vector2 drawPosition = top;
        for (int i = 0; i < currentDisplay.Lines.Length; i++)
        {
            Line line = currentDisplay.Lines[i];
            Color color = Color.White;
            if (line.Title)
                color = new Color(127, 127, 127);

            text.Enqueue(line.Text.Value, font, new Vector3(drawPosition, 0f), color, Vector2.One * scale, Vector2.One * 0.5f);

            drawPosition.Y += spacingPerLine;
        }
    }
}
