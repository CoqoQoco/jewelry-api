using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Sale.SaleOrder.Get
{
    public class Response
    {
        public string Running { get; set; }
        public string SoNumber { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }

        public string? RefQuotation { get; set; }

        public int Payment { get; set; }
        public string? PaymentName { get; set; }

        public int? DepositPercent { get; set; }

        public string Priority { get; set; }

        public string? Data { get; set; }

        // Customer Information
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerRemark { get; set; }

        // Currency and Pricing
        public string CurrencyUnit { get; set; }
        public decimal CurrencyRate { get; set; }
        public decimal? Markup { get; set; }
        public decimal? Discount { get; set; }
        public decimal? GoldRate { get; set; }

        public string? Remark { get; set; }

        public List<StockConfirm> StockConfirm { get; set; }
        
    }

    public class StockConfirm
    {
        public long Id { get; set; }
        public string StockNumber { get; set; }

        public bool IsConfirm { get; set; }
        public string PriceOrigin { get; set; }

        public bool IsInvoice => !string.IsNullOrEmpty(Invoice);
        public string? Invoice { get; set; }
        public string? InvoiceItem { get; set; }
    }
}