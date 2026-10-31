using SDL3;

namespace Northernlight.Core.InputHandling;

/// <summary>
///     Represents the state of a given keyboard button input.
/// </summary>
public enum ButtonInputState : byte
{
    Inactive,
    Held,
    JustPressed,
    JustReleased,
}

/// <summary>
///     The button key kind.
/// </summary>
public enum ButtonKeyKind
{
    /// <summary>
    ///     A button on the keyboard.
    /// </summary>
    Keyboard,

    /// <summary>
    ///     A mouse button.
    /// </summary>
    MouseButton,

    /// <summary>
    ///     A button on a controller.
    /// </summary>
    ControllerButton,

    /// <summary>
    ///     An axis on a controller.
    /// </summary>
    ControllerAxis,
}

public enum MouseButton
{
    Left,
    Middle,
    Right,
    X1,
    X2,
    Count,
}

public enum ControllerAxis
{
    LeftStickRight,
    LeftStickDown,
    LeftStickLeft,
    LeftStickUp,
    RightStickRight,
    RightStickDown,
    RightStickLeft,
    RightStickUp,
    Count,
}

/// <summary>
///     Represents a well-formed view into an <see cref="Input"/> state.
/// </summary>
public readonly struct ButtonInputProcessor
{
    private readonly int keyIndex;

    internal readonly ButtonKeyKind keyKind;

    private ButtonInputProcessor(ButtonKeyKind keyKind, int keyIndex)
    {
        this.keyKind = keyKind;
        this.keyIndex = keyIndex;
    }

    public ButtonInputProcessor(SDL.Scancode scancode) : this(ButtonKeyKind.Keyboard, (int)scancode) { }

    public ButtonInputProcessor(MouseButton code) : this(ButtonKeyKind.MouseButton, (int)code) { }

    public ButtonInputProcessor(SDL.GamepadButton code) : this(ButtonKeyKind.ControllerButton, (int)code) { }

    public ButtonInputProcessor(ControllerAxis code) : this(ButtonKeyKind.ControllerAxis, (int)code) { }

    /// <summary>
    ///     The state of this processor.
    /// </summary>
    public ButtonInputState State => Input.GetPressState(keyKind, keyIndex).State;

    /// <summary>
    ///     How long this key has been pressed for, in seconds.
    /// </summary>
    public float PressDuration => Input.GetPressState(keyKind, keyIndex).PressDuration;

    /// <summary>
    ///     Refer to <see cref="PressOrHeld"/> for details on this.
    /// </summary>
    public float PressIntervalTimer => Input.GetPressState(keyKind, keyIndex).PressIntervalTimer;

    /// <summary>
    ///     Determines whether this processor is being pressed.
    /// </summary>
    public bool IsPressed => State == ButtonInputState.Held || State == ButtonInputState.JustPressed;

    /// <summary>
    ///     Determines whether this processor was just pressed.
    /// </summary>
    public bool WasJustPressed => State == ButtonInputState.JustPressed;

    /// <summary>
    ///     Determines whether this processor was just released.
    /// </summary>
    public bool WasJustReleased => State == ButtonInputState.JustReleased;

    /// <summary>
    ///     Returns whether this processor was
    ///     just processed or has been held for
    ///     long enough to be considered pressed
    ///     down by the user.
    /// </summary>
    /// <remarks>
    ///     This method has the ability to make
    ///     events only fire on an interval even when held.
    ///     <br></br>
    ///     
    ///     Consider a user holding down the S key for one
    ///     second and being considered to have it press
    ///     down for an input editor.
    ///     <br></br>
    ///     
    ///     Ideally you wouldn't want the player to get 500
    ///     new keys per second if their FPS is 500, and 
    ///     only 60 if their FPS is 60. You'd want a limit of some
    ///     kind.
    /// </remarks>
    /// <param name="holdThreshold">How long the key has to be held for in order to be eligible for hold-down consideration.</param>
    /// <param name="holdActionRate">The rate at which the hold down action should fire. This exists to ensure framerate consistency when holding down. Refer to remarks for details.</param>
    public bool PressOrHeld(float holdThreshold, float holdActionRate = 0f)
    {
        if (WasJustPressed)
            return true;

        if (State == ButtonInputState.Held && PressDuration >= holdThreshold)
        {
            if (PressIntervalTimer >= holdActionRate)
            {
                Input.GetPressState(keyKind, keyIndex).PressIntervalTimer = 0f;
                return true;
            }
        }

        return false;
    }
}
