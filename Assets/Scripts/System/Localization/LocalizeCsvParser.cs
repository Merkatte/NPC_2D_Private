using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

/// <summary>Strict CSV reader shared by the Editor generator and runtime data.</summary>
public static class LocalizeCsvParser
{
    public sealed class Row
    {
        public int Line { get; }
        public int Id { get; }
        public string Key { get; }
        public ReadOnlyCollection<string> Texts { get; }

        internal Row(int line, int id, string key, List<string> texts)
        {
            Line = line;
            Id = id;
            Key = key;
            Texts = texts.AsReadOnly();
        }
    }

    public sealed class Table
    {
        public ReadOnlyCollection<string> Languages { get; }
        public ReadOnlyCollection<Row> Rows { get; }

        internal Table(List<string> languages, List<Row> rows)
        {
            Languages = languages.AsReadOnly();
            Rows = rows.AsReadOnly();
        }
    }

    private static readonly HashSet<string> ReservedNames = new HashSet<string>(
        ("abstract as base bool break byte case catch char checked class const continue decimal default " +
         "delegate do double else enum event explicit extern false finally fixed float for foreach goto " +
         "if implicit in int interface internal is lock long namespace new null object operator out " +
         "override params private protected public readonly ref return sbyte sealed short sizeof " +
         "stackalloc static string struct switch this throw true try typeof uint ulong unchecked " +
         "unsafe ushort using virtual void volatile while __arglist __makeref __reftype __refvalue " +
         "LocalizeKey value__").Split(' '), StringComparer.Ordinal);

    public static bool IsValidKey(string key)
    {
        if (string.IsNullOrEmpty(key) || ReservedNames.Contains(key))
            return false;

        for (int i = 0; i < key.Length; i++)
        {
            UnicodeCategory category = char.GetUnicodeCategory(key[i]);
            bool isLetter = char.IsLetter(key[i]) || category == UnicodeCategory.LetterNumber;
            bool isContinuation = category == UnicodeCategory.DecimalDigitNumber ||
                category == UnicodeCategory.ConnectorPunctuation ||
                category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark;
            if (key[i] != '_' && !isLetter && (i == 0 || !isContinuation))
                return false;
        }
        return true;
    }

    public static bool TryParse(string csv, out Table table, out string error)
    {
        table = null;
        error = string.Empty;
        if (string.IsNullOrEmpty(csv))
            return Fail(1, "CSV is empty.", out error);

        var records = new List<List<string>>();
        var lines = new List<int>();
        if (!TryReadRecords(csv, records, lines, out error))
            return false;

        List<string> header = records[0];
        if (header.Count < 3 || header[0] != "Id" || header[1] != "Key")
            return Fail(1, "Expected header Id,Key,ko (optional language columns may follow).", out error);

        var languages = header.GetRange(2, header.Count - 2);
        var languageSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (string language in languages)
        {
            if (string.IsNullOrWhiteSpace(language) || language != language.Trim() || !languageSet.Add(language))
                return Fail(1, "Empty, padded or duplicate language column: '" + language + "'.", out error);
        }
        int koreanIndex = languages.IndexOf("ko");
        if (koreanIndex < 0)
            return Fail(1, "Required language column 'ko' is missing.", out error);
        if (records.Count < 2)
            return Fail(2, "At least one localization row is required.", out error);

        var ids = new HashSet<int>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<Row>();
        for (int i = 1; i < records.Count; i++)
        {
            List<string> fields = records[i];
            int line = lines[i];
            if (fields.Count != header.Count)
                return Fail(line, "Expected " + header.Count + " columns, got " + fields.Count + ".", out error);
            if (!int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id <= 0)
                return Fail(line, "Id must be a positive Int32: '" + fields[0] + "'.", out error);
            if (!ids.Add(id))
                return Fail(line, "Duplicate Id " + id + ".", out error);
            string key = fields[1];
            if (!IsValidKey(key))
                return Fail(line, "Invalid C# enum member: '" + key + "'.", out error);
            if (!keys.Add(key))
                return Fail(line, "Duplicate Key '" + key + "'.", out error);
            if (string.IsNullOrWhiteSpace(fields[koreanIndex + 2]))
                return Fail(line, "Empty Korean text for '" + key + "'.", out error);
            rows.Add(new Row(line, id, key, fields.GetRange(2, languages.Count)));
        }
        table = new Table(languages, rows);
        return true;
    }

    private static bool TryReadRecords(string csv, List<List<string>> records, List<int> lines, out string error)
    {
        error = string.Empty;
        var fields = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        bool closedQuote = false;
        int line = 1;
        int recordLine = 1;
        int start = csv[0] == '\uFEFF' ? 1 : 0;
        for (int i = start; i < csv.Length; i++)
        {
            char c = csv[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                        closedQuote = true;
                    }
                }
                else
                {
                    field.Append(c);
                    if (c == '\n' || (c == '\r' && (i + 1 == csv.Length || csv[i + 1] != '\n')))
                        line++;
                }
                continue;
            }

            if (c == ',' || c == '\r' || c == '\n')
            {
                fields.Add(field.ToString());
                field.Clear();
                closedQuote = false;
                if (c != ',')
                {
                    records.Add(fields);
                    lines.Add(recordLine);
                    fields = new List<string>();
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                        i++;
                    recordLine = ++line;
                }
            }
            else if (closedQuote)
                return Fail(line, "Unexpected character after closing quote.", out error);
            else if (c == '"')
            {
                if (field.Length != 0)
                    return Fail(line, "Quote inside an unquoted field.", out error);
                quoted = true;
            }
            else
                field.Append(c);
        }
        if (quoted)
            return Fail(recordLine, "Unterminated quoted field.", out error);
        if (fields.Count > 0 || field.Length > 0 || closedQuote)
        {
            fields.Add(field.ToString());
            records.Add(fields);
            lines.Add(recordLine);
        }
        if (records.Count == 0)
            return Fail(1, "CSV is empty.", out error);
        return true;
    }

    private static bool Fail(int line, string reason, out string error)
    {
        error = "Localize CSV line " + line + ": " + reason;
        return false;
    }
}
