namespace jewelry.Model.Stock.Movement.Move
{
    public class Request
    {
        public List<string> StockNumbers { get; set; } = new();
        public string TargetLocationCode { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
