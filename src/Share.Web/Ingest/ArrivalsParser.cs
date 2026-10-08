using System.Globalization;

namespace Share.Web.Ingest;

public record ArrivalRow(int RowNumber, string? Gwf, string? Uan, DateOnly? DecisionDate, string? Decision,
    string? VoyageCode, string? ArrivalPort, DateTime? ArrivedAtUtc, string? PersonIdentifier, string? Problem);

// Parses an arrivals and decisions CSV. Columns are found by header name.
public static class ArrivalsParser
{
    static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static IReadOnlyList<ArrivalRow> Parse(string csv)
    {
        var lines = csv.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0) return [];
        var header = SplitLine(lines[0]).Select(h => h.Trim().ToUpperInvariant()).ToList();
        int Col(string name) => header.IndexOf(name);
        var rows = new List<ArrivalRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = SplitLine(lines[i]);
            string? Cell(string name)
            {
                var c = Col(name);
                return c >= 0 && c < cells.Count && !string.IsNullOrWhiteSpace(cells[c]) ? cells[c].Trim() : null;
            }

            string? problem = null;
            DateOnly? decisionDate = null;
            if (Cell("DECISION_DATE") is { } dd)
            {
                if (DateOnly.TryParseExact(dd, "yyyy-MM-dd", out var d)) decisionDate = d;
                else problem = $"Decision date '{dd}' is not a date";
            }
            DateTime? arrived = null;
            if (Cell("VOY_ARR_DATETIME") is { } ad)
            {
                // The feed gives UK local time (dd/MM/yyyy HH:mm:ss); we store UTC.
                if (DateTime.TryParseExact(ad, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
                    arrived = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), London);
                else problem = $"Arrival date and time '{ad}' is not a date";
            }
            rows.Add(new ArrivalRow(i, Cell("WEBVAF"), Cell("UNIQUE_APPLICATION_NUMBER"), decisionDate, Cell("DECISION"),
                Cell("VOY_CODE"), Cell("VOY_ARR_PORT"), arrived, Cell("PERSON_IDENTIFIER"), problem));
        }
        return rows;
    }

    // Comma separated, with double-quoted fields allowed.
    static List<string> SplitLine(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else current.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { cells.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        cells.Add(current.ToString());
        return cells;
    }
}
