#nullable enable

using System;
using System.Text.Json;

public static class DataExportJobHandlerJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToJson(this DataExportJobHandler value, bool indented = false)
    {
        var options = indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options;
        return JsonSerializer.Serialize(value, options);
    }

    public static DataExportJobHandler? FromJson(string json)
    {
        return JsonSerializer.Deserialize<DataExportJobHandler>(json, _options);
    }

    public static bool TryFromJson(string json, out DataExportJobHandler? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<DataExportJobHandler>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
