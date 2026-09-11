namespace Jewelry.Service.Notification.Rules;

public interface INotificationRule
{
    string TypeCode { get; }
    Task<IReadOnlyList<NotificationCandidate>> Evaluate();
}
