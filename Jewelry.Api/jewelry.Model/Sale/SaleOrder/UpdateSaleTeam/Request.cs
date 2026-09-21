namespace jewelry.Model.Sale.SaleOrder.UpdateSaleTeam
{
    public class Request
    {
        public string SoNumber { get; set; } = null!;
        public string? SalePerson { get; set; }
        public string? SaleSupport { get; set; }
    }
}
