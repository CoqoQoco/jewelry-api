using System;

namespace jewelry.Model.Report.Executive.SalesOrdersWithoutInvoice
{
    public class Item
    {
        public string Running { get; set; } = null!;
        public string SoNumber { get; set; } = null!;
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DateTime? SoDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string CurrencyUnit { get; set; } = null!;
        public decimal GrandTotal { get; set; }
        public decimal GrandTotalThb { get; set; }
        public bool IsOverdue { get; set; }
        public string? SalePerson { get; set; }
        public string? SaleChannelCode { get; set; }
    }
}
