using System.Runtime.InteropServices;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a shader.
/// </summary>
public class Shader : ReloadableResource, IDisposable
{
    private bool disposed;
    private bool compileAtAll;

    /// <summary>
    ///     The set of pipelines this shader
    ///     is bound to.
    /// </summary>
    internal List<WeakReference<IPipeline>> AttachedPipelines = [];

    public SpirvBytecodeHandle CodeHandle { get; private set; }

    /// <summary>
    ///     The device that created this shader.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
        private set;
    }

    /// <summary>
    ///     The file path to the compiled version of this resource.
    /// </summary>
    public string FilePathCompiled
    {
        get;
        private set;
    }

    /// <summary>
    ///     The handle of this shader to its underlying
    ///     SDL resource.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    } = nint.Zero;

    /// <summary>
    ///     The composite data that determines the structure of this shader.
    /// </summary>
    public ShaderData Data
    {
        get;
        private set;
    }

    /// <summary>
    ///     The language this shader is written in.
    /// </summary>
    public ShaderLanguage.Compiler ShaderCompiler
    {
        get;
        private set;
    }

    protected Shader(string filePath) : base(filePath)
    { }

    public Shader(string filePath, ShaderLanguage.Compiler shaderCompiler, GraphicsDevice device, ShaderData data) : base(ResourceMonitor.CheckForSubdirectoryEquivalent(filePath))
    {
        RunConstructor(filePath, shaderCompiler, device, data);
    }

    protected void RunConstructor(string filePath, ShaderLanguage.Compiler shaderCompiler, GraphicsDevice device, ShaderData data)
    {
        const string compiled_extension = ".spv";

        filePath = ResourceMonitor.CheckForSubdirectoryEquivalent(filePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string filePathWithoutExtension = Path.Combine(filePath.Replace(Path.GetFileName(filePath), string.Empty), fileNameWithoutExtension);
        FilePathCompiled = $"{filePathWithoutExtension}{compiled_extension}";
        FilePathCompiled = ResourceMonitor.CheckForSubdirectoryEquivalent(FilePathCompiled);

        if (!File.Exists(FilePathCompiled))
        {
            // Don't write for now; can cause issues if the game exits or
            // crashes before actually compiling (will attempt to read an empty
            // file as valid bytecode).
            // File.WriteAllBytes(FilePathCompiled, []);
            compileAtAll = true;
        }

#if DEBUG
        compileAtAll = true;
#endif

        ShaderCompiler = shaderCompiler;

        Device = device;
        Data = data;

        Recompile();
        Reload();
    }

    private unsafe void Recompile()
    {
        if (CodeHandle.Handle != 0)
        {
            SDL.Free(CodeHandle.Handle);
        }

        if (compileAtAll)
        {
            CodeHandle = ShaderCompiler.Invoke(Device, this, FilePath!);

            var codeSpan = new Span<byte>((void*)CodeHandle.Handle, (int)CodeHandle.Length);
            {
                File.WriteAllBytes(FilePathCompiled, codeSpan);
            }
        }
        else
        {
            using var fs = File.OpenRead(FilePathCompiled);

            var pCode = Marshal.AllocHGlobal((nint)fs.Length);

            fs.ReadExactly(new Span<byte>((void*)pCode, (int)fs.Length));

            CodeHandle = new SpirvBytecodeHandle(pCode, (nuint)fs.Length);
        }
    }

    private void Reload()
    {
        if (Handle != 0)
            SDL.ReleaseGPUShader(Device.Handle, Handle);

        if (CodeHandle.Handle == 0)
            throw new Exception("Failed to reload shader; was it compiled?");

        Handle = ShaderLanguage.CompileSpirv(Device, this, CodeHandle);
    }

    public void Dispose()
    {
        if (disposed || Handle == nint.Zero)
            return;

        disposed = true;
        Return();
        GC.SuppressFinalize(this);
        SDL.ReleaseGPUShader(Device.Handle, Handle);
    }

    protected internal override void Reload(FileSystemEventArgs fileChangeData)
    {
        if (disposed || FilePath is null)
            return;

        compileAtAll = true;
        Recompile();
        Reload();

        foreach (WeakReference<IPipeline> pipelineRef in AttachedPipelines)
        {
            if (pipelineRef.TryGetTarget(out IPipeline? pipeline))
                pipeline.Recreate();
        }
        AttachedPipelines.RemoveAll(p => !p.TryGetTarget(out _));
    }
}
