using System.Diagnostics.CodeAnalysis;
using IniParser;
using IniParser.Model;

namespace Northernlight.Content.SaveData.SurveyReader;

public sealed class DeltaruneSaveProvider(string rootDirectory)
{
    private static readonly FileIniDataParser ini_parser = new();

    public readonly string RootDirectory = rootDirectory;

    public bool FileExists(string localFileName)
    {
        return File.Exists(Path.Combine(RootDirectory, localFileName));
    }

    public bool TryGetIni(string localFileName, [NotNullWhen(returnValue: true)] out IniData? ini)
    {
        var path = Path.Combine(RootDirectory, localFileName);
        if (!File.Exists(path))
        {
            ini = null;
            return false;
        }

        try
        {
            ini = ini_parser.ReadFile(path);
            return true;
        }
        catch
        {
            ini = null;
            return false;
        }
    }

    public void SaveIni(string localFileName, IniData ini)
    {
        try
        {
            ini_parser.WriteFile(Path.Combine(RootDirectory, localFileName), ini);
        }
        catch
        {
            // ignore
        }
    }

    public bool TryToLoadChapter5Data(int fileChoice, [NotNullWhen(true)] out ChapterFiveSaveData? result)
    {
        result = null;

        string path = Path.Combine(RootDirectory, $"filech5_{fileChoice + 3}");
        if (!Path.Exists(path))
            return false;

        result = new ChapterFiveSaveData(path);
        return true;
    }
}
