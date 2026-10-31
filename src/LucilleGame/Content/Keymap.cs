using System.Collections.Frozen;
using IniParser.Model;
using Northernlight.Content.SaveData.SurveyReader;
using Northernlight.Core.InputHandling;
using SDL3;

namespace Northernlight.Content;

public sealed class Keymap
{
    public required InputIdentity Down { get; init; }

    public required InputIdentity Right { get; init; }

    public required InputIdentity Up { get; init; }

    public required InputIdentity Left { get; init; }

    public required InputIdentity Confirm { get; init; }

    public required InputIdentity Cancel { get; init; }

    public required InputIdentity Menu { get; init; }
}

public static class ActiveKeymap
{
    private enum GmGamepadKey
    {
        gp_face1 = 32769, // xbox a
        gp_face2 = 32770, // xbox b
        gp_face3 = 32771, // xbox x
        gp_face4 = 32772, // xbox y
        gp_shoulderl = 32773,
        gp_shoulderlb = 32775,
        gp_shoulderr = 32774,
        gp_shoulderrb = 32776,
        gp_select = 32777,
        gp_start = 32778,
        gp_stickl = 32779,
        gp_stickr = 32780,
        gp_padu = 32781,
        gp_padd = 32782,
        gp_padl = 32783,
        gp_padr = 32784,
        gp_axislh = 32785,
        gp_axislv = 32786,
        gp_axisrh = 32787,
        gp_axisrv = 32788,
        gp_axis_acceleration_x = 32789,
        gp_axis_acceleration_y = 32790,
        gp_axis_acceleration_z = 32791,
        gp_axis_angular_velocity_x = 32792,
        gp_axis_angular_velocity_y = 32793,
        gp_axis_angular_velocity_z = 32794,
        gp_axis_orientation_x = 32795,
        gp_axis_orientation_y = 32796,
        gp_axis_orientation_z = 32797,
        gp_axis_orientation_w = 32798,
        gp_home = 32799,
        gp_extra1 = 32800,
        gp_extra2 = 32801,
        gp_extra3 = 32802,
        gp_extra4 = 32803,
        gp_paddler = 32804,
        gp_paddlel = 32805,
        gp_paddlerb = 32806,
        gp_paddlelb = 32807,
        gp_touchpadbutton = 32808,
        gp_extra5 = 32809,
        gp_extra6 = 32810,
    }

