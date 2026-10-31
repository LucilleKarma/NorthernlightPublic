using Northernlight.Core.Resources;

namespace Northernlight.Content.ChapterSelect;

public sealed class Fonts
{
    public enum Type
    {
        fnt_ja_comicsans = 0,
        fnt_ja_main = 1,
        fnt_main = 2,
        fnt_comicsans = 3,
        fnt_mainbig = 4,
        fnt_dotumche = 5,
        fnt_small = 6,
        fnt_ja_small = 7,
        fnt_ja_dotumche = 8,
        fnt_ja_mainbig = 9,
    }

    private const float scale_factor = 2f;

    private static readonly Font?[] fonts =
    [
        null,                                   // fnt_ja_comicsans
        LoadFont("JF-Dot-Shinonome14.ttf", 7),  // fnt_ja_main
        LoadFont("8bitoperator.ttf", 12),       // fnt_main
        null,                                   // fnt_comicsans
        null,                                   // fnt_mainbig
        null,                                   // fnt_dotumche
        null,                                   // fnt_small
        null,                                   // fnt_ja_small
        null,                                   // fnt_ja_dotumche
        null,                                   // fnt_ja_mainbig
    ];

    public static Font GetFont(Type type)
    {
        return fonts[(int)type] ?? throw new InvalidOperationException("Font not implemented: " + type);
    }

    private static Font LoadFont(string path, float size)
    {
        return new Font(path, size * scale_factor);
    }
}
