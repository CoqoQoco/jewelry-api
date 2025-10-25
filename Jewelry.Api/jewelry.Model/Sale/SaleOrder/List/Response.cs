using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Sale.SaleOrder.List
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

        public string Priority { get; set; }

        // Customer Information
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }

        // Currency and Pricing
        public string CurrencyUnit { get; set; }
        public decimal CurrencyRate { get; set; }
        public decimal? Markup { get; set; }
        public decimal? GoldRate { get; set; }
    }
}