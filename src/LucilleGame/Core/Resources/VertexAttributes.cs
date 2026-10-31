using System.Reflection;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents a set of attributes that can define the contents of a vertex.
/// </summary>
public sealed unsafe class VertexAttributes
{
    private SDL.GPUVertexAttribute[] attributes;

    /// <summary>
    ///     The color target descriptions.
    /// </summary>
    public readonly GPUVertexBufferDescription[] VertexBufferDescriptions;

    /// <summary>
    ///     The color target descriptions.
    /// </summary>
    public readonly SDL.GPUColorTargetDescription[] ColorTargetDescriptions;

    /// <summary>
    ///     The individual attributes owned by this set.
    /// </summary>
    public SDL.GPUVertexAttribute[] Attributes
    {
        get => attributes;
        private set => attributes = value;
    }

    /// <summary>
    ///     The amount of attributes maintained by this set.
    /// </summary>
    public uint Count
    {
        get;
    }

    /// <summary>
    ///     The pointer to the array maintained by this set.
    /// </summary>
    public nint Pointer
    {
        get
        {
            fixed (SDL.GPUVertexAttribute* ptr = Attributes)
            {
                return (nint)ptr;
            }
        }
    }

    /// <summary>
    ///     The pointer to the vertex buffer descriptions.
    /// </summary>
    public nint VertexBufferDescriptionsPointer
    {
        get
        {
            SDL.GPUVertexBufferDescription[] sdl = VertexBufferDescriptions.Select(a =>
            {
                return new SDL.GPUVertexBufferDescription()
                {
                    InputRate = a.InputRate,
                    Pitch = a.Pitch,
                    Slot = a.Slot
                };
            }).ToArray();

            fixed (SDL.GPUVertexBufferDescription* ptr = sdl)
                return (nint)ptr;
        }
    }

    /// <summary>
    ///     The pointer to the color target descriptions.
    /// </summary>
    public nint ColorTargetDescriptionsPointer
    {
        get
        {
            fixed (SDL.GPUColorTargetDescription* ptr = ColorTargetDescriptions)
                return (nint)ptr;
        }
    }

    public VertexAttributes(SDL.GPUColorTargetDescription[] colorTargetDescriptions, GPUVertexBufferDescription[] vertexBufferDescriptions)
    {
        VertexBufferDescriptions = vertexBufferDescriptions;
        ColorTargetDescriptions = colorTargetDescriptions;

        AutoGenerateAttributes();

        Count = (uint)Attributes!.Length;
    }

    private void AutoGenerateAttributes()
    {
        uint locationIndex = 0;
        List<SDL.GPUVertexAttribute> attributesCache = [];
        for (int i = 0; i < VertexBufferDescriptions.Length; i++)
        {
            uint offset = 0;
            uint slot = VertexBufferDescriptions[i].Slot;

            foreach (FieldInfo field in VertexBufferDescriptions[i].Type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                Type fieldType = field.FieldType;
                TypeVertexMappings.Result mappingsValue = TypeVertexMappings.Evaluate(fieldType);

                attributesCache.Add(new SDL.GPUVertexAttribute
                {
                    Location = locationIndex,
                    Format = mappingsValue.Format,
                    Offset = offset,
                    BufferSlot = slot,
                });
                offset += mappingsValue.Size;
                locationIndex++;
            }
        }

        attributes = attributesCache.ToArray();
    }
}
