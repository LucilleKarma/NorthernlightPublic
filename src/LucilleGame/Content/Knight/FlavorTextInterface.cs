using System.Numerics;
using System.Text;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight;

public sealed class FlavorTextInterface : BattleMenuBottomsideOverlay
{
    private string textToSpellOut = string.Empty;

    private string spelledOutText = string.Empty;

    private string previousText = string.Empty;

    private bool firstTurn = true;

    private bool displayedHesitationText;

    private OwnedTimer spellOutTimer;

    private Font font;

    /// <summary>
    ///     The back-shadow color for displayed text.
    /// </summary>
    public static Color BackShadowColor => new Color(23, 23, 105);

    public FlavorTextInterface(BattleMenu menu) : base(menu)
    {
        RerollText();
    }

    /// <summary>
    ///     Rerolls the visible text.
    /// </summary>
    public void RerollText()
    {
        spelledOutText = string.Empty;
        spellOutTimer?.Reset();

        bool knightIsHesitating = true;
        List<LanguageAgnosticText> possibilities = [];
        foreach (Hero hero in Menu.Scene.Heroes.All)
        {
            if (hero.HP >= 1 && hero != Menu.Scene.Heroes.Noelle)
            {
                knightIsHesitating = false;
                break;
            }
        }
        if (Menu.Scene.SideB)
            knightIsHesitating = false;

        if (firstTurn)
        {
            firstTurn = false;
            possibilities.Add(LocalizationEntries.BattleFlavorTextStart);
        }
        else if (!displayedHesitationText && knightIsHesitating)
        {
            displayedHesitationText = true;
            possibilities.Add(LocalizationEntries.BattleFlavorTextHesitation);
        }
        else if (Menu.Scene.BattleHandler.Attack is not null)
            possibilities.Add(Menu.Scene.BattleHandler.Attack.MenuIndicatorFlavorText);

        if (possibilities.Count >= 1)
        {
            for (int i = 0; i < 50; i++)
            {
                textToSpellOut = possibilities[Menu.Scene.Rng.Next(possibilities.Count)].Value;
                if (textToSpellOut != previousText)
                    break;
            }
        }
        previousText = textToSpellOut;

        textToSpellOut = $"* {textToSpellOut}";
    }

    public override void Update()
    {
        spellOutTimer ??= Menu.Scene.Time.Rent();
        if (spellOutTimer.Cycle(0.01667f))
        {
            if (spelledOutText != textToSpellOut && spelledOutText.Length < textToSpellOut.Length)
            {
                Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("TextSpellout.wav")));

                spelledOutText += textToSpellOut[spelledOutText.Length];
            }
        }
    }

    internal static bool ValidTextSplitter(char c)
    {
        return char.IsWhiteSpace(c) || c == '.';
    }

    /// <summary>
    ///     Calculates all indices to insert newlines in
    ///     a text/
    /// </summary>
    internal static HashSet<int> CalculateLineWrapIndices(string text, Font font, TextRenderer textRenderer, Vector2 maxArea, float textScale)
    {
        StringBuilder fullText = new StringBuilder();

        float movingTextWidth = 0f;
        HashSet<int> indices = [];

        for (int i = 0; i < text.Length; i++)
        {
            Vector2 textSize = textRenderer.MeasureString(text[i].ToString(), font) * textScale;
            movingTextWidth += textSize.X;
            if (movingTextWidth >= maxArea.X || text[i] == '\n')
            {
                // Move back to the last space, and insert
                // a line break there.
                // This looks far, far more natural than inserting
                // a line break in the middle of a word and
                // cutting it in two.
                int lastSpaceIndex = fullText.Length - 1;

                if (text[i] != '\n')
                {
                    while (lastSpaceIndex >= 0)
                    {
                        if (ValidTextSplitter(fullText[lastSpaceIndex]))
                            break;
                        lastSpaceIndex--;
                    }
                }
                else
                    lastSpaceIndex++;

                indices.Add(lastSpaceIndex);

                movingTextWidth = 0f;
            }
            fullText.Append(text[i]);
        }

        return indices;
    }

    public override void PrepareRender(SpriteRenderer sprites, TextRenderer textRenderer, Vector3 center, Vector2 size)
    {
        font ??= new Font("8bitoperator.ttf", 64f);

        float scale = 0.85f;
        Vector2 maxArea = size * new Vector2(0.725f, 0.8f);

        float verticalSpacing = font.Size * scale * 0.9f;

        int characterIndex = 0;
        HashSet<int> lineWrapIndices = CalculateLineWrapIndices(textToSpellOut, font, textRenderer, maxArea, scale);
        Vector2 start = center.AsVector2() + new Vector2(-0.45f, -0.4f) * size;
        Vector2 drawPosition = start;
        float newlineX = start.Y;

        for (int i = 0; i < spelledOutText.Length; i++)
        {
            char c = spelledOutText[i];
            textRenderer.Enqueue(c.ToString(), font, new Vector3(drawPosition, center.Z + 1f), Color.White, Vector2.One * scale);

            Vector2 backShadowOffset = Vector2.One * font.Size * scale * 0.04f;
            textRenderer.Enqueue(c.ToString(), font, new Vector3(drawPosition + backShadowOffset, center.Z), BackShadowColor, Vector2.One * scale);

            Vector2 textSize = textRenderer.MeasureString(c.ToString(), font);
            drawPosition.X += (textSize.X + 4f) * scale;
            if (c == ' ')
                drawPosition.X += scale * 12f;

            if (lineWrapIndices.Contains(characterIndex))
            {
                drawPosition.X = newlineX;
                drawPosition.Y += verticalSpacing;
            }
            if (i == 1)
                newlineX = drawPosition.X;

            characterIndex++;
        }
    }
}
