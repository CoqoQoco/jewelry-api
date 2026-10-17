namespace jewelry.Model.Stock.Rfid.GetByEpcs;

public class Response
{
    public List<PieceInfo> Items { get; set; } = new();

    public List<string> NotFound { get; set; } = new();
}
