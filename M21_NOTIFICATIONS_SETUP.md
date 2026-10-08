# M21 Notifications Setup

This document describes the notification infrastructure that has been prepared for M21. Full implementation is pending, but the foundation is in place and ready to plug in.

## What's Been Added

### 1. Data Model
- **`Models/Notification.cs`**: Core notification entity with:
  - `UserId`: Target user for the notification
  - `EventType`: Enum of notification event types (application received, visa status changed, guest arrived, alert raised, etc.)
  - `Title` and `Description`: Notification content
  - `IsRead`: Track read/unread status
  - `CreatedAt`: Timestamp (UTC)
  - `RelatedEntityType` and `RelatedEntityId`: Link to related records (e.g., case, guest)

### 2. Service Layer
- **`Services/NotificationService.cs`**: INotificationService interface and implementation
  - `CreateNotificationAsync()`: Add new notification
  - `GetUnreadNotificationsAsync()`: Get unread notifications for a user
  - `GetAllNotificationsAsync()`: Get all notifications for a user
  - `MarkAsReadAsync()`: Mark single notification as read
  - `MarkAllAsReadAsync()`: Mark all notifications as read
  - `GetUnreadCountAsync()`: Get count of unread notifications
- Registered in `Program.cs` as scoped dependency

### 3. Database
- **Migration**: `20261008112100_AddNotifications.cs`
  - Creates Notifications table
  - Indexes on (UserId, IsRead) and CreatedAt for efficient queries
  - Run with `dotnet ef database update`

### 4. UI Components (Placeholder)
- **`Pages/Notifications/Index.cshtml.cs`**: Page model with TODO markers
- **`Pages/Notifications/Index.cshtml`**: Template with structure ready to fill
- **`Pages/Shared/_NotificationBell.cshtml`**: Partial for header notification bell
- **`Helpers/NotificationHelpers.cs`**: Display helpers for event types and colours

## Integration Points for M21

### 1. Authentication Context
Replace `userId` placeholders in:
- `Pages/Notifications/Index.cshtml.cs` → Get from User.Identity
- `Pages/Shared/_NotificationBell.cshtml` → Get from User.Identity

### 2. Create Notifications When Events Occur
Add notification creation in these services:

**IngestService** (when processing arrival files):
```csharp
await _notificationService.CreateNotificationAsync(new Notification
{
    UserId = councilUserId,
    EventType = NotificationEventType.GuestArrived,
    Title = $"Guest {guest.GivenName} has arrived",
    Description = $"Recorded arrival on {guest.ArrivalDate:d}",
    RelatedEntityType = "Guest",
    RelatedEntityId = guest.Id
});
```

**Other services** (when relevant actions occur):
- Alert creation → AlertRaised event
- Alert escalation → AlertEscalated event
- Visa status changes → VisaStatusChanged event
- Access requests → AccessRequested event
- etc.

### 3. UI Implementation
In `Pages/Notifications/Index.cshtml`:
- Loop through `Model.Notifications`
- Display each with title, description, event type tag (using `NotificationHelpers`)
- Show time relative to now (e.g., "2 hours ago")
- Link to related record if available
- Mark as read action

In `_Layout.cshtml`:
- Include `_NotificationBell.cshtml` partial
- Inject `INotificationService` to get unread count
- Add link to notifications page

### 4. Event Type Enum Extension
As new events are needed, add to `NotificationEventType` enum in `Models/Notification.cs` and update:
- `GetEventTypeDisplay()` in `NotificationHelpers.cs`
- `GetEventTypeColour()` in `NotificationHelpers.cs`

## Demo Readiness

The demo will benefit from:
1. Watching the notification bell tick up as files are ingested live
2. Clicking through to see what changed (links to guests, cases, etc.)
3. Showing the read/unread system in action

## Next Steps (M21 Full Implementation)

1. Add `using System.Security.Claims` to get actual user ID from HttpContext
2. Update notification creation calls throughout the codebase
3. Implement the Notifications index page full UI with GOV.UK styling
4. Add notification bell to layout with dynamic unread count
5. Add POST handlers for mark as read actions
6. Add tests for notification creation, retrieval, and read/unread transitions
7. Consider pagination for the notifications list
8. Consider filtering by event type or read status
9. Add a digest view option (email-style summary of recent events)
