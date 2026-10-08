namespace Share.Web.Models;

// A property guests will live in, identified by its address and council (see RecordKeys).
public class Accommodation
{
    public int Id { get; set; }
    public required string MatchKey { get; set; }
    public required string Address { get; set; }
    public string? Postcode { get; set; }
    public string? Council { get; set; }
    public List<VisaApplication> Applications { get; set; } = [];
}
