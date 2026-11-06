using System.Text;

namespace LibraryBookLendingSystem.Helpers;

/// <summary>Small CSV reader/writer used for importing old records and exporting the inventory.</summary>
public static class CsvUtil
{
    public static List<string[]> Parse(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = false;
                }
                else cell.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { row.Add(cell.ToString().Trim()); cell.Clear(); }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString().Trim());
                cell.Clear();
                if (row.Any(x => x.Length > 0)) rows.Add(row.ToArray());
                row = new List<string>();
            }
            else cell.Append(c);
        }

        row.Add(cell.ToString().Trim());
        if (row.Any(x => x.Length > 0)) rows.Add(row.ToArray());
        return rows;
    }

    public static string Escape(string? value)
    {
        value ??= "";
        return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
