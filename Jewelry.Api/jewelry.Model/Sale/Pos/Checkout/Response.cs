namespace jewelry.Model.Sale.Pos.Checkout
{
    public class Response
    {
        public string SoNumber { get; set; } = null!;
        public string InvoiceNumber { get; set; } = null!;
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsDuplicate { get; set; }
    }
}
