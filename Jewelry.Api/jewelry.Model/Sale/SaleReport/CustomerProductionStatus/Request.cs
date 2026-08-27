namespace jewelry.Model.Sale.SaleReport.CustomerProductionStatus
{
    public class Request
    {
        public bool OnlyMyCustomers { get; set; } = true;
        public int Take { get; set; } = 10;
        public int Skip { get; set; } = 0;
    }
}
