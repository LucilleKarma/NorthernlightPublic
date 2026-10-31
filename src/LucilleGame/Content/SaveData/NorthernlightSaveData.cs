using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Northernlight.Content.Knight;
using Northernlight.Content.Knight.Items;
using Northernlight.Core;

namespace Northernlight.Content.SaveData;

/// <summary>
///     The collection of all per-file data saved and
///     loaded for Northernlight.
/// </summary>
public sealed class NorthernlightSaveData
{
    private OwnedTimer playSessionTimer;

    /// <summary>
    ///     How many attempts have been
    ///     taken against the Knight.
    /// </summary>
    public int TotalAttempts
    {
        get;
        set;
    }

    /// <summary>
    ///     How long the save file has been played for, in seconds.
    /// </summary>
    public float TimePlayedSeconds
    {
        get;
        set;
    }

    /// <summary>
    ///     How many wins the player
    ///     has achieved against the Knight.
    /// </summary>
    public int TotalWins
    {
        get;
        set;
    }

    /// <summary>
    ///     The slot that this save data is in, indexed from 0.
    /// </summary>
    [JsonIgnore]
    public int Slot
    {
        get;
        set;
    }

    /// <summary>
    ///     The name associated with this save slot.
    /// </summary>
    public string Name
    {
        get;
        set;
    }

    /// <summary>
    ///     The player's inventory.
    /// </summary>
    public List<VanillaGameItemRegistry.ID> Inventory
    {
        get;
        private set;
    } = [];

    /// <summary>
    ///     Whether this save file was created from
    ///     an aborted Chapter 5 weird route.
    /// </summary>
    public bool AbortedChapter5WeirdRoute
    {
        get;
        set;
    }

    /// <summary>
    ///     The file path of the save data.
    /// </summary>
    [JsonIgnore]
    public string SaveFilePath => Path.Combine(NorthernlightGame.RootDirectory, $"GameData{Slot}.json");

    public NorthernlightSaveData(int slot, string name)
    {
        Slot = slot;
        Name = name;
    }

    /// <summary>
    ///     Loads a game save from file data, creating a fallback if
    ///     one does not exist.
    /// </summary>
    public static NorthernlightSaveData Load(int slot, string fallbackName)
    {
        string path = Path.Combine(NorthernlightGame.RootDirectory, $"GameData{slot}.json");

        NorthernlightSaveData result;
        if (!File.Exists(path))
            result = new NorthernlightSaveData(slot, fallbackName);
        else
        {
            string text = string.Empty;
            bool success = false;
            while (!success)
            {
                try
                {
                    text = File.ReadAllText(path);
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
            result = JsonConvert.DeserializeObject<NorthernlightSaveData>(text) ?? new NorthernlightSaveData(slot, fallbackName);
        }
        result.Slot = slot;

        return result;
    }

    /// <summary>
    ///     Tries to load a game save from file data, returning
    ///     whether the result was successful.
    /// </summary>
    public static bool TryLoad(int slot, [NotNullWhen(true)] out NorthernlightSaveData? data)
    {
        string path = Path.Combine(NorthernlightGame.RootDirectory, $"GameData{slot}.json");

        data = null;
        if (!File.Exists(path))
            return false;

        string text = string.Empty;
        bool success = false;

        while (!success)
        {
            try
            {
                text = File.ReadAllText(path);
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

        data = JsonConvert.DeserializeObject<NorthernlightSaveData>(text) ?? new NorthernlightSaveData(slot, string.Empty);
        data.Slot = slot;

        return true;
    }

    /// <summary>
    ///     Tries to delete a given save file.
    /// </summary>
    public static void TryDelete(int slot)
    {
        string path = Path.Combine(NorthernlightGame.RootDirectory, $"GameData{slot}.json");
        if (!File.Exists(path))
            return;

        File.Delete(path);
    }

    /// <summary>
    ///     Generates Chapter 6 exclusive healing items for the player to use.
    /// </summary>
    public NorthernlightSaveData WithChapter6HeaingItems()
    {
        int seed = 1225;
        unchecked
        {
            for (int i = 0; i < Name.Length; i++)
                seed += Name[i] << (i * 2 + 5);
        }

        WeightedRandom<VanillaGameItemRegistry.ID> itemSelector = new WeightedRandom<VanillaGameItemRegistry.ID>(seed);
        itemSelector.Add(VanillaGameItemRegistry.ID.DarkCandyCane);
        itemSelector.Add(VanillaGameItemRegistry.ID.Nutmegnog);
        itemSelector.Add(VanillaGameItemRegistry.ID.GingerbreadMonster);
        itemSelector.Add(VanillaGameItemRegistry.ID.PieCone, 0.85f);
        itemSelector.Add(VanillaGameItemRegistry.ID.IceCube, 0.67f);
        itemSelector.Add(VanillaGameItemRegistry.ID.GrandDinner, 0.4f);
        itemSelector.Add(VanillaGameItemRegistry.ID.SnowwomanPiece, 0.4f);

        bool alreadyHasReviveMint = Inventory.Any(i => i == VanillaGameItemRegistry.ID.ReviveMint);

        int desiredItemsToGive = AbortedChapter5WeirdRoute ? 5 : 4;
        int maxInventoryCapacity = 12;
        int itemsToAdd = int.Min(desiredItemsToGive, maxInventoryCapacity - Inventory.Count);
        for (int i = 0; i < itemsToAdd; i++)
        {
            if (!alreadyHasReviveMint && i == 0)
                Inventory.Add(VanillaGameItemRegistry.ID.ReviveMint);
            else
                Inventory.Add(itemSelector.Get());
        }

        return this;
    }

    /// <summary>
    ///     Creates a copy of this save data.
    /// </summary>
    public NorthernlightSaveData CreateCopy(int slot)
    {
        return new NorthernlightSaveData(slot, Name)
        {
            TotalAttempts = TotalAttempts,
            TimePlayedSeconds = TimePlayedSeconds,
            TotalWins = TotalWins,
            Inventory = Inventory.ToList()
        };
    }

    /// <summary>
    ///     Resets the play session timer to zero.
    /// </summary>
    public void InitializePlaySessionTimer(KnightFightScene scene)
    {
        playSessionTimer ??= scene.Time.Rent();
        playSessionTimer.Reset();
    }

    /// <summary>
    ///     Saves this game data to the default
    ///     <see cref="SaveFilePath"/> path.
    /// </summary>
    public void Save()
    {
        if (playSessionTimer is not null)
        {
            TimePlayedSeconds += playSessionTimer;
            playSessionTimer.Reset();
        }

        Save(SaveFilePath);
    }

    /// <summary>
    ///     Saves this game data to a given file.
    /// </summary>
    private void Save(string path)
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
