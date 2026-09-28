using System;
using System.Collections.Generic;

namespace jewelry.Model.Report.Executive.Summary
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public ProductionData Production { get; set; } = new ProductionData();
        public ReceivablesData Receivables { get; set; } = new ReceivablesData();
        public SalesOrdersData SalesOrders { get; set; } = new SalesOrdersData();
        public StockData Stock { get; set; } = new StockData();
        public List<GoldLossMonthData> GoldLoss { get; set; } = new List<GoldLossMonthData>();
    }

    public class ProductionData
    {
        public int OpenCount { get; set; }
        public int Moved30dCount { get; set; }
        public int Stale180dCount { get; set; }
        public int MeltedOpenCount { get; set; }
    }

    public class ReceivablesData
    {
        public int InvoiceCount { get; set; }
        public decimal InvoiceTotalThb { get; set; }
        public decimal PaidOrPartialThb { get; set; }
        public int UnpaidCount { get; set; }
        public decimal UnpaidThb { get; set; }
        public int OverdueCount { get; set; }
        public decimal OverdueThb { get; set; }
        public decimal UnpaidNotOverdueThb { get; set; }
        public int NoDueDateCount { get; set; }
    }

    public class SalesOrdersData
    {
        public int OpenCount { get; set; }
        public int NoInvoiceCount { get; set; }
        public decimal NoInvoiceThb { get; set; }
        public int OverdueNoInvoiceCount { get; set; }
        public int NoDeliveryDateCount { get; set; }
    }

    public class StockData
    {
        public int InStockCount { get; set; }
        public int NoCostCount { get; set; }
        public decimal CostThb { get; set; }
        public int AgedOver1yCount { get; set; }
    }

    public class GoldLossMonthData
    {
        public string Month { get; set; } = null!;
        public int SlipCount { get; set; }
        public decimal IssuedGram { get; set; }
        public decimal RawLossGram { get; set; }
        public decimal OverAllowedGram { get; set; }
        public decimal OverAllowedPercent { get; set; }
    }
}
