using System.Globalization;
using System.Text.Json;

public static class AnnotationEngine
{
    public static object? ResolveValue(JsonElement root, string path, JsonElement data)
    {
        var current = root;
        foreach (var segment in ParsePath(path))
        {
            if (segment.Index.HasValue)
            {
                if (current.ValueKind != JsonValueKind.Array || segment.Index.Value < 0 || segment.Index.Value >= current.GetArrayLength())
                    return GetFallback(data);
                current = current[segment.Index.Value];
                continue;
            }

            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment.PropertyName!, out var property))
                return GetFallback(data);
            current = property;
        }

        return ToObject(current);
    }

    public static string FormatValue(object? value, string fieldType, JsonElement format)
    {
        if (value is null)
            return string.Empty;

        var type = GetString(format, "type") ?? fieldType;
        return type switch
        {
            "currency" => FormatCurrency(value, format),
            "number" => FormatNumber(value, format),
            "percent" => FormatNumber(value, format) + "%",
            "date" => FormatDate(value, format),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    public static bool IsChecked(object? value, JsonElement annotation)
    {
        if (value is null)
            return false;
        if (annotation.TryGetProperty("value", out var expected) && expected.TryGetProperty("equals", out var equals))
            return string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), Convert.ToString(ToObject(equals), CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        return value is bool boolean && boolean;
    }

    private static object? GetFallback(JsonElement data) => data.TryGetProperty("fallback", out var fallback) ? ToObject(fallback) : null;

    private static object? ToObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.ToString()
        };
    }

    private static IEnumerable<PathSegment> ParsePath(string path)
    {
        var property = string.Empty;
        for (var index = 0; index < path.Length; index++)
        {
            var character = path[index];
            if (character == '.')
            {
                if (property.Length > 0)
                {
                    yield return new PathSegment(property, null);
                    property = string.Empty;
                }
            }
            else if (character == '[')
            {
                if (property.Length > 0)
                {
                    yield return new PathSegment(property, null);
                    property = string.Empty;
                }
                var end = path.IndexOf(']', index);
                if (end < 0 || !int.TryParse(path[(index + 1)..end], out var arrayIndex))
                    throw new InvalidOperationException($"Invalid data path: {path}");
                yield return new PathSegment(null, arrayIndex);
                index = end;
            }
            else
            {
                property += character;
            }
        }
        if (property.Length > 0)
            yield return new PathSegment(property, null);
    }

    private static string FormatCurrency(object value, JsonElement format)
    {
        if (!TryDecimal(value, out var amount))
            return string.Empty;
        var decimals = GetInt(format, "decimalPlaces", 0);
        var separator = GetBool(format, "thousandsSeparator", true);
        var symbol = GetString(format, "currencySymbol") ?? "$";
        return symbol + amount.ToString((separator ? "N" : "F") + decimals, CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(object value, JsonElement format)
    {
        if (!TryDecimal(value, out var number))
            return string.Empty;
        var decimals = GetInt(format, "decimalPlaces", 2);
        var separator = GetBool(format, "thousandsSeparator", false);
        return number.ToString((separator ? "N" : "F") + decimals, CultureInfo.InvariantCulture);
    }

    private static string FormatDate(object value, JsonElement format)
    {
        if (value is not string input || !DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return string.Empty;
        return date.ToString(GetString(format, "output") ?? "MM/dd/yyyy", CultureInfo.InvariantCulture);
    }

    private static bool TryDecimal(object value, out decimal result) => decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

    private static string? GetString(JsonElement value, string property) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var result) && result.ValueKind == JsonValueKind.String ? result.GetString() : null;

    private static int GetInt(JsonElement value, string property, int fallback) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var result) && result.TryGetInt32(out var number) ? number : fallback;

    private static bool GetBool(JsonElement value, string property, bool fallback) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var result) && result.ValueKind is JsonValueKind.True or JsonValueKind.False ? result.GetBoolean() : fallback;

    private sealed record PathSegment(string? PropertyName, int? Index);
}