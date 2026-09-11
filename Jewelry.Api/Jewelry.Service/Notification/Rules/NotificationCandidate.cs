namespace Jewelry.Service.Notification.Rules;

public class NotificationCandidate
{
    public string RecipientUsername { get; set; } = null!;
    public string RefDocType { get; set; } = null!;
    public string RefDocNo { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Body { get; set; }
    public decimal? Amount { get; set; }
    public string? CurrencyUnit { get; set; }
    public DateTime? DueDate { get; set; }
    public string? ActionUrl { get; set; }
}
