using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace KnightGameStatsRefsBuilder;

internal class Program
{
    /// <summary>
    ///     The name of the environment
    ///     variable containing the stats
    ///     repository.
    /// </summary>
    public const string EnvironmentVariableName = "StatsRepositoryPath";

    /// <summary>
    ///     The text indentation used for
    ///     lines in the auto-generated file.
    /// </summary>
    public const string Indent = "    ";

    static void Main(string[] args)
    {
        string outputPath = args[0];
        string namespaceText = args[1];
        string? repositoryPath = args[2];
        if (!File.Exists(repositoryPath))
            return;

        string json = repositoryPath is null ? string.Empty : File.ReadAllText(repositoryPath);
        Dictionary<string, float>? values = repositoryPath is null ? null : JsonConvert.DeserializeObject<Dictionary<string, float>>(json);

        string className = Path.GetFileNameWithoutExtension(outputPath);

        AssemblyName assemblyName = Assembly.GetCallingAssembly().GetName();
        string toolHeader = assemblyName.Name ?? "Unknown";
        string versionHeader = assemblyName.Version?.ToString() ?? "1.0.0.0";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System.CodeDom.Compiler;");
        sb.AppendLine("using Northernlight.Core;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceText};");
        sb.AppendLine();

        sb.AppendLine($"[GeneratedCode(\"{toolHeader}\", \"{versionHeader}\")]");
        sb.AppendLine($"public sealed class {className}");
        sb.AppendLine("{");

        AppendLine(sb, 1, "private static string FilePath");
        AppendLine(sb, 1, "{");

        AppendLine(sb, 2, "get");
        AppendLine(sb, 2, "{");

        AppendLine(sb, 3, $"string? environmentDictatedPath = Environment.GetEnvironmentVariable(EnvironmentVariableName);");
        AppendLine(sb, 3, "if (environmentDictatedPath is null)");
        AppendLine(sb, 3, "{");

        AppendLine(sb, 4, "string currentDirectory = Program.ContentRoot;");
        AppendLine(sb, 4, "string path = Path.Combine(currentDirectory, \"Data\", FileName);");
        AppendLine(sb, 4, "environmentDictatedPath = path;");
        AppendLine(sb, 4, "Environment.SetEnvironmentVariable(EnvironmentVariableName, path);");

        AppendLine(sb, 3, "}");

        AppendLine(sb, 3, "return environmentDictatedPath;");
        AppendLine(sb, 2, "}");

        AppendLine(sb, 1, "}");

        AppendLine(sb, 1, string.Empty);

        AppendLine(sb, 1, "private static readonly ReloadableData<Dictionary<string, float>> stats = new ReloadableData<Dictionary<string, float>>(FilePath);");
        AppendLine(sb, 1, string.Empty);

        AppendLine(sb, 1, "private const string EnvironmentVariableName = \"StatsRepositoryPath\";");
        AppendLine(sb, 1, string.Empty);

        AppendLine(sb, 1, "private const string FileName = \"StatsRepository.json\";");
        if (values is not null && values.Count >= 1)
        {
            AppendLine(sb, 1, string.Empty);

            int index = 0;
            foreach (var kv in values.OrderBy(kv => kv.Key))
            {
                AppendLine(sb, 1, $"public static float {kv.Key} => stats.Value[\"{kv.Key}\"];");
                if (index < values.Count - 1)
                    AppendLine(sb, 1, string.Empty);

                index++;
            }
        }

        sb.AppendLine("}");

        File.WriteAllText(outputPath, sb.ToString().Replace(Environment.NewLine, "\n"));
    }

    private static void AppendLine(StringBuilder sb, int indentCount, string text)
    {
        for (int i = 0; i < indentCount; i++)
            sb.Append(Indent);
        sb.AppendLine(text);
    }
}
