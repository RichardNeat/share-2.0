using Share.Web.Ingest;
using Share.Web.Models;
using static Share.Web.Models.VisaStatus;

namespace Share.Tests.Unit;

public class VisaStatusRulesTests
{
    static readonly DateTime Landed = new(2026, 2, 26, 10, 15, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("GRANT", true, Arrived)]
    [InlineData("Issued", true, Arrived)]
    [InlineData("GRANT", false, Issued)]
    [InlineData("issued", false, Issued)]
    [InlineData("Withdrawn", false, Withdrawn)]
    [InlineData("Refused", false, Refused)]
    [InlineData("Voided", false, Confirmed)]
    [InlineData("Something new", false, Confirmed)]
    [InlineData(null, false, Confirmed)]
    public void Each_decision_maps_to_a_status(string? decision, bool arrived, VisaStatus expected) =>
        Assert.Equal(expected, VisaStatusRules.FromUpdate(decision, arrived ? Landed : null));

    [Fact]
    public void Never_updated_is_pending() =>
        Assert.Equal(Pending, VisaStatusRules.Derive([]));

    [Theory]
    [InlineData(new[] { Confirmed, Refused }, Refused)]
    [InlineData(new[] { Refused, Withdrawn }, Withdrawn)]
    [InlineData(new[] { Withdrawn, Issued }, Issued)]
    [InlineData(new[] { Arrived, Issued, Refused }, Arrived)]
    [InlineData(new[] { Issued, Confirmed }, Issued)]
    public void On_conflict_the_highest_precedence_wins(VisaStatus[] updates, VisaStatus expected) =>
        Assert.Equal(expected, VisaStatusRules.Derive(updates));
}

public class ArrivalsParserTests
{
    const string Header = "WEBVAF,UNIQUE_APPLICATION_NUMBER,DECISION_DATE,DECISION,VOY_CODE,VOY_ARR_PORT,VOY_ARR_DATETIME,PERSON_IDENTIFIER\n";

    [Fact]
    public void Reads_a_full_row_and_converts_uk_time_to_utc()
    {
        var row = Assert.Single(ArrivalsParser.Parse(Header + "GWF063900037,1313-2026-0001-7919,2026-02-20,GRANT,FR1234,LGW,26/02/2026 10:15:00,32001\n"));
        Assert.Equal("GWF063900037", row.Gwf);
        Assert.Equal("1313-2026-0001-7919", row.Uan);
        Assert.Equal(new DateOnly(2026, 2, 20), row.DecisionDate);
        Assert.Equal("GRANT", row.Decision);
        Assert.Equal("FR1234", row.VoyageCode);
        Assert.Equal("LGW", row.ArrivalPort);
        Assert.Equal(new DateTime(2026, 2, 26, 10, 15, 0), row.ArrivedAtUtc);   // GMT in February
        Assert.Null(row.Problem);
    }

    [Fact]
    public void Summer_arrivals_are_converted_from_british_summer_time() =>
        Assert.Equal(new DateTime(2026, 6, 15, 9, 0, 0), ArrivalsParser.Parse(Header + "GWF1,,2026-06-01,GRANT,X1,LHR,15/06/2026 10:00:00,1\n")[0].ArrivedAtUtc);

    [Fact]
    public void Blank_cells_are_null_and_rows_keep_their_line_number()
    {
        var rows = ArrivalsParser.Parse(Header + ",1313-2026-0005-9595,2026-02-22,Issued,,,,32004\n\nGWF2,,,,,,,\n");
        Assert.Equal(2, rows.Count);
        Assert.Null(rows[0].Gwf);
        Assert.Null(rows[0].ArrivedAtUtc);
        Assert.Equal(1, rows[0].RowNumber);
        Assert.Equal(3, rows[1].RowNumber);
    }

    [Fact]
    public void Quoted_fields_and_a_bad_date_are_handled_without_throwing()
    {
        var row = ArrivalsParser.Parse(Header + "\"GWF3\",\"\",not-a-date,\"GRANT, late\",,,,\n")[0];
        Assert.Equal("GWF3", row.Gwf);
        Assert.Equal("GRANT, late", row.Decision);
        Assert.Contains("not a date", row.Problem);
    }
}
