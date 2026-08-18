namespace jewelry.Model.Sale.ExportShipment.RemoveItems;

public class Request
{
    public string Running { get; set; }
    public List<long> ItemIds { get; set; } = new();
}
