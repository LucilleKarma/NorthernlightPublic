using System.Text;
using SDL3;

namespace Northernlight.Core.InputHandling;

public static class InputTextProcessor
{
    public delegate char StringSelector(bool holdingShift);

    /// <summary>
    ///     The set of all key to text edit mappings.
    /// </summary>
    public static readonly Dictionary<SDL.Scancode, StringSelector> KeyMappings = new Dictionary<SDL.Scancode, StringSelector>()
    {
        [SDL.Scancode.Alpha0] = Adder('0', ')'),
        [SDL.Scancode.Alpha1] = Adder('1', '!'),
        [SDL.Scancode.Alpha2] = Adder('2', '@'),
        [SDL.Scancode.Alpha3] = Adder('3', '#'),
        [SDL.Scancode.Alpha4] = Adder('4', '$'),
        [SDL.Scancode.Alpha5] = Adder('5', '%'),
        [SDL.Scancode.Alpha6] = Adder('6', '^'),
        [SDL.Scancode.Alpha7] = Adder('7', '&'),
        [SDL.Scancode.Alpha8] = Adder('8', '*'),
        [SDL.Scancode.Alpha9] = Adder('9', '('),

        [SDL.Scancode.Period] = Adder('.', '>'),
        [SDL.Scancode.Comma] = Adder(',', '<'),
        [SDL.Scancode.Semicolon] = Adder(';', ':'),
        [SDL.Scancode.Leftbracket] = Adder('[', '{'),
        [SDL.Scancode.Rightbracket] = Adder(']', '}'),

        [SDL.Scancode.Minus] = Adder('-', '_'),

        [SDL.Scancode.Slash] = Adder('/', '?'),

        [SDL.Scancode.Space] = Adder(' ', ' '),

        [SDL.Scancode.Apostrophe] = Adder('\'', '\"'),

        [SDL.Scancode.A] = Adder('a', 'A'),
        [SDL.Scancode.B] = Adder('b', 'B'),
        [SDL.Scancode.C] = Adder('c', 'C'),
        [SDL.Scancode.D] = Adder('d', 'D'),
        [SDL.Scancode.E] = Adder('e', 'E'),
        [SDL.Scancode.F] = Adder('f', 'F'),
        [SDL.Scancode.G] = Adder('g', 'G'),
        [SDL.Scancode.H] = Adder('h', 'H'),
        [SDL.Scancode.I] = Adder('i', 'I'),
        [SDL.Scancode.J] = Adder('j', 'J'),
        [SDL.Scancode.K] = Adder('k', 'K'),
        [SDL.Scancode.L] = Adder('l', 'L'),
        [SDL.Scancode.M] = Adder('m', 'M'),
        [SDL.Scancode.N] = Adder('n', 'N'),
        [SDL.Scancode.O] = Adder('o', 'O'),
        [SDL.Scancode.P] = Adder('p', 'P'),
        [SDL.Scancode.Q] = Adder('q', 'Q'),
        [SDL.Scancode.R] = Adder('r', 'R'),
        [SDL.Scancode.S] = Adder('s', 'S'),
        [SDL.Scancode.T] = Adder('t', 'T'),
        [SDL.Scancode.U] = Adder('u', 'U'),
        [SDL.Scancode.V] = Adder('v', 'V'),
        [SDL.Scancode.W] = Adder('w', 'W'),
        [SDL.Scancode.X] = Adder('x', 'X'),
        [SDL.Scancode.Y] = Adder('y', 'Y'),
        [SDL.Scancode.Z] = Adder('z', 'Z')
    };

    /// <summary>
    ///     How long a button needs to be held down
    ///     before text can be spammed.
    /// </summary>
    public const float StandardTextHoldRequirement = 0.5f;

    /// <summary>
    ///     The maximum amount of keys that can be spammed
    ///     per second on hold.
    /// </summary>
    public const int MaxHoldCharactersPerSecond = 60;

    /// <summary>
    ///     Creates a simple, standard <see cref="StringSelector"/>
    ///     that returns two sets of characters
    ///     based on whether the user is holding shift or not.
    /// </summary>
    public static StringSelector Adder(char standardCharacter, char charWithShift)
    {
        return new StringSelector(holdingShift =>
        {
            if (holdingShift)
                return charWithShift;

            return standardCharacter;
        });
    }

    /// <summary>
    ///     Acquires the current text input.
    /// </summary>
    public static string GetInput()
    {
        StringBuilder sb = new StringBuilder();

        bool holdingShift = Input.AnyMods(SDL.Keymod.Shift);
        foreach (var kv in KeyMappings)
        {
            ButtonInputProcessor key = Input.Get(kv.Key);
            if (key.PressOrHeld(StandardTextHoldRequirement, 1f / MaxHoldCharactersPerSecond))
            {
                char c = kv.Value(holdingShift);
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
