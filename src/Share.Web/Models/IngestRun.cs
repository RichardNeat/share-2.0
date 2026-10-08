namespace Share.Web.Models;

public class IngestRun
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public int FileNumber { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int RecordsInFile { get; set; }
    public int Added { get; set; }
    public int AlreadyPresent { get; set; }
    public int PeopleAdded { get; set; }
    public int AccommodationsAdded { get; set; }
    public int CasesAdded { get; set; }
    public int StatusesChanged { get; set; }
    public int Arrivals { get; set; }

    public bool IsArrivalsFile => FileName.EndsWith("-arrivals.csv");
    public List<SkippedRow> SkippedRows { get; set; } = [];
    public List<TimelineEvent> Events { get; set; } = [];
}

public class SkippedRow
{
    public int Id { get; set; }
    public int IngestRunId { get; set; }
    public int RowNumber { get; set; }
    public string? Reference { get; set; }
    public required string Reason { get; set; }
}
