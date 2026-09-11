using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleReport.ByChannel
{
    public class Response
    {
        public SummaryData Summary { get; set; } = new SummaryData();
        public List<ByDayData> ByDay { get; set; } = new List<ByDayData>();
        public List<BySellerData> BySeller { get; set; } = new List<BySellerData>();
        public List<TopProductData> TopProducts { get; set; } = new List<TopProductData>();
        public List<PaymentMixData> PaymentMix { get; set; } = new List<PaymentMixData>();
    }

    public class CurrencyTotal
    {
        public string CurrencyUnit { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    public class SummaryData
    {
        public int InvoiceCount { get; set; }
        public int PieceCount { get; set; }
        public List<CurrencyTotal> Amounts { get; set; } = new List<CurrencyTotal>();
    }

    public class ByDayData
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public List<CurrencyTotal> Amounts { get; set; } = new List<CurrencyTotal>();
    }

    public class BySellerData
    {
        public string SellerUsername { get; set; } = null!;
        public int InvoiceCount { get; set; }
        public List<CurrencyTotal> Amounts { get; set; } = new List<CurrencyTotal>();
    }

    public class TopProductData
    {
        public string ProductCode { get; set; } = null!;
        public int PieceCount { get; set; }
    }

    public class PaymentMixData
    {
        public int Payment { get; set; }
        public int InvoiceCount { get; set; }
    }
}
