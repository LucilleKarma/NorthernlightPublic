using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight;

public sealed class ManualTextInterface : BattleMenuBottomsideOverlay
{
    private int lineIndex;

    private Font font;

    private OwnedTimer spellOutTimer;

    private string spelledOutText = string.Empty;

    private readonly Action<ManualTextInterface> onClose;

    private readonly string[] textsToSpellOut = [];

    public override bool RequiresInput => true;

    /// <summary>
    ///     The character used to indicate a pause.
    /// </summary>
    public const char PauseCharacter = '&';

    public ManualTextInterface(BattleMenu menu, Action<ManualTextInterface> onClose, bool automaticallyInsertAsterisks, params string[] lines) : base(menu)
    {
        this.onClose = onClose;
        textsToSpellOut = lines;

        if (automaticallyInsertAsterisks)
        {
            for (int i = 0; i < textsToSpellOut.Length; i++)
                textsToSpellOut[i] = $"* {textsToSpellOut[i]}";
        }
    }

    public override void Update()
    {
        string textToSpellOut = textsToSpellOut[lineIndex];

        spellOutTimer ??= Menu.Scene.Time.Rent();
        while (spellOutTimer.CycleWhile(CalculateSpellOutDelay(textToSpellOut)))
        {
            if (spelledOutText != textToSpellOut && spelledOutText.Length < textToSpellOut.Length)
            {
                Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("TextSpellout.wav")));

                spelledOutText += textToSpellOut[spelledOutText.Length];
            }
        }

        if (!justShowedUp)
        {
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                if (lineIndex >= textsToSpellOut.Length - 1 && spelledOutText == textToSpellOut)
                {
                    onClose?.Invoke(this);
                    Pop();
                }

                if (spelledOutText != textToSpellOut)
                    spelledOutText = textToSpellOut;
                else if (lineIndex < textsToSpellOut.Length - 1)
                {
                    spelledOutText = string.Empty;
                    lineIndex++;
                }
            }
        }
        else
            justShowedUp = false;
    }

    private float CalculateSpellOutDelay(string textToSpellOut)
    {
        if (spelledOutText.Length < textToSpellOut.Length - 1)
        {
            char nextCharacter = textToSpellOut[spelledOutText.Length];
            if (nextCharacter == PauseCharacter)
                return 0.8f;
        }

        return 0.01667f;
    }

    public override void PrepareRender(SpriteRenderer sprites, TextRenderer textRenderer, Vector3 center, Vector2 size)
    {
        font ??= new Font("8bitoperator.ttf", 64f);

        float scale = 0.85f;
        Vector2 maxArea = size * new Vector2(0.725f, 0.8f);

        float verticalSpacing = font.Size * scale * 0.9f;

        int characterIndex = 0;
        HashSet<int> lineWrapIndices = FlavorTextInterface.CalculateLineWrapIndices(textsToSpellOut[lineIndex], font, textRenderer, maxArea, scale);
        Vector2 start = center.AsVector2() + new Vector2(-0.45f, -0.4f) * size;
        Vector2 drawPosition = start;
        float newlineX = start.X;

        for (int i = 0; i < spelledOutText.Length; i++)
        {
            char c = spelledOutText[i];

            if (c != PauseCharacter)
            {
                textRenderer.Enqueue(c.ToString(), font, new Vector3(drawPosition, center.Z + 1f), Color.White, Vector2.One * scale);

                Vector2 backShadowOffset = Vector2.One * font.Size * scale * 0.04f;
                textRenderer.Enqueue(c.ToString(), font, new Vector3(drawPosition + backShadowOffset, center.Z), FlavorTextInterface.BackShadowColor, Vector2.One * scale);
            }

            Vector2 textSize = textRenderer.MeasureString(c.ToString(), font);
            drawPosition.X += (textSize.X + 4f) * scale;
            if (c == ' ')
                drawPosition.X += scale * 12f;

            if (lineWrapIndices.Contains(characterIndex))
            {
                drawPosition.X = newlineX;
                drawPosition.Y += verticalSpacing;
            }

            characterIndex++;
        }
    }
}
