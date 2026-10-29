namespace jewelry.Model.Stock.Movement.MoveUnits
{
    public class Response
    {
        public string Message { get; set; } = null!;
        public int MovedCount { get; set; }
        public List<MovedLot> Lots { get; set; } = new();
    }

    public class MovedLot
    {
        public string StockNumber { get; set; } = null!;
        public int Moved { get; set; }
        public string PrimaryLocation { get; set; } = null!;
    }
}
