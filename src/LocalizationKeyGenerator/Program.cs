using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace LocalizationKeyGenerator;

public static class Program
{
    /// <summary>
    ///     Represents an instance of a localization entry.
    /// </summary>
    public sealed class LocalizationEntry
    {
        /// <summary>
        ///     The text associated with this localization
        ///     entry.
        /// </summary>
        public required string Text
        {
            get;
            set;
        }

        /// <summary>
        ///     The development status of this entry.
        /// </summary>
        public required string Status
        {
            get;
            set;
        }
    }

    /// <summary>
    ///     The text indentation used for
    ///     lines in the auto-generated file.
    /// </summary>
    public const string Indent = "    ";

    public static void Main(string[] args)
    {
        string englishLocalizationDirectory = args[0];
        string outputPath = args[1];
        string namespaceText = args[2];
        string outputDirectory = args[3];
        if (!Directory.Exists(englishLocalizationDirectory))
            return;

        string outputClassName = Path.GetFileNameWithoutExtension(outputPath);

        StringBuilder sb = new StringBuilder();
        Dictionary<string, LocalizationEntry> englishData = [];
        foreach (string path in Directory.GetFiles(englishLocalizationDirectory, "*.json", new EnumerationOptions()
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 2
        }))
        {
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, LocalizationEntry>>(File.ReadAllText(path));
            if (dictionary is not null)
            {
                foreach (var kv in dictionary)
                {
                    englishData[kv.Key] = kv.Value;
                }
            }
        }

        var englishKeys = englishData.Keys.OrderBy(k => k).ToList();

        sb.AppendLine("using System.CodeDom.Compiler;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceText};");
        sb.AppendLine();

        AssemblyName assemblyName = Assembly.GetCallingAssembly().GetName();
        string toolHeader = assemblyName.Name ?? "Unknown";
        string versionHeader = assemblyName.Version?.ToString() ?? "1.0.0.0";
        sb.AppendLine($"[GeneratedCode(\"{toolHeader}\", \"{versionHeader}\")]");
        sb.AppendLine($"public static class {outputClassName}");
        sb.AppendLine("{");

        for (int i = 0; i < englishKeys.Count; i++)
        {
            string sanitizedKey = SanitizeKey(englishKeys[i]);
            string englishText = englishData[englishKeys[i]].Text;
            string sanitizedEnglishText = englishText.Replace("\n", "\n    ///     <br></br>");

            string xml = $"The '{sanitizedKey}' localization key.<br></br> Localized in unformatted English as:<br></br><br></br> '{sanitizedEnglishText}'";
            AppendLine(sb, 1, "/// <summary>");
            AppendLine(sb, 1, $"///     {xml}");
            AppendLine(sb, 1, "/// </summary>");

            string line = $"public static readonly LanguageAgnosticText {sanitizedKey} = new LanguageAgnosticText(\"{englishKeys[i]}\");";
            AppendLine(sb, 1, line);
            if (i < englishKeys.Count - 1)
                AppendLine(sb, 1, string.Empty);
        }

        sb.AppendLine("}");

        File.WriteAllText(outputPath, sb.ToString().Replace(Environment.NewLine, "\n"));
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        string? oneDirectoryUpFromEnglish = Path.GetDirectoryName(englishLocalizationDirectory);
        if (oneDirectoryUpFromEnglish is null)
            return;

        foreach (string directory in Directory.GetDirectories(oneDirectoryUpFromEnglish))
        {
            string languageName = new DirectoryInfo(directory).Name;
            foreach (string categoryPath in Directory.GetFiles(directory, "*.json"))
            {
                string localOutputFilePath = Path.Combine(outputDirectory, "Localization", languageName, Path.GetFileName(categoryPath));
                string? localOutputDirectory = Path.GetDirectoryName(localOutputFilePath);

                if (localOutputDirectory is not null && !Directory.Exists(localOutputDirectory))
                    Directory.CreateDirectory(localOutputDirectory);
                if (File.Exists(localOutputFilePath))
                    File.Delete(localOutputFilePath);
                File.Copy(categoryPath, localOutputFilePath);
            }
        }
    }

    private static string SanitizeKey(string key) => key.Replace("-", "_").Replace(".", "_");

    private static void AppendLine(StringBuilder sb, int indentCount, string text)
    {
        for (int i = 0; i < indentCount; i++)
            sb.Append(Indent);
        sb.AppendLine(text);
    }
}
