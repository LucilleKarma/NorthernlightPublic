using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Northernlight.Core.Data;

public class NBT()
{
    public NBT(IEnumerable<object> tags) : this()
    {
        foreach (var tag in tags)
            data.Add(tag);
    }

    public NBT(NBT other) : this()
    {
        foreach (var tag in other.data)
            data.Add(tag);
    }

    // Contains the contents of this NBT.
    private readonly HashSet<object> data = [];

    /// <summary>
    ///     The total number of tags in this <see cref="NBT"/>
    /// </summary>
    public int Count => data.Count;

    /// <summary>
    ///     A collection of all tags in this <see cref="NBT"/>
    /// </summary>
    public IEnumerable<string> Tags => data.Select(o => o switch
    {
        TagValue tagValue => tagValue.Tag,
        _ => (string)o
    }).Distinct();

    /// <summary>
    ///     Checks if the specified tag is contained in this <see cref="NBT"/>.
    /// </summary>
    /// <param name="tag">The tag to check for.</param>
    /// <returns><see langword="true"/> if the specified tag is contained in this <see cref="NBT"/>; otherwise <see langword="false"/></returns>
    public bool Get(string tag)
    {
        return data.Contains(tag);
    }

    /// <summary>
    ///     Adds the specified tag to this <see cref="NBT"/>.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    /// <returns><see langword="true"/> if the tag was added to the <see cref="NBT"/>; <see langword="false"/> if it was already present.</returns>
    public bool Add(string tag)
    {
        return data.Add(tag);
    }

    /// <summary>
    ///     Removes the specified tag from this <see cref="NBT"/>.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    /// <returns><see langword="true"/> if the tag was removed from the <see cref="NBT"/>; <see langword="false"/> if it was not present.</returns>
    public bool Remove(string tag)
    {
        return data.Remove(tag);
    }

    /// <summary>
    ///     Sets the value at the specified <paramref name="tag"/>.<br/>
    ///     Assigning <see langword="null"/> will remove the value from this <see cref="NBT"/>.
    /// </summary>
    /// <param name="tag">The tag to assign the value at.</param>
    /// <param name="value">The value to assign.</param>
    /// <typeparam name="TValue">The type of the value to assign.</typeparam>
    public void SetValue<TValue>(string tag, TValue? value)
    {
        if (data.FirstOrDefault(o => o is TagValue tagValue && tagValue.Tag == tag, null) is TagValue { Value: TValue } existingTag)
            data.Remove(existingTag);

        if (value is not null)
            data.Add(new TagValue(tag, value));
    }

    /// <summary>
    ///     Sets the value at the specified <paramref name="tag"/>.<br/>
    ///     Assigning <see langword="null"/> will remove the value from this <see cref="NBT"/>.
    /// </summary>
    /// <param name="tag">The tag to assign the value at.</param>
    /// <param name="value">The value to assign.</param>
    /// <param name="valueType">The type of the value to assign.</param>
    public void SetValue(string tag, object? value, Type valueType)
    {
        if (data.FirstOrDefault(o => o is TagValue tagValue && tagValue.Tag == tag && tagValue.Value.GetType() == valueType, null) is TagValue existingTag)
            data.Remove(existingTag);

        if (value is not null)
            data.Add(new TagValue(tag, value));
    }

    /// <summary>
    ///     Checks if there is a value of type <see cref="TValue"/> at the specified tag.
    /// </summary>
    /// <param name="tag">The tag to check.</param>
    /// <typeparam name="TValue">The value type to check for.</typeparam>
    /// <returns><see langword="true"/> if this <see cref="NBT"/> has a value of type <see cref="TValue"/> at the specified <paramref name="tag"/>; otherwise <see langword="false"/></returns>
    public bool HasValue<TValue>(string tag)
    {
        return TryGet<TValue>(tag, out _);
    }

