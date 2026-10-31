using System.Numerics;
using System.Runtime.CompilerServices;
using SDL3;

namespace Northernlight.Core.InputHandling;

/// <summary>
///     A key-combo forming a single valid button combination for an action.
/// </summary>
/// <param name="Processors">
///     Any individual button that must be pressed.  This does not form a
///     combination of buttons.
/// </param>
/// <param name="Modifiers">
///     The modifiers necessary for this action.  At least one modifier from
///     each element must be pressed.  This allows for combining different
///     modifiers while keeping the directionality (left/right, etc.) of the
///     buttons agnostic (i.e., CTRL+SHIFT+Enter may allow left/right CTRL and
///     SHIFT, but still require at least one CTRL and one SHIFT button to be
///     pressed).
/// </param>
public readonly record struct InputIdentity(ButtonInputProcessor[] Processors, params SDL.Keymod[] Modifiers)
{
    public bool IsPressed => Processors.Any(processor => processor.IsPressed) && AreModifiersPressed();

    public bool WasJustPressed => Processors.Any(processor => processor.WasJustPressed) && AreModifiersPressed();

    public bool WasJustReleased => Processors.All(processor => processor.WasJustReleased) && AreModifiersPressed();

    public float PressDuration
    {
        get
        {
            if (!AreModifiersPressed())
                return 0f;

            return Processors.Max(p => p.PressDuration);
        }
    }

    public InputIdentity(ButtonInputProcessor processor, params SDL.Keymod[] modifiers) : this([processor], modifiers) { }

    private bool AreModifiersPressed()
    {
        return Modifiers.All(Input.AnyMods);
    }
}

/// <summary>
///     The system responsible for SDL poll interfacing
///     in handling inputs, as well as creating high level
///     abstraction for input management.
/// </summary>
public sealed class Input
{
    private enum Direction8
    {
        None = -1,
        Right = 0,
        DownRight = 1,
        Down = 2,
        DownLeft = 3,
        Left = 4,
        UpLeft = 5,
        Up = 6,
        UpRight = 7,
    }

    /// <summary>
    ///     Represents a contextual press state.
    /// </summary>
    internal struct PressState
    {
        public float PressDuration;
        public float PressIntervalTimer;
        public ButtonInputState State;
    }

    /// <summary>
    ///     The current contextual press states of the available scan keys.
    /// </summary>
    private static readonly PressState[] press_states = new PressState[
        (int)SDL.Scancode.Count
      + (int)MouseButton.Count
      + (int)SDL.GamepadButton.Count
      + (int)ControllerAxis.Count
    ];

    /// <summary>
    ///     The raw position of the mouse.
    /// </summary>
    public static Vector2 RawMousePosition
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of vertical motion registered from the mouse wheel.
    /// </summary>
    public static float MouseWheelMotion
    {
        get;
        internal set;
    }

    /// <summary>
    ///     The left mouse button processor.
    /// </summary>
    public static ButtonInputProcessor Lmb => new(MouseButton.Left);

    /// <summary>
    ///     The right mouse button processor.
    /// </summary>
    public static ButtonInputProcessor Rmb => new(MouseButton.Right);

    public static bool HasMods(SDL.Keymod modifiers)
    {
        return SDL.GetModState().HasFlag(modifiers);
    }

    public static bool AnyMods(SDL.Keymod modifiers)
    {
        return (SDL.GetModState() & modifiers) != 0;
    }

    private static nint? currentGamepad;
    private static bool hasDoneFirstPoll;

