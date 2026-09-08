namespace Jewelry.Model.Stock.Reconciliation
{
    public class RebuildBalanceResult
    {
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int ZeroedCount { get; set; }
        public int UnchangedCount { get; set; }
    }
}
