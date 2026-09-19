using System;
using System.Globalization;
using System.Text;

/// <summary>Dependency-free cases run in both the Editor and the command-line verification host.</summary>
public static class LocalizeTests
{
    public static int RunPure()
    {
        int passed = 0;
        string original = "Id,Key,ko\n1001,UI_Confirm,확인\n1002,NPC_Thought_Hungry,배가 고프네…\n1003,Tutorial_Welcome,환영\n";
        Assert(LocalizeCsvParser.TryParse("\uFEFFId,Key,ko,en\r\n1,Example,\"안녕, \"\"마을\"\"\r\n환영\",Hello\r\n",
            out LocalizeCsvParser.Table table, out string error), error);
        Assert(table.Rows[0].Texts[0] == "안녕, \"마을\"\r\n환영" && table.Rows[0].Texts[1] == "Hello", "Quoted text fidelity");
        passed++;

        string[] invalid =
        {
            "", "\uFEFF", "Id,Key,en\n1,A,hello", "Id,Key,ko\n", "Id,Key,ko,ko\n1,A,a,b",
            "Id,Key,ko\n0,A,가", "Id,Key,ko\n-1,A,가", "Id,Key,ko\n2147483648,A,가",
            "Id,Key,ko\n1,A,가\n1,B,나", "Id,Key,ko\n1,A,가\n2,A,나",
            "Id,Key,ko\n1,1Bad,가", "Id,Key,ko\n1,class,가", "Id,Key,ko\n1,value__,가",
            "Id,Key,ko\n1,LocalizeKey,가", "Id,Key,ko\n1,A,", "Id,Key,ko\n1,A,   ",
            "Id,Key,ko\n1,A,가,나", "Id,Key,ko\n1,A", "Id,Key,ko\n1,A,\"abc",
            "Id,Key,ko\n1,A,ab\"cd", "Id,Key,ko\n1,A,\"abc\"x", "Id,Key,ko\n1,A,가\n\n"
        };
        foreach (string csv in invalid)
        {
            Assert(!LocalizeCsvParser.TryParse(csv, out table, out error) && table == null && error.Contains("line"),
                "Invalid CSV must fail atomically with line: " + csv);
            passed++;
        }
        Assert(!LocalizeCsvParser.TryParse("Id,Key,ko\n1,A,\"line1\nline2\"\n2,B,", out table, out error) &&
            error.Contains("line 4"), "Physical line after multiline field");
        passed++;
        Assert(LocalizeCsvParser.TryParse("Id,Key,ko\n1,키_1,문구", out table, out error), error);
        passed++;
        Assert(LocalizeKeySource.TryGenerate(original, null, out string source, out error), error);
        passed++;
        string reordered = "Id,Key,ko\n1003,Tutorial_Welcome,수정\n1001,UI_Confirm,수정\n1002,NPC_Thought_Hungry,수정\n";
        Assert(LocalizeKeySource.TryGenerate(reordered, source, out string reorderedSource, out error) && source == reorderedSource,
            "Reorder/text-only edit must leave enum bytes identical");
        passed++;
        Assert(LocalizeKeySource.TryGenerate(original + "1004,New_Key,새 문구\n", source, out string added, out error) &&
            added.Contains("New_Key = 1004,") && added.Contains("UI_Confirm = 1001,"), "Add stable ID");
        passed++;
        Assert(!LocalizeKeySource.TryGenerate(original, added, out _, out _), "Protect uncompiled added key history");
        passed++;
        foreach (string changed in new[]
        {
            original.Replace("1001,UI_Confirm,확인\n", ""), original.Replace("UI_Confirm", "Renamed"),
            original.Replace("1001", "9999"), original.Replace("UI_Confirm", "ui_confirm")
        })
        {
            Assert(!LocalizeKeySource.TryGenerate(changed, source, out string output, out error) && output == string.Empty,
                "Existing identity must not change");
            passed++;
        }
        Assert(!LocalizeKeySource.TryGenerate(original, "public enum LocalizeKey {}", out _, out _), "Reject corrupt baseline");
        passed++;
        Assert(LocalizeKeySource.TryGenerate(original, source.Replace("\n", "\r\n"), out string crlf, out error) && crlf == source,
            "CRLF baseline");
        passed++;
        string compiledFixture = CreateCompiledEnumFixture();
        Assert(LocalizeCsvParser.TryParse(compiledFixture, out table, out error) && LocalizeKeyValidation.TryValidate(table, out error), error);
        passed++;
        LocalizeCsvParser.TryParse(compiledFixture.Replace("UI_Confirm", "ui_confirm"), out table, out error);
        Assert(!LocalizeKeyValidation.TryValidate(table, out error), "Exact enum case check");
        passed++;
        LocalizeCsvParser.TryParse(compiledFixture.Replace("1001,UI_Confirm,fixture\n", ""), out table, out error);
        Assert(!LocalizeKeyValidation.TryValidate(table, out error), "Runtime must reject removed enum row");
        passed++;
        return passed;
    }

    public static string CreateCompiledEnumFixture()
    {
        var csv = new StringBuilder("Id,Key,ko\n");
        foreach (LocalizeKey key in Enum.GetValues(typeof(LocalizeKey)))
            csv.Append(((int)key).ToString(CultureInfo.InvariantCulture)).Append(',').Append(key).Append(",fixture\n");
        return csv.ToString();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
