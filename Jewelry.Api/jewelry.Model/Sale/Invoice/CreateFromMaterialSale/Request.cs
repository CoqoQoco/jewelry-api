namespace jewelry.Model.Sale.Invoice.CreateFromMaterialSale
{
    public class Request
    {
        public string MaterialSaleRunning { get; set; } = null!;

        public int Payment { get; set; }
        public string PaymentName { get; set; } = null!;
        public int PaymentDay { get; set; }

        public string? SaleChannelCode { get; set; }
        public string? Remark { get; set; }
    }
}
