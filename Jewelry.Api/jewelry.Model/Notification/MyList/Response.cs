namespace jewelry.Model.Notification.MyList;

public class Response
{
    public int Total { get; set; }
    public List<Item> List { get; set; } = new();
}

public class Item
{
    public long Id { get; set; }
    public string TypeCode { get; set; } = null!;
    public string? TypeName { get; set; }
    public string? Module { get; set; }
    public string? Icon { get; set; }
    public string Title { get; set; } = null!;
    public string? Body { get; set; }
    public string? RefDocType { get; set; }
    public string? RefDocNo { get; set; }
    public string? ActionUrl { get; set; }
    public string Severity { get; set; } = null!;
    public decimal? Amount { get; set; }
    public string? CurrencyUnit { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int OverdueDays { get; set; }
    public string State { get; set; } = null!;
    public bool IsEscalated { get; set; }
    public string RecipientUsername { get; set; } = null!;
}
