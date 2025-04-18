using System.Collections.Generic;
using System.Text.Json;

namespace SocialiteNET;

/// <summary>
/// Extensions for working with JSON
/// </summary>
internal static class JsonExtensions
{
    /// <summary>
    /// Converts a JsonElement to Dictionary
    /// </summary>
    /// <param name="element">JSON element</param>
    /// <returns>Dictionary representation</returns>
    public static Dictionary<string, object?> DeserializeToDict(this JsonElement element)
    {
        Dictionary<string, object?> dict = new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonValue(property.Value);
        }

        return dict;
    }

    /// <summary>
    /// Converts a JSON value to the appropriate .NET type
    /// </summary>
    /// <param name="value">JSON value</param>
    /// <returns>Converted value</returns>
    private static object? ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out long longValue) ? longValue : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => value.DeserializeToDict(),
            JsonValueKind.Array => ConvertJsonArray(value),
            _ => null
        };
    }

    /// <summary>
    /// Converts a JSON array to a list
    /// </summary>
    /// <param name="array">JSON array</param>
    /// <returns>List of converted values</returns>
    private static List<object?> ConvertJsonArray(JsonElement array)
    {
        List<object?> list = new List<object?>();

        foreach (var item in array.EnumerateArray())
        {
            list.Add(ConvertJsonValue(item));
        }
        return list;
    }
}