    /// <summary>
    ///     Polls SDL for keyboard state information.
    /// </summary>
    private static void Poll(Game game)
    {
        if (!hasDoneFirstPoll)
        {
            currentGamepad = FindGamepad();
            hasDoneFirstPoll = true;
        }

        /*
        ReadOnlySpan<bool> pressStates = SDL.GetKeyboardState(out int keyCount);
        for (int i = 0; i < keyCount; i++)
        {
            SDL.Scancode scanCode = (SDL.Scancode)i;
            ImmediatePressStates[scanCode] = pressStates[i];

            ButtonInputProcessor processor = Get(scanCode);
            processor.Process(game, pressStates[i]);
        }

        SDL.MouseButtonFlags flags = SDL.GetMouseState(out float x, out float y);
        Lmb.Process(game, flags.HasFlag(SDL.MouseButtonFlags.Left));
        Rmb.Process(game, flags.HasFlag(SDL.MouseButtonFlags.Right));
        RawMousePosition = new Vector2(x, y);
        */

        ReadOnlySpan<bool> pressStates = SDL.GetKeyboardState(out int keyCount);
        for (var i = 0; i < keyCount; i++)
        {
            UpdatePressState(game, ref GetPressState((SDL.Scancode)i), pressStates[i]);
        }

        SDL.MouseButtonFlags flags = SDL.GetMouseState(out float x, out float y);
        for (var i = MouseButton.Left; i < MouseButton.Count; i++)
        {
            UpdatePressState(game, ref GetPressState(i), flags.HasFlag((SDL.MouseButtonFlags)(1 << (int)i)));
        }

        for (var i = (SDL.GamepadButton)0; i < SDL.GamepadButton.Count; i++)
        {
            if (currentGamepad is not null)
            {
                UpdatePressState(game, ref GetPressState(i), SDL.GetGamepadButton(currentGamepad.Value, i));
            }
            else
            {
                UpdatePressState(game, ref GetPressState(i), pressed: false);
            }
        }

        if (currentGamepad is not null)
        {
            var leftStick = GetDirection8(currentGamepad.Value, right: false);
            var rightStick = GetDirection8(currentGamepad.Value, right: true);

            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickRight), pressed: leftStick is Direction8.Right or Direction8.DownRight or Direction8.UpRight);
            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickDown), pressed: leftStick is Direction8.Down or Direction8.DownLeft or Direction8.DownRight);
            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickLeft), pressed: leftStick is Direction8.Left or Direction8.DownLeft or Direction8.UpLeft);
            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickUp), pressed: leftStick is Direction8.Up or Direction8.UpLeft or Direction8.UpRight);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickRight), pressed: rightStick is Direction8.Right or Direction8.DownRight or Direction8.UpRight);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickDown), pressed: rightStick is Direction8.Down or Direction8.DownLeft or Direction8.DownRight);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickLeft), pressed: rightStick is Direction8.Left or Direction8.DownLeft or Direction8.UpLeft);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickUp), pressed: rightStick is Direction8.Up or Direction8.UpLeft or Direction8.UpRight);
        }
        else
        {
            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickRight), pressed: false);
            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickDown), pressed: false);
            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickLeft), pressed: false);
            UpdatePressState(game, ref GetPressState(ControllerAxis.LeftStickUp), pressed: false);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickRight), pressed: false);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickDown), pressed: false);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickLeft), pressed: false);
            UpdatePressState(game, ref GetPressState(ControllerAxis.RightStickUp), pressed: false);
        }

        RawMousePosition = new Vector2(x, y);
    }

    private static Direction8 GetDirection8(nint gamepad, bool right, float deadZone = 0.1f)
    {
        var x = SDL.GetGamepadAxis(gamepad, right ? SDL.GamepadAxis.RightX : SDL.GamepadAxis.LeftX) / 32767f;
        var y = SDL.GetGamepadAxis(gamepad, right ? SDL.GamepadAxis.RightY : SDL.GamepadAxis.LeftY) / 32767f;

        var length = float.Sqrt(x * x + y * y);
        if (length <= deadZone)
        {
            return Direction8.None;
        }

        var angle = float.Atan2(y, x);
        if (angle < 0)
        {
            angle += float.Tau;
        }

        var dir = (int)((angle + float.Pi / 8) / (float.Pi / 4)) & 7;
        return (Direction8)dir;
    }

    private static void UpdatePressState(Game game, ref PressState pressState, bool pressed)
    {
        bool wasPressed = pressState.State == ButtonInputState.Held || pressState.State == ButtonInputState.JustPressed;

        // The processor is still being pressed, and should be considered held.
        if (pressed && wasPressed)
        {
            pressState.State = ButtonInputState.Held;
            pressState.PressDuration += game.Time.DeltaTime;
            pressState.PressIntervalTimer += game.Time.DeltaTime;
        }

        // The processor has been pressed, but was not pressed before. That means that the processor should be considered JustPressed.
        if (pressed && !wasPressed)
            pressState.State = ButtonInputState.JustPressed;

        // The processor is not pressed at all and was not pressed before, and should be considered inactive.
        if (!pressed && !wasPressed)
        {
            pressState.State = ButtonInputState.Inactive;
            pressState.PressDuration = 0f;
        }

        // The processor was pressed, but isn't anymore, and should be considered JustReleased.
        if (!pressed && wasPressed)
        {
            pressState.State = ButtonInputState.JustReleased;
            pressState.PressDuration = 0f;
        }
    }

    /// <summary>
    ///     Processes incoming events for this
    ///     input handler.
    /// </summary>
    internal static void ProcessEvents(SDL.Event e, SDL.EventType type)
    {
        if (type == SDL.EventType.MouseWheel)
        {
            SDL.MouseWheelEvent mouseWheelEvent = e.Wheel;
            bool flippedDirection = mouseWheelEvent.Direction == SDL.MouseWheelDirection.Flipped;
            MouseWheelMotion = mouseWheelEvent.Y * (flippedDirection ? -1f : 1f);
        }
        else if (type == SDL.EventType.GamepadAdded)
        {
            currentGamepad ??= FindGamepad();
        }
        else if (type == SDL.EventType.GamepadRemoved)
        {
            if (currentGamepad is null)
            {
                return;
            }

            if (e.GDevice.Which == SDL.GetJoystickID(SDL.GetGamepadJoystick(currentGamepad.Value)))
            {
                SDL.CloseGamepad(currentGamepad.Value);
                currentGamepad = FindGamepad();
            }
        }
    }

    private static nint? FindGamepad()
    {
        var joysticks = SDL.GetJoysticks(out var joystickCount);
        if (joysticks is not null)
        {
            for (var i = 0; i < joystickCount; i++)
            {
                if (!SDL.IsGamepad(joysticks[i]))
                {
                    continue;
                }

                return SDL.OpenGamepad(joysticks[i]);
            }
        }

        return null;
    }

    /// <summary>
    ///     Processes all key states.
    /// </summary>
    public static void Process(Game game)
    {
        Poll(game);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ButtonInputProcessor Get(SDL.Scancode code)
    {
        return new ButtonInputProcessor(code);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ButtonInputProcessor Get(MouseButton code)
    {
        return new ButtonInputProcessor(code);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ButtonInputProcessor Get(SDL.GamepadButton code)
    {
        return new ButtonInputProcessor(code);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ButtonInputProcessor Get(ControllerAxis code)
    {
        return new ButtonInputProcessor(code);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PressState GetPressState(ButtonKeyKind keyKind, int keyIndex)
    {
        return ref press_states[GetIndex(keyKind, keyIndex)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PressState GetPressState(SDL.Scancode code)
    {
        return ref press_states[GetIndex(ButtonKeyKind.Keyboard, (int)code)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PressState GetPressState(MouseButton code)
    {
        return ref press_states[GetIndex(ButtonKeyKind.MouseButton, (int)code)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PressState GetPressState(SDL.GamepadButton code)
    {
        return ref press_states[GetIndex(ButtonKeyKind.ControllerButton, (int)code)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PressState GetPressState(ControllerAxis code)
    {
        return ref press_states[GetIndex(ButtonKeyKind.ControllerAxis, (int)code)];
    }

    private static readonly int[] index_offsets =
    [
        0,                                                                               // keyboard
        (int)SDL.Scancode.Count,                                                         // mouse
        (int)SDL.Scancode.Count + (int)MouseButton.Count,                                // controller button
        (int)SDL.Scancode.Count + (int)MouseButton.Count + (int)SDL.GamepadButton.Count, // controller axis
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndex(ButtonKeyKind keyKind, int keyIndex)
    {
        return index_offsets[(int)keyKind] + keyIndex;
    }
}
