using System.Reflection;

namespace ShaderGenerator;

public partial class Program
{
    public static void Main(string[] args)
    {
        string rootDirectory = args[0];
        string outputFilePath = args[1];
        string namespaceText = args[2];
        string className = Path.GetFileNameWithoutExtension(outputFilePath);

        string shaderContentsDirectory = Path.Combine(rootDirectory, "Shaders");
        if (!Directory.Exists(shaderContentsDirectory))
            return;

        List<ShaderMetadata> shaders = [];
        foreach (string path in Directory.GetFiles(shaderContentsDirectory, "*.hlsl", SearchOption.AllDirectories).OrderBy(p => Path.GetFileName(p)))
        {
            string fileName = Path.GetFileName(path);
            string relativeDirectory = path.Replace(fileName, string.Empty).Replace(shaderContentsDirectory, string.Empty);
            bool vertexShader = relativeDirectory.Contains("Vertex");
            bool computeShader = relativeDirectory.Contains("Compute");

            ShaderFormat format = ShaderFormat.Fragment;
            if (vertexShader)
                format = ShaderFormat.Vertex;
            if (computeShader)
                format = ShaderFormat.Compute;

            shaders.Add(new ShaderMetadata(path, format));
        }

        ScopedStringBuilder sb = new ScopedStringBuilder();
        sb.Write("using System.CodeDom.Compiler;");
        sb.Write("using Northernlight.Core.Resources;");
        sb.Write("using SDL3;");
        sb.Write();
        sb.Write($"namespace {namespaceText};");
        sb.Write();

        AssemblyName assemblyName = Assembly.GetCallingAssembly().GetName();
        string toolHeader = assemblyName.Name ?? "Unknown";
        string versionHeader = assemblyName.Version?.ToString() ?? "1.0.0.0";
        sb.Write($"[GeneratedCode(\"{toolHeader}\", \"{versionHeader}\")]");
        sb.Write($"public static class {className}");

        using (sb.IndentScope())
        {
            sb.Write($"private static bool initialized;");
            sb.Write();

            for (int i = 0; i < shaders.Count; i++)
            {
                ShaderMetadata metadata = shaders[i];

                string typeName = "Shader";
                if (metadata.Format == ShaderFormat.Compute)
                    typeName = "ComputeShader";

                if (metadata.Format == ShaderFormat.Unknown)
                    sb.Write($"#warning The shader below, {metadata.ShaderName}, is of an unknown format and may break if used.");

                metadata.EmitXml(sb);
                sb.Write($"public static {typeName} {metadata.ShaderName}");
                using (sb.IndentScope())
                {
                    sb.Write($"get;");
                    sb.Write($"private set;");
                }
                sb.Write();
            }

            sb.Write("/// <summary>");
            sb.Write($"/// {ScopedStringBuilder.Indent}Pre-initializes all shader instances in the game.");
            sb.Write("/// </summary>");

            sb.Write("internal static void Load(GraphicsDevice device)");
            using (sb.IndentScope())
            {
                sb.Write("if (initialized)");
                using (sb.IndentScope(includeBraces: false))
                    sb.Write("return;");
                sb.Write();

                sb.Write("initialized = true;");
                sb.Write();

                for (int i = 0; i < shaders.Count; i++)
                {
                    ShaderMetadata metadata = shaders[i];

                    sb.Write($"{metadata.ShaderName} = ", newline: false);
                    metadata.EmitDefinitionText(sb);

                    bool lastShader = i == shaders.Count - 1;
                    if (!lastShader)
                        sb.Write(skipIndent: true);
                }
            }
        }

        File.WriteAllText(outputFilePath, sb.ToString().Replace(Environment.NewLine, "\n"));
    }
}
