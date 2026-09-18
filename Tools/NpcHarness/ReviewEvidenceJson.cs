using System.Text.Json;

namespace NpcHarness;

// Small strict reader shared only by the review-evidence contracts.
internal static class ReviewEvidenceJson
{
    public static JsonElement Read(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        RejectDuplicates(document.RootElement);
        return document.RootElement.Clone();
    }

    public static void Fields(JsonElement value, params string[] fields)
    {
        Require(value.ValueKind == JsonValueKind.Object, "Expected a JSON object.");
        HashSet<string> actual = value.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Require(actual.SetEquals(fields), "Missing or unknown JSON properties: " + string.Join(", ", fields));
    }

    public static string Text(JsonElement value, string field)
    {
        Require(value.TryGetProperty(field, out JsonElement item) && item.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(item.GetString()), "Missing nonempty string: " + field);
        return item.GetString()!;
    }

    public static int Number(JsonElement value, string field)
    {
        Require(value.TryGetProperty(field, out JsonElement item) && item.TryGetInt32(out int _) &&
                item.GetInt32() > 0, "Expected positive integer: " + field);
        return item.GetInt32();
    }

    public static bool Boolean(JsonElement value, string field)
    {
        Require(value.TryGetProperty(field, out JsonElement item) &&
                item.ValueKind is JsonValueKind.True or JsonValueKind.False, "Expected boolean: " + field);
        return item.GetBoolean();
    }

    public static JsonElement[] Rows(JsonElement value, string field, bool nonempty = true)
    {
        Require(value.TryGetProperty(field, out JsonElement item) && item.ValueKind == JsonValueKind.Array,
            "Expected array: " + field);
        JsonElement[] rows = item.EnumerateArray().ToArray();
        Require(!nonempty || rows.Length > 0, "Empty required array: " + field);
        return rows;
    }

    public static string[] Strings(JsonElement value, string field)
    {
        string[] values = Rows(value, field).Select(item =>
        {
            Require(item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()),
                "Expected nonempty string in " + field);
            return item.GetString()!;
        }).ToArray();
        Unique(values, field);
        return values;
    }

    public static void Unique(IEnumerable<string> values, string name)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string value in values)
            Require(seen.Add(value), "Duplicate " + name + ": " + value);
    }

    public static void Hash(string value)
    {
        Require(value.Length == 64 && value.All(Uri.IsHexDigit), "Expected SHA-256 hex digest.");
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private static void RejectDuplicates(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                Require(names.Add(property.Name), "Duplicate JSON property: " + property.Name);
                RejectDuplicates(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (JsonElement child in value.EnumerateArray()) RejectDuplicates(child);
    }
}
