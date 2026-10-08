using Share.Web.Models;

namespace Share.Web.Helpers;

public static class NotificationHelpers
{
    public static string GetEventTypeDisplay(NotificationEventType eventType) =>
        eventType switch
        {
            NotificationEventType.ApplicationReceived => "Application received",
            NotificationEventType.VisaStatusChanged => "Visa status changed",
            NotificationEventType.GuestArrived => "Guest arrived",
            NotificationEventType.AlertRaised => "Alert raised",
            NotificationEventType.AlertEscalated => "Alert escalated",
            NotificationEventType.AccessRequested => "Access requested",
            NotificationEventType.AccessGranted => "Access granted",
            NotificationEventType.AssignmentChanged => "Assignment changed",
            NotificationEventType.DuplicateSuggested => "Duplicate suggested",
            NotificationEventType.CommentAdded => "Comment added",
            _ => "Unknown event"
        };

    public static string GetEventTypeColour(NotificationEventType eventType) =>
        eventType switch
        {
            NotificationEventType.AlertRaised => "red",
            NotificationEventType.AlertEscalated => "red",
            NotificationEventType.AccessRequested => "blue",
            NotificationEventType.DuplicateSuggested => "yellow",
            _ => "grey"
        };

    public static string GetRelatedEntityLink(Notification notification) =>
        notification.RelatedEntityType switch
        {
            "VisaApplication" => $"/VisaApplications/Detail/{notification.RelatedEntityId}",
            "Guest" => $"/Guests/Detail/{notification.RelatedEntityId}",
            "Case" => $"/Cases/Detail/{notification.RelatedEntityId}",
            _ => "#"
        };
}
