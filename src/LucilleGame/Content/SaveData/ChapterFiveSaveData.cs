using System.Globalization;

namespace Northernlight.Content.SaveData;

/// <summary>
///     The collection of all data saved and
///     loaded in a given chapter five save file.
/// </summary>
public sealed class ChapterFiveSaveData
{
    /// <summary>
    ///     The name of the save file's creator.
    /// </summary>
    public string CreatorName
    {
        get;
    }

    /// <summary>
    ///     The set of other names identified on the save file.
    /// </summary>
    public List<string> OtherNames
    {
        get;
    } = [];

    /// <summary>
    ///     The amount of gold owned on the save file.
    /// </summary>
    public int Gold
    {
        get;
    }

    // Experience Points? Execution Points? Eh, whatever.
    /// <summary>
    ///     The amount of XP earned on the save file.
    /// </summary>
    public int XP
    {
        get;
    }

    /// <summary>
    ///     The level on the save file.
    /// </summary>
    public int LV
    {
        get;
    }

    /// <summary>
    ///     The hero HP stat data.
    /// </summary>
    public List<int> HeroesHP
    {
        get;
    } = [];

    /// <summary>
    ///     The hero max HP stat data.
    /// </summary>
    public List<int> HeroesMaxHP
    {
        get;
    } = [];

    /// <summary>
    ///     The hero attack stat data.
    /// </summary>
    public List<int> HeroesAttack
    {
        get;
    } = [];

    /// <summary>
    ///     The hero defense stat data.
    /// </summary>
    public List<int> HeroesDefense
    {
        get;
    } = [];

    /// <summary>
    ///     The hero magic stat data.
    /// </summary>
    public List<int> HeroesMagic
    {
        get;
    } = [];

    /// <summary>
    ///     The hero weapons list.
    /// </summary>
    public List<int> HeroesWeapons
    {
        get;
    } = [];

    /// <summary>
    ///     The hero first armor slot list.
    /// </summary>
    public List<int> HeroesArmorA
    {
        get;
    } = [];

    /// <summary>
    ///     The hero second armor slot list.
    /// </summary>
    public List<int> HeroesArmorB
    {
        get;
    } = [];

    /// <summary>
    ///     Whether the save file is in the dark world
    ///     currently or not.
    /// </summary>
    public bool InDarkWorld
    {
        get;
    }

    /// <summary>
    ///     The set of all normal item IDs.
    /// </summary>
    public List<int> Items
    {
        get;
    } = [];

    /// <summary>
    ///     The set of all key item IDs.
    /// </summary>
    public List<int> KeyItems
    {
        get;
    } = [];

    /// <summary>
    ///     The set of all weapon IDs.
    /// </summary>
    public List<int> Weapons
    {
        get;
    } = [];

    /// <summary>
    ///     The set of all armor IDs.
    /// </summary>
    public List<int> Armor
    {
        get;
    } = [];

    /// <summary>
    ///     The set of all pocket item IDs.
    /// </summary>
    public List<int> PocketItems
    {
        get;
    } = [];

    /// <summary>
    ///     The set of all general save flags used.
    /// </summary>
    public List<float> GeneralSaveFlags
    {
        get;
    } = [];

    /// <summary>
    ///     The plot progression value.
    /// </summary>
    public int Plot
    {
        get;
    }

    /// <summary>
    ///     The current room ID.
    /// </summary>
    public int CurrentRoom
    {
        get;
    }

    /// <summary>
    ///     The playtime value, in accumulated frames of
    ///     Deltarune.
    /// </summary>
    /// <remarks>
    ///     Deltarune runs at 30FPS, so dividing this value by 30
    ///     will yield the amount of playtime seconds.
    /// </remarks>
    public float PlayTime
    {
        get;
    }

    /// <summary>
    ///     Whether this save file completed all weird route
    ///     events but aborted at the lake scene.
    /// </summary>
    public bool AbortedChapter5WeirdRoute => GeneralSaveFlags[1743] != 0f;

