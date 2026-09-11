namespace Jewelry.Service.Notification;

public interface INotificationService
{
    Task<jewelry.Model.Notification.MyCount.Response> MyCount();
    Task<jewelry.Model.Notification.MyList.Response> MyList(jewelry.Model.Notification.MyList.Request request);
    Task<string> MarkRead(jewelry.Model.Notification.MarkRead.Request request);
    Task<string> MarkDone(jewelry.Model.Notification.MarkDone.Request request);
    Task<string> Snooze(jewelry.Model.Notification.Snooze.Request request);
    Task<jewelry.Model.Notification.MyList.Response> TeamList(jewelry.Model.Notification.TeamList.Request request);
}
