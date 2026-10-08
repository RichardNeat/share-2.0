namespace Share.Web.Models;

public class Announcement
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishAt { get; set; }
    public bool IsHidden { get; set; }
}
