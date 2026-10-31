using System.Numerics;
using Northernlight.Content.Knight.Dialogue;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents a dialogue exchange that can occur during
///     battle. This can include multiple speakers.
/// </summary>
public sealed class BattleDialogueExchange
{
    private int lineIndex;

    private string spokenText = string.Empty;

    private readonly OwnedTimer spellOutTimer;

    /// <summary>
    ///     Whether this exchange is finished due to
    ///     all text having been read.
    /// </summary>
    public bool Done
    {
        get;
        private set;
    }

    /// <summary>
    ///     The set of all dialogue lines to say.
    /// </summary>
    public readonly CharacterDialogueLine[] LinesToSay = [];

    /// <summary>
    ///     The game handling this exchange.
    /// </summary>
    public readonly KnightFightScene Scene;

    public BattleDialogueExchange(KnightFightScene scene, CharacterDialogueLine[] linesToSay)
    {
        Scene = scene;
        LinesToSay = linesToSay;
        spellOutTimer = scene.Time.Rent();
    }

    /// <summary>
    ///     Updates this dialogue exchange.
    /// </summary>
    public void Update()
    {
        if (Done)
            return;

        CharacterDialogueLine line = LinesToSay[lineIndex];
        if (ActiveKeymap.Confirm.WasJustPressed)
        {
            spellOutTimer.Reset();

            if (spokenText == line.FullText.Value)
            {
                lineIndex++;
                spokenText = string.Empty;

                if (lineIndex >= LinesToSay.Length)
                {
                    Done = true;
                    return;
                }
            }
            else
                spokenText = line.FullText.Value;
        }

        float spellOutRate = 1f / 60f;
        if (spokenText != line.FullText.Value && line.FullText.Value[spokenText.Length] == CharacterDialogueHandler.PauseIndicator)
            spellOutRate = 1.1f;

        if (spokenText != line.FullText.Value && spellOutTimer.Elapsed(spellOutRate))
            AdvanceCharacter();
    }

    private void AdvanceCharacter()
    {
        CharacterDialogueLine line = LinesToSay[lineIndex];
        spokenText += line.FullText.Value[spokenText.Length];

        if (!string.IsNullOrEmpty(line.Speaker.TextSoundFileName))
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav(line.Speaker.TextSoundFileName), 0.5f));
    }

    /// <summary>
    ///     Prepares this exchange for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, TextRenderer text)
    {
        if (Done)
            return;

        CharacterDialogueLine line = LinesToSay[lineIndex];
        int direction = (line.Speaker.SpeakerPosition.X < NorthernlightGame.KnightFightResolution.X * 0.5f) ? 1 : -1;
        Vector2 start = line.Speaker.SpeakerPosition + Vector2.UnitX * direction * 100f;
        line.Speaker.PrepareRenderTextAndBox(sprites, text, start, direction, spokenText, line.FullText.Value);
    }
}
