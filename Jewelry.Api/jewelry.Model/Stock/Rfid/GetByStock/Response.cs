namespace jewelry.Model.Stock.Rfid.GetByStock;

public class Response
{
    public bool Found { get; set; }

    public string? Epc { get; set; }

    public string? Tid { get; set; }

    public DateTime? BoundDate { get; set; }
}
