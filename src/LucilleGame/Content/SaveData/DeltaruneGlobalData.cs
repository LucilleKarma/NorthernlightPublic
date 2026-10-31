using System.Globalization;
using Northernlight.Content.SaveData.SurveyReader;
using Northernlight.Core.Logging;

namespace Northernlight.Content.SaveData;

public enum SaveChapter
{
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
}

public enum SaveSlot
{
    One = 0,
    Two = 1,
    Three = 2,

    OneCompleted = 3,
    TwoCompleted = 4,
    ThreeCompleted = 5,

    Count = 6,
}

public static class DeltaruneGlobalData
{
    private const int completed_offset = SaveSlot.OneCompleted - SaveSlot.One;

    private static readonly DeltaruneSaveProvider? save_provider = ResolveSaveProvider();

    /// <summary>
    ///     The Deltarune save provider.
    /// </summary>
    public static DeltaruneSaveProvider? SaveProvider => save_provider;

    static DeltaruneGlobalData()
    {
        if (save_provider is not null)
            ActiveKeymap.Load(save_provider);
        else
            Logger.Warning("Base game save provider data could not be loaded. Assuming DELTARUNE is not installed.");
    }

    public static bool FileExists(string relativePath)
    {
        if (save_provider is null)
        {
            return false;
        }

        return save_provider.FileExists(relativePath);
    }

    // scr_chapter_save_file_exists
    public static bool ChapterSaveFileExists(SaveChapter chapter)
    {
        if (save_provider is null)
        {
            return false;
        }

        for (var i = SaveSlot.One; i <= SaveSlot.Three; i++)
        {
            if (save_provider.FileExists($"filech{(int)chapter}_{(int)i}"))
            {
                return true;
            }
        }

        return false;
    }

    // scr_chapter_save_file_exists_in_slot
    public static bool ChapterSaveFileExistsInSlot(SaveChapter chapter, SaveSlot slot)
    {
        if (save_provider is null)
        {
            return false;
        }

        return save_provider.FileExists($"filech{(int)chapter}_{(int)slot}");
    }

    // scr_completed_chapter_any_slot
    public static bool CompletedChapterAnySlot(SaveChapter chapter)
    {
        if (save_provider is null)
        {
            return false;
        }

        for (var i = SaveSlot.One; i <= SaveSlot.Three; i++)
        {
            if (save_provider.FileExists($"filech{(int)chapter}_{(int)i + completed_offset}"))
            {
                return true;
            }
        }

        return false;
    }

    // scr_completed_chapter_in_slot
    public static bool CompletedChapterInSlot(SaveChapter chapter, SaveSlot slot)
    {
        if (save_provider is null)
        {
            return false;
        }

        return save_provider.FileExists($"filech{(int)chapter}_{(int)slot + completed_offset}");
    }

    // scr_fought_secret_boss_any_slot
    public static bool FoughtSecretBossAnySlot(SaveChapter chapter)
    {
        if (save_provider is null)
        {
            return false;
        }

        for (var i = SaveSlot.One; i <= SaveSlot.Three; i++)
        {
            var result = GetUraValue(chapter, i);
            if (result > 0f)
            {
                return true;
            }
        }

        return false;
    }

    // scr_ini_chapter
    public static string IniChapter(SaveChapter chapter, SaveSlot slot)
    {
        if (chapter >= SaveChapter.Two)
        {
            return $"G_{(int)chapter}_{(int)slot}";
        }

        return $"G{(int)slot}";
    }

    public static float GetIniValue(string iniPath, string sectionKey, string key)
    {
        if (save_provider is null || !save_provider.TryGetIni(iniPath, out var ini))
        {
            return 0f;
        }

        if (!ini.Sections.ContainsSection(sectionKey))
        {
            return 0f;
        }

        var section = ini.Sections[sectionKey];
        if (!section.ContainsKey(key))
        {
            return 0f;
        }

        return float.TryParse(section[key], CultureInfo.InvariantCulture, out var value) ? value : 0f;
    }

    // scr_get_ini_value
    public static float GetIniValue(SaveChapter chapter, SaveSlot slot, string key)
    {
        return GetIniValue("dr.ini", IniChapter(chapter, slot), key);
    }

    public static void SetIniValue(string iniPath, string sectionKey, string key, float value)
    {
        if (save_provider is null || !save_provider.TryGetIni(iniPath, out var ini))
        {
            return;
        }

        if (!ini.Sections.ContainsSection(sectionKey))
        {
            ini.Sections.AddSection(sectionKey);
        }

        ini[sectionKey][key] = value.ToString("F6", CultureInfo.InvariantCulture);

        // TODO: Is it right for us to do this?
        save_provider.SaveIni(iniPath, ini);
    }

