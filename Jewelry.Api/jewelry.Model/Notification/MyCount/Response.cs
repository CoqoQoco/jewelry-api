namespace jewelry.Model.Notification.MyCount;

public class Response
{
    public int Total { get; set; }
    public List<ModuleCount> List { get; set; } = new();
}

public class ModuleCount
{
    public string Module { get; set; } = null!;
    public int Count { get; set; }
}
