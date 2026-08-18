namespace jewelry.Model.Sale.ExportShipment.List;

public class Response
{
    public string Running { get; set; }
    public string DocumentNumber { get; set; }
    public string? CustomNumber { get; set; }
    public DateTime DocumentDate { get; set; }
    public string? ConsigneeName { get; set; }
    public string? EventName { get; set; }
    public string? BoothNo { get; set; }
    public string? Currency { get; set; }
    public int? ParcelCount { get; set; }
    public int ItemCount { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; }
}
