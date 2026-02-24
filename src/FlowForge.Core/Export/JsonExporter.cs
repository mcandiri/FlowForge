using System.Text.Json;
using System.Text.Json.Serialization;
using FlowForge.Core.Models;

namespace FlowForge.Core.Export;

public class JsonExporter : IWorkflowExporter
{
    public string Format => "json";

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new NodeConfigJsonConverter() }
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new NodeConfigJsonConverter() }
    };

    public string Export(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        return JsonSerializer.Serialize(workflow, SerializeOptions);
    }

    public static Workflow? Import(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<Workflow>(json, DeserializeOptions);
    }
}

/// <summary>
/// Custom JSON converter for NodeConfig that properly handles serialization
/// and deserialization of the Dictionary&lt;string, object?&gt; base type.
/// On deserialization, JsonElement values are converted to their underlying
/// CLR types (string, bool, int, double, etc.) so that NodeConfig.GetValue&lt;T&gt;
/// works without requiring JsonElement unwrapping for primitive types.
/// </summary>
public class NodeConfigJsonConverter : JsonConverter<NodeConfig>
{
    public override NodeConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token for NodeConfig.");

        var config = new NodeConfig();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return config;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token.");

            string key = reader.GetString()!;
            reader.Read();

            object? value = ReadValue(ref reader);
            config[key] = value;
        }

        throw new JsonException("Unexpected end of JSON while reading NodeConfig.");
    }

    public override void Write(Utf8JsonWriter writer, NodeConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            // Use camelCase for keys to match the serializer policy
            string key = kvp.Key;
            if (options.PropertyNamingPolicy is not null)
                key = options.PropertyNamingPolicy.ConvertName(key);

            writer.WritePropertyName(key);
            WriteValue(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }

    private static object? ReadValue(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out long l) ? (l >= int.MinValue && l <= int.MaxValue ? (object)(int)l : l) : reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.StartArray => ReadArray(ref reader),
            JsonTokenType.StartObject => ReadObject(ref reader),
            _ => throw new JsonException($"Unexpected token type: {reader.TokenType}")
        };
    }

    private static List<object?> ReadArray(ref Utf8JsonReader reader)
    {
        var list = new List<object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return list;

            list.Add(ReadValue(ref reader));
        }

        throw new JsonException("Unexpected end of JSON while reading array.");
    }

    private static Dictionary<string, object?> ReadObject(ref Utf8JsonReader reader)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dict;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token.");

            string key = reader.GetString()!;
            reader.Read();
            dict[key] = ReadValue(ref reader);
        }

        throw new JsonException("Unexpected end of JSON while reading object.");
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case Dictionary<string, object?> dict:
                writer.WriteStartObject();
                foreach (var kvp in dict)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value, options);
                }
                writer.WriteEndObject();
                break;
            case List<object?> list:
                writer.WriteStartArray();
                foreach (var item in list)
                    WriteValue(writer, item, options);
                writer.WriteEndArray();
                break;
            default:
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
                break;
        }
    }
}
