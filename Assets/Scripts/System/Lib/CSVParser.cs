using System.Collections.Generic;
using UnityEngine;

public static class CSVParser
{
    public static List<string[]> ParseRows(TextAsset asset, bool hasHeader = true)
    {
        var rows = new List<string[]>();

        if (!asset)
            return rows;

        string[] lines = asset.text.Split('\n');
        int startIndex = hasHeader ? 1 : 0;

        for (int index = startIndex; index < lines.Length; index++)
        {
            string line = lines[index].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
                continue;

            rows.Add(line.Split(','));
        }

        return rows;
    }
}
