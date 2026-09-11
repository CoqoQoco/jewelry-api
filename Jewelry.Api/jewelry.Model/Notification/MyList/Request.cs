namespace jewelry.Model.Notification.MyList;

public class Request
{
    public int Take { get; set; }
    public int Skip { get; set; }
    public string? Module { get; set; }
    public string? TypeCode { get; set; }
    public bool IncludeClosed { get; set; } = false;
}
