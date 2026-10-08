namespace Share.Web.Models;

public class Comment
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case? Case { get; set; }
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string Text { get; set; }
}
