using System.Collections.Generic;
using System.Globalization;

public static class BuilderWorkCsvMapper
{
    public static bool TryMap(List<string[]> rows, out BuilderWorkDefinition definition, out string error)
    {
        definition = null;
        error = "BuilderWork.csv: expected one row of four finite positive values.";
        if (rows == null || rows.Count != 1 || rows[0].Length != 4)
            return false;
        var values = new float[4];
        for (int i = 0; i < values.Length; ++i)
            if (!float.TryParse(rows[0][i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])
                || values[i] <= 0f || float.IsInfinity(values[i]) || float.IsNaN(values[i]))
                return false;
        definition = new BuilderWorkDefinition(values[0], values[1], values[2], values[3]);
        error = null;
        return true;
    }
}
