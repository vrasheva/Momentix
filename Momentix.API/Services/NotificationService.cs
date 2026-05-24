using Momentix.Data.Data;
using Momentix.Data.Models;

namespace Momentix.API.Services;

public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public void Add(
        string userId,
        string title,
        string message,
        NotificationType type,
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        string? actorUserId = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            ActorUserId = actorUserId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public void AddForUsers(
        IEnumerable<string> userIds,
        string title,
        string message,
        NotificationType type,
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        string? actorUserId = null)
    {
        foreach (var userId in userIds.Distinct())
        {
            Add(userId, title, message, type, relatedEntityType, relatedEntityId, actorUserId);
        }
    }
}
