using System.Reflection;
using System.Text;

namespace AtlasGenerator;

public static class Program
{
    private static readonly List<string> classNames = [];

    /// <summary>
    ///     The text indentation used for
    ///     lines in the auto-generated file.
    /// </summary>
    public const string Indent = "    ";

    public static void Main(string[] args)
    {
        string assetsDirectory = args[0];
        string outputPath = args[1];
        string namespaceText = args[2];
        string outputClassName = Path.GetFileNameWithoutExtension(outputPath);

        ProcessImages(assetsDirectory, namespaceText, outputClassName, outputPath);
    }

    private static void ProcessImages(string assetsDirectory, string namespaceText, string className, string outputPath)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("using System.CodeDom.Compiler;");
        sb.AppendLine("using Northernlight.Core.Resources;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceText};");
        sb.AppendLine();

        AssemblyName assemblyName = Assembly.GetCallingAssembly().GetName();
        string toolHeader = assemblyName.Name ?? "Unknown";
        string versionHeader = assemblyName.Version?.ToString() ?? "1.0.0.0";
        sb.AppendLine($"[GeneratedCode(\"{toolHeader}\", \"{versionHeader}\")]");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        AppendLine(sb, 1, "private static GraphicsDevice device;");
        sb.AppendLine();
        AppendLine(sb, 1, "private static CommandBuffer.CopyPassScope activeCopyScope;");
        sb.AppendLine();
        AppendLine(sb, 1, "internal static readonly Dictionary<string, Lazy<Atlas>> CreatedAtlases = [];");
        sb.AppendLine();

        foreach (string directory in Directory.GetDirectories(assetsDirectory))
            ProcessDirectory(directory, sb);

        sb.AppendLine();
        AppendLine(sb, 1, "/// <summary>");
        AppendLine(sb, 1, "///     Prepares this atlas system for use.");
        AppendLine(sb, 1, "/// </summary>");
        AppendLine(sb, 1, $"public static void Initialize(GraphicsDevice device, FrameContext frameContext)");
        BuildInitializeMethodBody(sb, className);

        AppendLine(sb, 0, string.Empty);

        AppendLine(sb, 1, "/// <summary>");
        AppendLine(sb, 1, "///     Registers a new atlas globally.");
        AppendLine(sb, 1, "/// </summary>");
        AppendLine(sb, 1, $"public static Lazy<Atlas> Register(string name, List<Texture> sources) => CreatedAtlases[name] = new Lazy<Atlas>(() => new Atlas(name, sources));");

        sb.AppendLine("}");

        File.WriteAllText(outputPath, sb.ToString().Replace(Environment.NewLine, "\n"));
    }

    private static void BuildInitializeMethodBody(StringBuilder sb, string className)
    {
        AppendLine(sb, 1, "{");
        AppendLine(sb, 2, $"{className}.device = device;");
        foreach (string atlas in classNames)
            AppendLine(sb, 2, $"_ = {atlas}.Atlas;");

        AppendLine(sb, 2, $"{className}.activeCopyScope = frameContext.CommandBuffer.Copy();");
        AppendLine(sb, 2, "foreach (Lazy<Atlas> atlas in CreatedAtlases.Values)");
        AppendLine(sb, 3, "atlas.Value.UploadSourcesIfNecessary(activeCopyScope);");

        AppendLine(sb, 2, $"{className}.activeCopyScope.Dispose();");

        AppendLine(sb, 2, "foreach (Lazy<Atlas> atlas in CreatedAtlases.Values)");
        AppendLine(sb, 3, "atlas.Value.BuildIfNecessary(device, frameContext);");

        AppendLine(sb, 1, "}");
    }

    private static void ProcessDirectory(string directory, StringBuilder sb)
    {
        string[] imagePaths = Directory.GetFiles(directory, "*.png");
        if (imagePaths.Length <= 0)
            return;

        string className = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        classNames.Add(className);

        AppendLine(sb, 1, $"public sealed class {className}");
        AppendLine(sb, 1, "{");

        AppendLine(sb, 2, $"public static readonly Lazy<Atlas> Atlas = Register(nameof({className}), [");

        foreach (string imagePath in imagePaths)
        {
            // It is expected that the resource manager will
            // move all atlas texture data to the same directory
            // as the executeable, thus permitting the usage
            // of relative, rather than brittle absolute paths.
            string relativePath = Path.GetFileName(imagePath);

            AppendLine(sb, 3, $"new Texture(@\"{relativePath}\", device),");
        }
        AppendLine(sb, 2, "]);");

        sb.AppendLine();
        for (int i = 0; i < imagePaths.Length; i++)
        {
            string propertyName = Path.GetFileNameWithoutExtension(imagePaths[i]).Replace(' ', '_');
            string keyDefinition = $"new Atlas.AtlasKey(\"{propertyName}\", {i})";
            AppendLine(sb, 2, $"public static AtlasEntry {propertyName} => Atlas.Value.Entries[{keyDefinition}];");

            if (i < imagePaths.Length - 1)
                sb.AppendLine();
        }

        AppendLine(sb, 1, "}");
    }

    private static void AppendLine(StringBuilder sb, int indentCount, string text)
    {
        for (int i = 0; i < indentCount; i++)
            sb.Append(Indent);
        sb.AppendLine(text);
    }
}
