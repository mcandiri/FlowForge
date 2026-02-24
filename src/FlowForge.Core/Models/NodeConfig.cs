using System.Text.Json;

namespace FlowForge.Core.Models;

public class NodeConfig : Dictionary<string, object?>
{
    public NodeConfig() : base(StringComparer.OrdinalIgnoreCase) { }

    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (!TryGetValue(key, out var value) || value is null)
            return defaultValue;

        if (value is T typed)
            return typed;

        if (value is JsonElement element)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
            catch
            {
                return defaultValue;
            }
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public string GetString(string key, string defaultValue = "")
        => GetValue<string>(key, defaultValue) ?? defaultValue;

    public int GetInt(string key, int defaultValue = 0)
        => GetValue<int>(key, defaultValue);

    public bool GetBool(string key, bool defaultValue = false)
        => GetValue<bool>(key, defaultValue);
}