    public ChapterFiveSaveData(string path)
    {
        using FileStream stream = new FileStream(path, FileMode.Open);
        using StreamReader reader = new StreamReader(stream);

        string ReadRawLine() => reader.ReadLine() ?? string.Empty;

        int ReadInt()
        {
            string text = ReadRawLine();
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new InvalidDataException($"Invalid int: '{text}'");
            return value;
        }
        float ReadFloat()
        {
            string text = ReadRawLine();
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw new InvalidDataException($"Invalid float: '{text}'");
            return value;
        }

        CreatorName = ReadRawLine();
        for (int i = 0; i < 6; i++)
            OtherNames.Add(ReadRawLine());

        // These values correspond to information about which
        // heroes the game expects to be present.
        // For most contexts this'll be Kris, Susie, or Ralsei, or
        // some combination of just a handful of such.
        // Either way, it's not really relevant to Northernlight, so
        // onto the next lines we go.
        _ = ReadInt();
        _ = ReadInt();
        _ = ReadInt();

        Gold = ReadInt();
        XP = ReadInt();
        LV = ReadInt();

        // Invicibility data?
        _ = ReadInt();
        _ = ReadInt();

        InDarkWorld = ReadInt() != 0;

        // Kris, Susie, Ralsei, Noelle, and I thiiink
        // Flowery for a short gag in chapter five?
        int heroesToConsider = 5;

        for (int i = 0; i < heroesToConsider; i++)
        {
            HeroesHP.Add(ReadInt());
            HeroesMaxHP.Add(ReadInt());
            HeroesAttack.Add(ReadInt());
            HeroesDefense.Add(ReadInt());
            HeroesMagic.Add(ReadInt());

            // global.guts???
            _ = ReadInt();

            HeroesWeapons.Add(ReadInt());
            HeroesArmorA.Add(ReadInt());
            HeroesArmorB.Add(ReadInt());

            // Attack style data.
            _ = ReadInt();

            // Contains a bunch of equipment modifier data.
            // Miiiight be relevant at some point but we don't
            // use this at all, so just skip all of it.
            for (int j = 0; j < 40; j++)
                _ = ReadFloat();

            // Contains spell mappings from hero
            // ID to spell ID.
            // We set these up manually rather than
            // reading them from save data and so this is
            // ignored too.
            // Very interesting, though. Maybe would be fun
            // to tamper with the save data and see if I
            // can give Noelle rude buster or something...
            for (int j = 0; j < 12; j++)
                _ = ReadInt();
        }

        // Bullet game design tuning values.
        // Again, very interesting, but we set these up
        // ourselves, and I doubt they change much
        // in Deltarune given how central they are
        // to everything.
        float bulletSpeed = ReadFloat();
        float grazeAmount = ReadFloat();
        float grazeSize = ReadFloat();

        for (int i = 0; i < 13; i++)
        {
            Items.Add(ReadInt());
            KeyItems.Add(ReadInt());
        }

        for (int i = 0; i < 48; i++)
        {
            Weapons.Add(ReadInt());
            Armor.Add(ReadInt());
        }

        for (int i = 0; i < 72; i++)
            PocketItems.Add(ReadInt());

        // Tension point and max tension point data.
        // Not sure why this is saved, honestly? Since
        // tension points get reset after battles.
        _ = ReadFloat();
        _ = ReadFloat();

        // Legacy data, not relevant to chapter five
        // saves.
        for (int i = 0; i < 11; i++)
            _ = ReadInt();

        for (int i = 0; i < 16; i++)
            _ = ReadInt();

        for (int i = 0; i < 2500; i++)
            GeneralSaveFlags.Add(ReadFloat());

        Plot = ReadInt();
        CurrentRoom = ReadInt();
        PlayTime = ReadFloat();
    }
}
