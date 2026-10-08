namespace Share.Web.Models;

// One matched row from an arrivals and decisions file (Home Office feed).
public class DecisionUpdate
{
    public int Id { get; set; }
    public int VisaApplicationId { get; set; }
    public VisaApplication? VisaApplication { get; set; }
    public int IngestRunId { get; set; }
    public IngestRun? IngestRun { get; set; }
    public int RowNumber { get; set; }
    public string? Gwf { get; set; }
    public string? Uan { get; set; }
    public DateOnly? DecisionDate { get; set; }
    public string? Decision { get; set; }
    public string? VoyageCode { get; set; }
    public string? ArrivalPort { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public string? PersonIdentifier { get; set; }
}
