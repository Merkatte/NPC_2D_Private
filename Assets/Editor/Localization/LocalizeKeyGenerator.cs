using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class LocalizeKeyGenerator
{
    public const string CsvPath = "Assets/Data/CSV/LocalizeData.csv";
    public const string EnumPath = "Assets/Scripts/Enum/LocalizeKey.cs";

    [MenuItem("Tools/Localization/Regenerate LocalizeKey")]
    public static void Regenerate()
    {
        if (!TryRegenerate(out string error))
            Debug.LogError(error);
    }

    public static bool TryRegenerate(out string error)
    {
        try
        {
            string previous = File.Exists(EnumPath) ? File.ReadAllText(EnumPath, Encoding.UTF8) : null;
            // Never silently forget the stable ID history by recreating a deleted enum source.
            if (previous == null)
            {
                error = "LocalizeKey.cs is missing. Restore the generated file from version control.";
                return false;
            }
            if (!LocalizeKeySource.TryGenerate(ReadCsv(), previous, out string source, out error))
                return false;
            if (previous.Replace("\r\n", "\n") == source)
                return true;
            File.WriteAllText(EnumPath, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(EnumPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is DecoderFallbackException)
        {
            error = "LocalizeKey generation failed: " + exception.Message;
            return false;
        }
    }

    public static bool TryValidateBuild(out string error)
    {
        try
        {
            if (!File.Exists(EnumPath))
            {
                error = "LocalizeKey.cs is missing.";
                return false;
            }
            string csv = ReadCsv();
            string previous = File.ReadAllText(EnumPath, Encoding.UTF8);
            if (!LocalizeKeySource.TryGenerate(csv, previous, out string expected, out error))
                return false;
            if (previous.Replace("\r\n", "\n") != expected)
            {
                error = "CSV and LocalizeKey.cs differ. Use Tools/Localization/Regenerate LocalizeKey and wait for compilation.";
                return false;
            }
            return LocalizeCsvParser.TryParse(csv, out LocalizeCsvParser.Table table, out error) &&
                LocalizeKeyValidation.TryValidate(table, out error);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is DecoderFallbackException)
        {
            error = "Localize build validation failed: " + exception.Message;
            return false;
        }
    }

    private static string ReadCsv() => File.ReadAllText(CsvPath, new UTF8Encoding(false, true));
}
