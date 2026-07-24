using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleReport.PipelineSummary
{
    public class Response
    {
        public SummaryData Summary { get; set; } = new SummaryData();
        public FunnelData Funnel { get; set; } = new FunnelData();
        public List<MonthlyQuotationData> MonthlyQuotation { get; set; } = new List<MonthlyQuotationData>();
        public List<TopCustomerData> TopCustomers { get; set; } = new List<TopCustomerData>();
    }

    public class SummaryData
    {
        public decimal TotalQuotationValue { get; set; }
        public int QuotationCount { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class FunnelData
    {
        public int QuotationCount { get; set; }
        public int SaleOrderCount { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class MonthlyQuotationData
    {
        public string Ym { get; set; } = null!;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    public class TopCustomerData
    {
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }
}
