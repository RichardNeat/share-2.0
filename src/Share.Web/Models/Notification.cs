namespace Share.Web.Models;

public enum NotificationEventType
{
    ApplicationReceived,
    VisaStatusChanged,
    GuestArrived,
    AlertRaised,
    AlertEscalated,
    AccessRequested,
    AccessGranted,
    AssignmentChanged,
    DuplicateSuggested,
    CommentAdded,
}

// One in-app notification. Born automatically from meaningful events.
// Different users care about different events (council users see their council's events, central sees escalations).
public class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public NotificationEventType EventType { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Link to related entity (case reference, guest GWF, etc.)
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}
