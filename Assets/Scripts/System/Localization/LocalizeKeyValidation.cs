using System;

public static class LocalizeKeyValidation
{
    public static bool TryValidate(LocalizeCsvParser.Table table, out string error)
    {
        foreach (LocalizeCsvParser.Row row in table.Rows)
        {
            if (!string.Equals(Enum.GetName(typeof(LocalizeKey), row.Id), row.Key, StringComparison.Ordinal))
            {
                error = "Localize CSV line " + row.Line + ": Key/Id mismatch for '" + row.Key +
                    "' (" + row.Id + "). Regenerate LocalizeKey in the Editor.";
                return false;
            }
        }
        if (table.Rows.Count != Enum.GetNames(typeof(LocalizeKey)).Length)
        {
            error = "Localize CSV: a generated key was removed; keep all existing rows.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}
