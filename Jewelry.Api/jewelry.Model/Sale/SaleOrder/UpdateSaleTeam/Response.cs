namespace jewelry.Model.Sale.SaleOrder.UpdateSaleTeam
{
    public class Response
    {
        public string SoNumber { get; set; } = string.Empty;
        public string? SalePerson { get; set; }
        public string? SaleSupport { get; set; }
        public int UpdatedInvoiceCount { get; set; }
    }
}
