using Newtonsoft.Json;
using Northernlight.Content.Borders;
using Northernlight.Core.InputHandling;
using SDL3;

namespace Northernlight.Content.SaveData;

/// <summary>
///     The collection of all global data saved and
///     loaded for Northernlight.
/// </summary>
public sealed class Config
{
    [JsonRequired]
    private bool wasdControlsOverride;

    [JsonRequired]
    private float musicVolume = 0.6f;

    [JsonRequired]
    private float soundVolume = 0.6f;

    [JsonRequired]
    private string language = "en-US";

    [JsonRequired]
    private BorderMode borderMode = BorderMode.Dynamic;

    /// <summary>
    ///     The music volume factor of the game.
    /// </summary>
    [JsonIgnore]
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            if (float.Abs(musicVolume - value) >= 1e-5f)
            {
                musicVolume = value;
                Save();
            }
        }
    }

    /// <summary>
    ///     The sound volume factor of the game.
    /// </summary>
    [JsonIgnore]
    public float SoundVolume
    {
        get => soundVolume;
        set
        {
            if (float.Abs(soundVolume - value) >= 1e-5f)
            {
                soundVolume = value;
                Save();
            }
        }
    }

    /// <summary>
    ///     The language to use for the game.
    /// </summary>
    [JsonIgnore]
    public string Language
    {
        get => language;
        set
        {
            if (language != value)
            {
                language = value;
                Save();
            }
        }
    }

    /// <summary>
    ///     Whether directional controls should be swapped
    ///     from their standard arrow keys to a WASD
    ///     format.
    /// </summary>
    [JsonIgnore]
    public bool WasdControlsOverride
    {
        get => wasdControlsOverride;
        set
        {
            if (wasdControlsOverride != value)
            {
                wasdControlsOverride = value;
                ApplyKeymapSetting();
                Save();
            }
        }
    }

    /// <summary>
    ///     The mode dictating what kind of border should be
    ///     displayed beyond game letterboxing.
    /// </summary>
    public BorderMode BorderMode
    {
        get => borderMode;
        set
        {
            if (borderMode != value)
            {
                borderMode = value;
                Save();
            }
        }
    }

    internal void ApplyKeymapSetting()
    {
        Keymap originalKeymap = ActiveKeymap.Instance;
        if (WasdControlsOverride)
        {
            ButtonInputProcessor downGamepad = originalKeymap.Down.Processors.FirstOrDefault(p => p.keyKind == ButtonKeyKind.ControllerButton);
            ButtonInputProcessor rightGamepad = originalKeymap.Right.Processors.FirstOrDefault(p => p.keyKind == ButtonKeyKind.ControllerButton);
            ButtonInputProcessor upGamepad = originalKeymap.Up.Processors.FirstOrDefault(p => p.keyKind == ButtonKeyKind.ControllerButton);
            ButtonInputProcessor leftGamepad = originalKeymap.Left.Processors.FirstOrDefault(p => p.keyKind == ButtonKeyKind.ControllerButton);

            ActiveKeymap.Instance = new Keymap()
            {
                Down = new InputIdentity([Input.Get(SDL.Scancode.S), downGamepad, Input.Get(ControllerAxis.LeftStickDown)]),
                Right = new InputIdentity([Input.Get(SDL.Scancode.D), rightGamepad, Input.Get(ControllerAxis.LeftStickRight)]),
                Up = new InputIdentity([Input.Get(SDL.Scancode.W), upGamepad, Input.Get(ControllerAxis.LeftStickUp)]),
                Left = new InputIdentity([Input.Get(SDL.Scancode.A), leftGamepad, Input.Get(ControllerAxis.LeftStickLeft)]),
                Confirm = originalKeymap.Confirm,
                Cancel = originalKeymap.Cancel,
                Menu = originalKeymap.Menu,
            };
        }
        else if (DeltaruneGlobalData.SaveProvider is not null)
            ActiveKeymap.Load(DeltaruneGlobalData.SaveProvider);
    }

    /// <summary>
    ///     The file path of the config.
    /// </summary>
    [JsonIgnore]
    public static string SaveFilePath => Path.Combine(NorthernlightGame.RootDirectory, "Config.json");

    /// <summary>
    ///     Loads a config from file data, creating a fallback if
    ///     one does not exist.
    /// </summary>
    public static Config Load()
    {
        Config result;
        if (!File.Exists(SaveFilePath))
        {
            result = new Config();
            result.Save();
        }
        else
        {
            bool success = false;
            string text = string.Empty;
            while (!success)
            {
                try
                {
                    text = File.ReadAllText(SaveFilePath);
                    success = true;
                }
                catch (IOException)
                {
                    continue;
                }
                catch
                {
                    throw;
                }
            }

            try
            {
                result = JsonConvert.DeserializeObject<Config>(text) ?? new Config();
            }
            catch
            {
                result = new Config();
                result.Save();
            }
        }

        return result;
    }

    /// <summary>
    ///     Saves this config to the default
    ///     <see cref="SaveFilePath"/> path.
    /// </summary>
    public void Save() => Save(SaveFilePath);

    /// <summary>
    ///     Saves this config to a given file.
    /// </summary>
    private void Save(string path)
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
