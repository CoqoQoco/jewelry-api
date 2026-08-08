namespace jewelry.Model.Sale.MaterialSale.Cancel
{
    public class Request
    {
        public string Running { get; set; } = null!;
        public string? CancelReason { get; set; }
    }
}