    private static readonly FrozenDictionary<string, SDL.Scancode> GmToScancode = new Dictionary<string, SDL.Scancode>
    {
        ["65"] = SDL.Scancode.A,
        ["66"] = SDL.Scancode.B,
        ["67"] = SDL.Scancode.C,
        ["68"] = SDL.Scancode.D,
        ["69"] = SDL.Scancode.E,
        ["70"] = SDL.Scancode.F,
        ["71"] = SDL.Scancode.G,
        ["72"] = SDL.Scancode.H,
        ["73"] = SDL.Scancode.I,
        ["74"] = SDL.Scancode.J,
        ["75"] = SDL.Scancode.K,
        ["76"] = SDL.Scancode.L,
        ["77"] = SDL.Scancode.M,
        ["78"] = SDL.Scancode.N,
        ["79"] = SDL.Scancode.O,
        ["80"] = SDL.Scancode.P,
        ["81"] = SDL.Scancode.Q,
        ["82"] = SDL.Scancode.R,
        ["83"] = SDL.Scancode.S,
        ["84"] = SDL.Scancode.T,
        ["85"] = SDL.Scancode.U,
        ["86"] = SDL.Scancode.V,
        ["87"] = SDL.Scancode.W,
        ["88"] = SDL.Scancode.X,
        ["89"] = SDL.Scancode.Y,
        ["90"] = SDL.Scancode.Z,
        ["48"] = SDL.Scancode.Alpha0,
        ["49"] = SDL.Scancode.Alpha1,
        ["50"] = SDL.Scancode.Alpha2,
        ["51"] = SDL.Scancode.Alpha3,
        ["52"] = SDL.Scancode.Alpha4,
        ["53"] = SDL.Scancode.Alpha5,
        ["54"] = SDL.Scancode.Alpha6,
        ["55"] = SDL.Scancode.Alpha7,
        ["56"] = SDL.Scancode.Alpha8,
        ["57"] = SDL.Scancode.Alpha9,
        ["37"] = SDL.Scancode.Left,
        ["38"] = SDL.Scancode.Up,
        ["39"] = SDL.Scancode.Right,
        ["40"] = SDL.Scancode.Down,
        ["13"] = SDL.Scancode.Return,
        ["8"] = SDL.Scancode.Backspace,
        ["9"] = SDL.Scancode.Tab,
        ["20"] = SDL.Scancode.Capslock,
        ["27"] = SDL.Scancode.Escape,
        ["32"] = SDL.Scancode.Space,
        ["16"] = SDL.Scancode.LShift,
        ["17"] = SDL.Scancode.LCtrl,
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, SDL.GamepadButton> GmToGamepad = new Dictionary<string, SDL.GamepadButton>
    {
        [((int)GmGamepadKey.gp_face1).ToString()] = SDL.GamepadButton.South,
        [((int)GmGamepadKey.gp_face2).ToString()] = SDL.GamepadButton.East,
        [((int)GmGamepadKey.gp_face3).ToString()] = SDL.GamepadButton.West,
        [((int)GmGamepadKey.gp_face4).ToString()] = SDL.GamepadButton.North,
        [((int)GmGamepadKey.gp_shoulderl).ToString()] = SDL.GamepadButton.LeftShoulder,
        [((int)GmGamepadKey.gp_shoulderlb).ToString()] = default, // TODO: this is an axis
        [((int)GmGamepadKey.gp_shoulderr).ToString()] = SDL.GamepadButton.RightShoulder,
        [((int)GmGamepadKey.gp_shoulderrb).ToString()] = default, // TODO: this is an axis
        [((int)GmGamepadKey.gp_select).ToString()] = SDL.GamepadButton.Back, // I think this is Select
        [((int)GmGamepadKey.gp_start).ToString()] = SDL.GamepadButton.Start,
        [((int)GmGamepadKey.gp_stickl).ToString()] = SDL.GamepadButton.LeftStick,
        [((int)GmGamepadKey.gp_stickr).ToString()] = SDL.GamepadButton.RightStick,
        [((int)GmGamepadKey.gp_padu).ToString()] = SDL.GamepadButton.DPadUp,
        [((int)GmGamepadKey.gp_padd).ToString()] = SDL.GamepadButton.DPadDown,
        [((int)GmGamepadKey.gp_padl).ToString()] = SDL.GamepadButton.DPadLeft,
        [((int)GmGamepadKey.gp_padr).ToString()] = SDL.GamepadButton.DPadRight,
        /*
        [nameof(GmGamepadKey.gp_axislh)] = default,
        [nameof(GmGamepadKey.gp_axislv)] = default,
        [nameof(GmGamepadKey.gp_axisrh)] = default,
        [nameof(GmGamepadKey.gp_axisrv)] = default,
        [nameof(GmGamepadKey.gp_axis_acceleration_x)] = default,
        [nameof(GmGamepadKey.gp_axis_acceleration_y)] = default,
        [nameof(GmGamepadKey.gp_axis_acceleration_z)] = default,
        [nameof(GmGamepadKey.gp_axis_angular_velocity_x)] = default,
        [nameof(GmGamepadKey.gp_axis_angular_velocity_y)] = default,
        [nameof(GmGamepadKey.gp_axis_angular_velocity_z)] = default,
        [nameof(GmGamepadKey.gp_axis_orientation_x)] = default,
        [nameof(GmGamepadKey.gp_axis_orientation_y)] = default,
        [nameof(GmGamepadKey.gp_axis_orientation_z)] = default,
        [nameof(GmGamepadKey.gp_axis_orientation_w)] = default,
        */
        [nameof(GmGamepadKey.gp_home)] = SDL.GamepadButton.Guide,
        [nameof(GmGamepadKey.gp_extra1)] = SDL.GamepadButton.Misc1,
        [nameof(GmGamepadKey.gp_extra2)] = SDL.GamepadButton.Misc2,
        [nameof(GmGamepadKey.gp_extra3)] = SDL.GamepadButton.Misc3,
        [nameof(GmGamepadKey.gp_extra4)] = SDL.GamepadButton.Misc4,
        [nameof(GmGamepadKey.gp_paddler)] = SDL.GamepadButton.RightPaddle1,
        [nameof(GmGamepadKey.gp_paddlel)] = SDL.GamepadButton.LeftPaddle1,
        [nameof(GmGamepadKey.gp_paddlerb)] = SDL.GamepadButton.RightPaddle2,
        [nameof(GmGamepadKey.gp_paddlelb)] = SDL.GamepadButton.LeftPaddle2,
        [nameof(GmGamepadKey.gp_touchpadbutton)] = SDL.GamepadButton.Touchpad,
        [nameof(GmGamepadKey.gp_extra5)] = SDL.GamepadButton.Misc5,
        [nameof(GmGamepadKey.gp_extra6)] = SDL.GamepadButton.Misc6,
    }.ToFrozenDictionary();

    /// <summary>
    ///     The default keymap, serving as a fallback if no other keymap is
    ///     loaded.
    /// </summary>
    internal static readonly Keymap default_keyboard_keymap = new()
    {
        Down = new InputIdentity([Input.Get(SDL.Scancode.Down), Input.Get(SDL.GamepadButton.DPadDown), Input.Get(ControllerAxis.LeftStickDown)]),
        Right = new InputIdentity([Input.Get(SDL.Scancode.Right), Input.Get(SDL.GamepadButton.DPadRight), Input.Get(ControllerAxis.LeftStickRight)]),
        Up = new InputIdentity([Input.Get(SDL.Scancode.Up), Input.Get(SDL.GamepadButton.DPadUp), Input.Get(ControllerAxis.LeftStickUp)]),
        Left = new InputIdentity([Input.Get(SDL.Scancode.Left), Input.Get(SDL.GamepadButton.DPadLeft), Input.Get(ControllerAxis.LeftStickLeft)]),
        Confirm = new InputIdentity([Input.Get(SDL.Scancode.Z), Input.Get(SDL.GamepadButton.East)]),
        Cancel = new InputIdentity([Input.Get(SDL.Scancode.X), Input.Get(SDL.GamepadButton.South)]),
        Menu = new InputIdentity([Input.Get(SDL.Scancode.C), Input.Get(SDL.GamepadButton.North)]),
    };

    /// <summary>
    ///     The keymap currently in use. Will always be a valid keymap
    ///     instance.
    /// </summary>
    internal static Keymap Instance
    {
        get => field ?? default_keyboard_keymap;
        set;
    }

    private static readonly ButtonInputProcessor[] confirm_processors =
    [
        default, // keyboard
        default, // controller
        Input.Get(SDL.Scancode.Return),
    ];

    private static readonly ButtonInputProcessor[] cancel_processors =
    [
        default, // keyboard
        default, // controller
        Input.Get(SDL.Scancode.LShift),
        Input.Get(SDL.Scancode.RShift),
    ];

    private static readonly ButtonInputProcessor[] menu_processors =
    [
        default, // keyboard
        default, // controller
        Input.Get(SDL.Scancode.LCtrl),
        Input.Get(SDL.Scancode.RCtrl),
    ];

    public static InputIdentity Down => Instance.Down;

    public static InputIdentity Right => Instance.Right;

    public static InputIdentity Up => Instance.Up;

    public static InputIdentity Left => Instance.Left;

    public static InputIdentity Confirm
    {
        get
        {
            confirm_processors[0] = Instance.Confirm.Processors[0];
            confirm_processors[1] = Instance.Confirm.Processors[1];
            return new InputIdentity(confirm_processors, Instance.Confirm.Modifiers);
        }
    }

    public static InputIdentity Cancel
    {
        get
        {
            cancel_processors[0] = Instance.Cancel.Processors[0];
            cancel_processors[1] = Instance.Cancel.Processors[1];
            return new InputIdentity(cancel_processors, Instance.Confirm.Modifiers);
        }
    }

    public static InputIdentity Menu
    {
        get
        {
            menu_processors[0] = Instance.Menu.Processors[0];
            menu_processors[1] = Instance.Menu.Processors[1];
            return new InputIdentity(menu_processors, Instance.Menu.Modifiers);
        }
    }

    public static InputIdentity ChangeWindowMode { get; } = new(Input.Get(SDL.Scancode.Return), SDL.Keymod.Alt);

    public static InputIdentity ChangeWindowModeAlt { get; } = new(Input.Get(SDL.Scancode.F4));

    public static InputIdentity Quit { get; } = new(Input.Get(SDL.Scancode.Escape));

    public static InputIdentity Run => Cancel;

    internal static void Load(DeltaruneSaveProvider save)
    {
        var ini = default(IniData?);
        for (var i = 5; i >= 0; i--)
        {
            if (!save.TryGetIni($"keyconfig_{i}.ini", out ini))
                continue;
        }

        if (ini is null)
            return;

        KeyDataCollection keySection = ini.Sections["KEYBOARD_CONTROLS"];

        string ParseKey(int index) => new string(keySection[index.ToString()].Split('.')[0].Trim("\""));

        SDL.Scancode down = GmToScancode[ParseKey(0)];
        SDL.Scancode right = GmToScancode[ParseKey(1)];
        SDL.Scancode up = GmToScancode[ParseKey(2)];
        SDL.Scancode left = GmToScancode[ParseKey(3)];
        SDL.Scancode confirm = GmToScancode[ParseKey(4)];
        SDL.Scancode cancel = GmToScancode[ParseKey(5)];
        SDL.Scancode menu = GmToScancode[ParseKey(6)];

        var gamepadSection = ini.Sections["GAMEPAD_CONTROLS"];

        string ParseGamepadKey(int index) => new string(gamepadSection[index.ToString()].Split('.')[0].Trim("\""));

        SDL.GamepadButton gamepadDown = GmToGamepad[ParseGamepadKey(0)];
        SDL.GamepadButton gamepadRight = GmToGamepad[ParseGamepadKey(1)];
        SDL.GamepadButton gamepadUp = GmToGamepad[ParseGamepadKey(2)];
        SDL.GamepadButton gamepadLeft = GmToGamepad[ParseGamepadKey(3)];
        SDL.GamepadButton gamepadConfirm = GmToGamepad[ParseGamepadKey(4)];
        SDL.GamepadButton gamepadCancel = GmToGamepad[ParseGamepadKey(5)];
        SDL.GamepadButton gamepadMenu = GmToGamepad[ParseGamepadKey(6)];

        Instance = new Keymap
        {
            Down = new InputIdentity([Input.Get(down), Input.Get(gamepadDown), Input.Get(ControllerAxis.LeftStickDown)]),
            Right = new InputIdentity([Input.Get(right), Input.Get(gamepadRight), Input.Get(ControllerAxis.LeftStickRight)]),
            Up = new InputIdentity([Input.Get(up), Input.Get(gamepadUp), Input.Get(ControllerAxis.LeftStickUp)]),
            Left = new InputIdentity([Input.Get(left), Input.Get(gamepadLeft), Input.Get(ControllerAxis.LeftStickLeft)]),
            Confirm = new InputIdentity([Input.Get(confirm), Input.Get(gamepadConfirm)]),
            Cancel = new InputIdentity([Input.Get(cancel), Input.Get(gamepadCancel)]),
            Menu = new InputIdentity([Input.Get(menu), Input.Get(gamepadMenu)]),
        };
    }
}
