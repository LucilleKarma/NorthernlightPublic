using System.CodeDom.Compiler;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

[GeneratedCode("ShaderGenerator", "1.0.0.0")]
public static class Shaders
{
    private static bool initialized;
    
    /// <summary>
    ///     The auto-generated fragment shader read from the AuroraShader.hlsl file.
    /// </summary>
    public static Shader AuroraShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the AwesomeSilhouetteShader.hlsl file.
    /// </summary>
    public static Shader AwesomeSilhouetteShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the BasicFrag.hlsl file.
    /// </summary>
    public static Shader BasicFrag
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated vertex shader read from the BasicVert.hlsl file.
    /// </summary>
    public static Shader BasicVert
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the BattleBoxFreezeShader.hlsl file.
    /// </summary>
    public static Shader BattleBoxFreezeShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the BlizzardShader.hlsl file.
    /// </summary>
    public static Shader BlizzardShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the CrtShader.hlsl file.
    /// </summary>
    public static Shader CrtShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the DentShader.hlsl file.
    /// </summary>
    public static Shader DentShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the DepthsBallMaskingShader.hlsl file.
    /// </summary>
    public static Shader DepthsBallMaskingShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the DirectionalOutlineShader.hlsl file.
    /// </summary>
    public static Shader DirectionalOutlineShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated vertex shader read from the FontVertexShader.hlsl file.
    /// </summary>
    public static Shader FontVertexShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the FullColorOverlayBlendShader.hlsl file.
    /// </summary>
    public static Shader FullColorOverlayBlendShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the FullColorOverlayShader.hlsl file.
    /// </summary>
    public static Shader FullColorOverlayShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the KnightDeathAnimationShader.hlsl file.
    /// </summary>
    public static Shader KnightDeathAnimationShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the KnightFightBorderShader.hlsl file.
    /// </summary>
    public static Shader KnightFightBorderShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the LogoShader.hlsl file.
    /// </summary>
    public static Shader LogoShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated vertex shader read from the MeshVertexShader.hlsl file.
    /// </summary>
    public static Shader MeshVertexShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the OverlayBlendShader.hlsl file.
    /// </summary>
    public static Shader OverlayBlendShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the RedVignetteShader.hlsl file.
    /// </summary>
    public static Shader RedVignetteShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the RotatedBoxRestrictionShader.hlsl file.
    /// </summary>
    public static Shader RotatedBoxRestrictionShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the SimpleBorderShader.hlsl file.
    /// </summary>
    public static Shader SimpleBorderShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated vertex shader read from the SpriteVertexShader.hlsl file.
    /// </summary>
    public static Shader SpriteVertexShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     The auto-generated fragment shader read from the WindLinesShader.hlsl file.
    /// </summary>
    public static Shader WindLinesShader
    {
        get;
        private set;
    }
    
    /// <summary>
    ///     Pre-initializes all shader instances in the game.
    /// </summary>
    internal static void Load(GraphicsDevice device)
    {
        if (initialized)
            return;
        
        initialized = true;
        
        AuroraShader = new Shader(Path.Combine("chapter6_windows", "AuroraShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        AwesomeSilhouetteShader = new Shader(Path.Combine("chapter6_windows", "AwesomeSilhouetteShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        BasicFrag = new Shader(Path.Combine("chapter6_windows", "BasicFrag.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 0,
        });

        BasicVert = new Shader(Path.Combine("chapter6_windows", "BasicVert.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Vertex,
            SamplerCount = 0,
            UniformBufferCount = 1,
        });

        BattleBoxFreezeShader = new Shader(Path.Combine("chapter6_windows", "BattleBoxFreezeShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        BlizzardShader = new Shader(Path.Combine("chapter6_windows", "BlizzardShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        CrtShader = new Shader(Path.Combine("chapter6_windows", "CrtShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        DentShader = new Shader(Path.Combine("chapter6_windows", "DentShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 2,
            UniformBufferCount = 1,
        });

        DepthsBallMaskingShader = new Shader(Path.Combine("chapter6_windows", "DepthsBallMaskingShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 2,
            UniformBufferCount = 1,
        });

        DirectionalOutlineShader = new Shader(Path.Combine("chapter6_windows", "DirectionalOutlineShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        FontVertexShader = new Shader(Path.Combine("chapter6_windows", "FontVertexShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Vertex,
            SamplerCount = 0,
            UniformBufferCount = 1,
        });

        FullColorOverlayBlendShader = new Shader(Path.Combine("chapter6_windows", "FullColorOverlayBlendShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        FullColorOverlayShader = new Shader(Path.Combine("chapter6_windows", "FullColorOverlayShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 0,
        });

        KnightDeathAnimationShader = new Shader(Path.Combine("chapter6_windows", "KnightDeathAnimationShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        KnightFightBorderShader = new Shader(Path.Combine("chapter6_windows", "KnightFightBorderShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        LogoShader = new Shader(Path.Combine("chapter6_windows", "LogoShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        MeshVertexShader = new Shader(Path.Combine("chapter6_windows", "MeshVertexShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Vertex,
            SamplerCount = 0,
            UniformBufferCount = 1,
        });

        OverlayBlendShader = new Shader(Path.Combine("chapter6_windows", "OverlayBlendShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 2,
            UniformBufferCount = 1,
        });

        RedVignetteShader = new Shader(Path.Combine("chapter6_windows", "RedVignetteShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 0,
            UniformBufferCount = 0,
        });

        RotatedBoxRestrictionShader = new Shader(Path.Combine("chapter6_windows", "RotatedBoxRestrictionShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        SimpleBorderShader = new Shader(Path.Combine("chapter6_windows", "SimpleBorderShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });

        SpriteVertexShader = new Shader(Path.Combine("chapter6_windows", "SpriteVertexShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Vertex,
            SamplerCount = 0,
            UniformBufferCount = 1,
        });

        WindLinesShader = new Shader(Path.Combine("chapter6_windows", "WindLinesShader.hlsl"), ShaderLanguage.Hlsl, device, new ShaderData()
        {
            Entrypoint = "main",
            Format = device.ShaderFormat,
            Stage = SDL.GPUShaderStage.Fragment,
            SamplerCount = 1,
            UniformBufferCount = 1,
        });
    }
}