    /// <summary>
    ///     Checks if there is a value of type <paramref name="valueType"/> at the specified tag.
    /// </summary>
    /// <param name="tag">The tag to check.</param>
    /// <param name="valueType"> The value type to check for.</param>
    /// <returns><see langword="true"/> if this <see cref="NBT"/> has a value of type <paramref name="valueType"/> at the specified <paramref name="tag"/>; otherwise <see langword="false"/></returns>
    public bool HasValue(string tag, Type valueType)
    {
        return data.Any(o => o is TagValue tagValue && tagValue.Tag == tag && tagValue.Value.GetType() == valueType);
    }

    /// <summary>
    ///     Gets the value at the specified <paramref name="tag"/> as a <see cref="TValue"/>.
    /// </summary>
    /// <param name="tag">The tag to fetch.</param>
    /// <typeparam name="TValue">The type of the value at the specified tag.</typeparam>
    public TValue Get<TValue>(string tag)
    {
        return (TValue)((TagValue)data.First(o => o is TagValue tagValue && tagValue.Tag == tag)).Value;
    }

    /// <summary>
    ///     Safely attempts to get the value at the specified <paramref name="tag"/> as a <see cref="TValue"/>.
    /// </summary>
    /// <param name="tag">The tag to fetch.</param>
    /// <param name="value">The value associated with the given tag, if the tag was found; otherwise <see langword="default"/>.</param>
    /// <typeparam name="TValue">The type of the value at the specified tag.</typeparam>
    /// <returns><see langword="true"/> if the value was successfully retrieved; otherwise <see langword="false"/>.</returns>
    public bool TryGet<TValue>(string tag, [MaybeNullWhen(false)] out TValue value)
    {
        if (data.FirstOrDefault(o => o is TagValue tagValue && tagValue.Tag == tag, null) is TagValue { Value: TValue foundValue })
        {
            value = foundValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Gets the value at the specified <paramref name="tag"/>, or the supplied <paramref name="defaultValue"/> if it was not found.
    /// </summary>
    /// <param name="tag">The tag to fetch.</param>
    /// <param name="defaultValue">The value to return in the event the given <paramref name="tag"/> was not found.</param>
    /// <typeparam name="TValue">The type of the value at the specified tag.</typeparam>
    /// <returns>The value of the specified tag, or <paramref name="defaultValue"/> if the tag is not present.</returns>
    public TValue GetOrDefault<TValue>(string tag, TValue defaultValue)
    {
        return TryGet<TValue>(tag, out var value) ? value : defaultValue;
    }

    /// <summary>
    ///     Writes this <see cref="NBT"/> to the given stream.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="compressOutput">Whether to run the serialized <see cref="NBT"/> through compression before writing.</param>
    /// <exception cref="UnauthorizedAccessException">If the given <paramref name="stream"/> is not writable.</exception>
    public void WriteTo(Stream stream, bool compressOutput)
    {
        if (!stream.CanWrite)
            throw new UnauthorizedAccessException("The stream is not writable.");

        BinaryWriter outputStream = new BinaryWriter(stream);

        // First write whether this stream is compressed.
        // This is written directly to the output so that this value is not compressed.
        outputStream.Write(compressOutput);

        // Set up the serialization to be written to either a memory stream for compression,
        // or directly to the output if compression is not requested.
        MemoryStream? compressionStream = compressOutput ? new MemoryStream() : null;
        BinaryWriter serializer = compressOutput ? new BinaryWriter(compressionStream!) : outputStream;

        // Serialize and write to the stream
        string serializedNbt = JsonConvert.SerializeObject(data, compressOutput ? Formatting.None : Formatting.Indented, Json.SerializerSettings);
        serializer.Write(serializedNbt);

        // Close the serializer stream
        // If no compression is requested, this is also the output stream
        serializer.Dispose();

        if (!compressOutput)
            return;

        // Compress and write the serialized data
        byte[] uncompressedData = compressionStream!.ToArray();
        compressionStream.Dispose();

        byte[] compressedData = Compressor.Compress(uncompressedData);
        outputStream.Write(compressedData);

        // Close the output stream
        outputStream.Dispose();
    }

    /// <summary>
    ///     Reads the stream into this <see cref="NBT"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <param name="clearExisting">Whether any existing tags on this <see cref="NBT"/> should be cleared before reading.</param>
    /// <param name="overwrite">Whether the existing tags on this <see cref="NBT"/> should be overwritten by new values.</param>
    /// <exception cref="UnauthorizedAccessException">If the given <paramref name="stream"/> is not readable.</exception>
    public void ReadFrom(Stream stream, bool clearExisting = true, bool overwrite = true)
    {
        if (!stream.CanRead)
            throw new UnauthorizedAccessException("The stream is not readable.");

        MemoryStream? decompressionStream = null;
        BinaryReader reader = new BinaryReader(stream);

        // True if compression was used
        if (reader.ReadBoolean())
        {
            // Read remaining bytes into an array
            long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
            byte[] compressedBytes = reader.ReadBytes((int)remainingBytes);
            reader.Dispose();

            // Decompress bytes
            var decompressed = Compressor.Decompress(compressedBytes);
            decompressionStream = new MemoryStream(decompressed);
            reader = new BinaryReader(decompressionStream);
        }

        // Clear existing values if requested
        if (clearExisting)
            data.Clear();

        // Read the serialized data and dispose streams
        string serializedNbt = reader.ReadString();
        reader.Dispose();
        decompressionStream?.Dispose();

        // Deserialize into the underlying hashset
        var nbtData = JsonConvert.DeserializeObject<HashSet<object>>(serializedNbt, Json.SerializerSettings)!;

        foreach (var tag in nbtData)
        {
            switch (tag)
            {
                // For tag values, we need to check if data should be overwritten
                // Tag values are kept based on object type
                case TagValue tagValue:
                    if (overwrite || !HasValue(tagValue.Tag, tagValue.Value.GetType()))
                        SetValue(tagValue.Tag, tagValue.Value, tagValue.Value.GetType());
                    break;

                // For simple tags we do not need to check, since they are
                // the equivalent of a bool that is always true
                case string value:
                    Add(value);
                    break;
            }
        }
    }

    /// <summary>
    ///     Converts this <see cref="NBT"/> to an array of serialized bytes.
    /// </summary>
    /// <param name="compressOutput">Whether to run the serialized bytes through compression.</param>
    /// <returns>The serialized bytes of this <see cref="NBT"/>.</returns>
    public byte[] GetBytes(bool compressOutput)
    {
        using MemoryStream stream = new MemoryStream();
        WriteTo(stream, compressOutput);
        return stream.ToArray();
    }

    /// <summary>
    ///     Writes this <see cref="NBT"/> to the given file path.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="compressOutput"></param>
    public void WriteToFile(string filePath, bool compressOutput = true)
    {
        using var fileStream = File.Create(filePath);
        WriteTo(fileStream, compressOutput);
    }

    /// <summary>
    ///     Creates a new <see cref="NBT"/> from the given stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A new <see cref="NBT"/> containing the data from <paramref name="stream"/>.</returns>
    public static NBT FromStream(Stream stream)
    {
        NBT nbt = new NBT();
        nbt.ReadFrom(stream);
        return nbt;
    }

    /// <summary>
    ///     Creates a new <see cref="NBT"/> from the given serialized bytes.
    /// </summary>
    /// <param name="bytes">The serialized bytes to read from.</param>
    /// <returns>A new <see cref="NBT"/> containing the data from <paramref name="bytes"/>.</returns>
    public static NBT FromBytes(byte[] bytes)
    {
        using MemoryStream stream = new MemoryStream(bytes);
        return FromStream(stream);
    }

    /// <summary>
    ///     Creates a new <see cref="NBT"/> from a file at the given path.
    /// </summary>
    /// <param name="filePath">The file to read from.</param>
    /// <returns>A new <see cref="NBT"/> containing the data from the file at <paramref name="filePath"/>.</returns>
    public static NBT FromFile(string filePath)
    {
        using FileStream fileStream = File.OpenRead(filePath);
        return FromStream(fileStream);
    }

    /// <summary>
    ///     Copies the contents of this <see cref="NBT"/> to a new <see cref="NBT"/>.
    /// </summary>
    public NBT Copy() => new NBT(this);

    /// <summary>
    ///     Represents a bundled string tag and object value.
    /// </summary>
    private class TagValue(string tag, object value)
    {
        // Set accessors are required for json serialization.
        public string Tag
        {
            get;
            set;
        } = tag;

        public object Value
        {
            get;
            set;
        } = value;
    }

    private static class Json
    {
        // Used to serialize/deserialize this TagData
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            DefaultValueHandling = DefaultValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            CheckAdditionalContent = false
        };

        // Json does not automatically distinguish between different number types
        // We need to do it manually with our own converter
        private class NumberConverter : JsonConverter
        {
            private static readonly FrozenDictionary<Type, string> numberIdentifiers = new Dictionary<Type, string>
            {
                [typeof(byte)] = "by",
                [typeof(sbyte)] = "sb",
                [typeof(decimal)] = "dc",
                [typeof(double)] = "db",
                [typeof(float)] = "fl",
                [typeof(int)] = "in",
                [typeof(uint)] = "ui",
                [typeof(nint)] = "ni",
                [typeof(nuint)] = "nu",
                [typeof(long)] = "lo",
                [typeof(ulong)] = "ul",
                [typeof(short)] = "sh",
                [typeof(ushort)] = "us"
            }.ToFrozenDictionary();

            public override bool CanConvert(Type objectType)
            {
                // typeof(object) is necessary for deserialization
                return numberIdentifiers.ContainsKey(objectType) || objectType == typeof(object);
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                Type? valueType = value?.GetType() ?? null;

                if (valueType is not null && numberIdentifiers.TryGetValue(valueType, out string? identifier))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("$");
                    writer.WriteValue($"{identifier}{value}");
                    writer.WriteEndObject();
                }

                else
                {
                    serializer.Serialize(writer, value);
                }
            }

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                var result = serializer.Deserialize(reader);

                if (result is not JObject jObject || !jObject.TryGetValue("$", out var jToken))
                    return result;

                string serialized = jToken.ToString();
                string identifier = serialized[..2];

                foreach ((Type valueType, string valueKey) in numberIdentifiers)
                {
                    if (identifier != valueKey)
                        continue;

                    // Automatically invoke the corresponding Parse() method based on found type
                    MethodInfo parseMethod = valueType.GetMethod(
                        "Parse",
                        BindingFlags.Static | BindingFlags.Public,
                        [typeof(string)])!;

                    return parseMethod.Invoke(null, [serialized[2..]]);
                }

                return result;
            }
        }
    }

    private static class Compressor
    {
        /// <summary>
        ///     Compresses an array of bytes utilizing <see cref="GZipStream"/> with <see cref="CompressionLevel.SmallestSize"/>.
        /// </summary>
        public static byte[] Compress(byte[] input)
        {
            using MemoryStream output = new MemoryStream();
            using GZipStream compressor = new GZipStream(output, CompressionLevel.SmallestSize);

            compressor.Write(input, 0, input.Length);
            compressor.Flush();

            return output.ToArray();
        }

        /// <summary>
        ///     Decompresses an array of bytes previously compressed by a <see cref="GZipStream"/>.
        /// </summary>
        public static byte[] Decompress(byte[] input)
        {
            using MemoryStream inputReader = new MemoryStream(input);
            using GZipStream decompressor = new GZipStream(inputReader, CompressionMode.Decompress);

            List<byte> output = [];
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = decompressor.Read(buffer)) > 0)
                output.AddRange(buffer.Take(bytesRead));

            return output.ToArray();
        }
    }
}
