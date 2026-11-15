using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class DefenseWaveCsvMapper
{
    private static readonly string[] _header = { "wave", "order", "enemyId", "count", "spawnInterval" };

    // Failure publishes no partial plan. Records count nonblank rows, including the header.
    public static bool TryMap(TextAsset csv, IReadOnlyList<DefenseWaveEnemyEntry> enemies,
        out DefenseWavePlan[] waves, out string error)
    {
        waves = Array.Empty<DefenseWavePlan>();
        error = "CSV key '_csv': missing TextAsset.";
        if (!csv) return false;
        List<string[]> rows = CSVParser.ParseRows(csv, false);
        error = "CSV record 1 (header): expected wave,order,enemyId,count,spawnInterval and at least one data record.";
        if (rows.Count < 2 || rows[0].Length != _header.Length) return false;
        for (int column = 0; column < _header.Length; ++column)
        {
            string cell = rows[0][column].Trim();
            if (column == 0) cell = cell.TrimStart('\uFEFF').Trim();
            if (cell != _header[column])
            {
                error = $"CSV record 1: expected header key '{_header[column]}', got '{cell}'.";
                return false;
            }
        }

        error = "CSV mapping key '_enemies': missing enemy catalog.";
        if (enemies == null || enemies.Count == 0) return false;
        var mappings = new Dictionary<string, DefenseWaveEnemyEntry>(StringComparer.Ordinal);
        for (int index = 0; index < enemies.Count; ++index)
        {
            DefenseWaveEnemyEntry entry = enemies[index];
            string id = entry.Id?.Trim();
            error = $"CSV mapping entry {index + 1}, enemyId='{id}': invalid/duplicate ID or missing prefab/stat definition.";
            if (!IsValidId(id) || mappings.ContainsKey(id) || !entry.Prefab || !entry.StatDefinition)
                return false;
            mappings.Add(id, entry);
        }

        var groups = new List<DefenseWaveSpawnGroup>();
        for (int index = 1; index < rows.Count; ++index)
        {
            string[] row = rows[index];
            string record = $"CSV record {index + 1}";
            error = $"{record}: expected exactly five unquoted cells.";
            if (row.Length != _header.Length) return false;
            for (int column = 0; column < row.Length; ++column)
            {
                if (row[column].Contains("\"")) return false;
                row[column] = row[column].Trim();
            }
            error = $"{record}, wave='{row[0]}': expected a positive Int32.";
            if (!TryPositiveInt(row[0], out int wave)) return false;
            error = $"{record}, wave={wave}, order='{row[1]}': expected a positive Int32.";
            if (!TryPositiveInt(row[1], out int order)) return false;
            record += $" (wave={wave}, order={order}, enemyId='{row[2]}')";
            error = $"{record}: unknown or unmapped enemyId.";
            if (!mappings.TryGetValue(row[2], out DefenseWaveEnemyEntry enemy)) return false;
            error = $"{record}, count='{row[3]}': expected a positive Int32.";
            if (!TryPositiveInt(row[3], out int count)) return false;
            error = $"{record}, spawnInterval='{row[4]}': expected a finite positive number.";
            if (!float.TryParse(row[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float interval) ||
                interval <= 0f || float.IsNaN(interval) || float.IsInfinity(interval)) return false;
            groups.Add(new DefenseWaveSpawnGroup(index + 1, wave, order, row[2], count, interval,
                enemy.Prefab, enemy.StatDefinition));
        }

        groups.Sort((left, right) => left.Wave != right.Wave
            ? left.Wave.CompareTo(right.Wave) : left.Order.CompareTo(right.Order));
        var result = new List<DefenseWavePlan>();
        int start = 0;
        int planTotal = 0;
        while (start < groups.Count)
        {
            DefenseWaveSpawnGroup first = groups[start];
            error = $"{first.SourceKey}: wave numbers must be contiguous from 1; expected {result.Count + 1}.";
            if (first.Wave != result.Count + 1) return false;
            int end = start;
            int waveTotal = 0;
            int previousOrder = 0;
            while (end < groups.Count && groups[end].Wave == first.Wave)
            {
                DefenseWaveSpawnGroup group = groups[end];
                error = $"{group.SourceKey}: duplicate order within wave.";
                if (group.Order == previousOrder) return false;
                previousOrder = group.Order;
                error = $"{group.SourceKey}: enemy count total exceeds Int32 capacity.";
                try
                {
                    waveTotal = checked(waveTotal + group.Count);
                    planTotal = checked(planTotal + group.Count);
                }
                catch (OverflowException) { return false; }
                ++end;
            }
            result.Add(new DefenseWavePlan(groups.GetRange(start, end - start).ToArray(), waveTotal));
            start = end;
        }
        waves = result.ToArray();
        error = null;
        return true;
    }

    private static bool TryPositiveInt(string value, out int number)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number > 0;

    private static bool IsValidId(string id)
        => !string.IsNullOrWhiteSpace(id) && id.IndexOf(',') < 0 && id.IndexOf('"') < 0 &&
            id.IndexOf('\r') < 0 && id.IndexOf('\n') < 0;
}