    // scr_set_ini_value
    public static void SetIniValue(SaveChapter chapter, SaveSlot slot, string key, float value)
    {
        if (save_provider is null || !save_provider.TryGetIni("dr.ini", out var ini))
        {
            return;
        }

        var sectionKey = IniChapter(chapter, slot);
        if (!ini.Sections.ContainsSection(sectionKey))
        {
            ini.Sections.AddSection(sectionKey);
        }

        ini[sectionKey][key] = value.ToString("F6", CultureInfo.InvariantCulture);

        // TODO: Is it right for us to do this?
        save_provider.SaveIni("dr.ini", ini);
    }

    public static float[] GetIniValueAllSlots(SaveChapter chapter, string key)
    {
        var list = new float[(int)SaveSlot.Count];

        if (save_provider is null || !save_provider.TryGetIni("dr.ini", out _))
        {
            return list;
        }

        for (var i = SaveSlot.One; i < SaveSlot.Count; i++)
        {
            list[(int)i] = GetIniValue(chapter, i, key);
        }

        return list;
    }

    // scr_is_chapter_revealed
    public static bool IsChapterRevealed(SaveChapter chapter)
    {
        // DELTARUNE only reveals the first chapter by default:
        /*
        if (chapter == SaveChapter.One)
        {
            return true;
        }
        */

        // But let's only guarantee ours instead...
        if (chapter == SaveChapter.Six)
        {
            return true;
        }

        // If there's no known save, keep Chapter 1 hidden.
        if (save_provider is null)
        {
            return false;
        }

        // Otherwise, base game logic should be fine?
        if (chapter == SaveChapter.One)
        {
            return true;
        }

        var completedPrevious = CompletedChapterAnySlot(chapter - 1);
        var completedCurrent = CompletedChapterAnySlot(chapter);
        var inProgress = ChapterSaveFileExists(chapter);
        return completedPrevious || completedCurrent || inProgress;
    }

    // TODO: Note that we should always reveal chapter 6 regardless of this!!!
    // get_highest_revealed_chapter
    public static SaveChapter GetHighestRevealedChapter()
    {
        var highestRevealed = SaveChapter.One;

        for (var i = SaveChapter.One; i <= SaveChapter.Six; i++)
        {
            if (IsChapterRevealed(i))
            {
                break;
            }

            if (i > highestRevealed)
            {
                highestRevealed = i;
            }
        }

        return highestRevealed;
    }

    // scr_has_ura_data
    public static bool HasUraData()
    {
        if (save_provider is null || !save_provider.TryGetIni("dr.ini", out var ini))
        {
            return false;
        }

        return ini.Sections.ContainsSection("URA");
    }

    // scr_get_ura_value
    public static float GetUraValue(SaveChapter chapter, SaveSlot slot)
    {
        if (save_provider is null || !save_provider.TryGetIni("dr.ini", out var ini))
        {
            return 0f;
        }

        if (!ini.Sections.ContainsSection("URA"))
        {
            return 0f;
        }

        var ura = ini["URA"];
        var keyName = $"{(int)chapter}_{(int)slot}";

        if (!ura.ContainsKey(keyName))
        {
            return 0f;
        }

        var uraValue = ura[keyName].Trim("\"");
        if (float.TryParse(uraValue, CultureInfo.InvariantCulture, out var value))
            return value;

        return 0f;
    }

    // scr_set_ura_value
    public static float SetUraValue(SaveChapter chapter, SaveSlot slot, float value)
    {
        if (save_provider is null || !save_provider.TryGetIni("dr.ini", out var ini))
        {
            return value;
        }

        if (!ini.Sections.ContainsSection("URA"))
        {
            ini.Sections.AddSection("URA");
        }

        var ura = ini["URA"];
        var keyName = $"{(int)chapter}_{(int)slot}";
        ura[keyName] = value.ToString("F6", CultureInfo.InvariantCulture);

        // TODO: This function is never used... but is it really right for us to
        //       write to these files?
        save_provider.SaveIni("dr.ini", ini);

        return value;
    }

    private static DeltaruneSaveProvider? ResolveSaveProvider()
    {
        foreach (var path in ResolvePossibleSavePaths())
        {
            if (Directory.Exists(path))
            {
                return new DeltaruneSaveProvider(path);
            }
        }

        return null;
    }

    private static IEnumerable<string> ResolvePossibleSavePaths()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return SaveLocations.WindowsDirectory;
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return SaveLocations.MacDirectory;
        }
        else if (OperatingSystem.IsLinux())
        {
            yield return SaveLocations.LinuxSteamPlayFullDirectory;
            yield return SaveLocations.LinuxUnofficialDirectory;
            yield return SaveLocations.LinuxSteamPlayDemoDirectory;
        }
    }
}
