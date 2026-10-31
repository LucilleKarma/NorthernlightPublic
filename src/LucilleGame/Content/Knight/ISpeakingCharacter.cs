using System.Numerics;
using Northernlight.Core.Rendering;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents a kind of character that can
///     speak during the battle.
/// </summary>
public interface ISpeakingCharacter
{
    /// <summary>
    ///     The file name for the text sound to play
    ///     when this character speaks.
    /// </summary>
    /// <remarks>
    ///     If this is null or empty, no sound is played
    ///     with the dialogue.
    /// </remarks>
    public string? TextSoundFileName
    {
        get;
    }

    /// <summary>
    ///     The speaker's position. Used for determining
    ///     where dialogue boxes should be placed.
    /// </summary>
    public Vector2 SpeakerPosition
    {
        get;
    }

    /// <summary>
    ///     Prepares the text and its corresponding box for rendering.
    /// </summary>
    void PrepareRenderTextAndBox(SpriteRenderer sprites, TextRenderer text, Vector2 start, int direction, string spokenText, string fullText);
}
