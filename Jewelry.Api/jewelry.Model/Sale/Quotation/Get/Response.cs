using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Sale.Quotation.Get
{
    public class Response
    {
        public string Number { get; set; }
        public string Running { get; set; }

        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        public string Currency { get; set; }
        public decimal CurrencyRate { get; set; }

        public decimal MarkUp { get; set; }
        public decimal Discount { get; set; }

        public string Remark { get; set; }
        public string Data { get; set; }

        public decimal? Freight { get; set; }
        public decimal? SpecialDiscount { get; set; }
        public decimal? SpecialAddition { get; set; }
        public decimal? Vat { get; set; }
        public decimal? GoldPerOz { get; set; }
        public DateTimeOffset? Date { get; set; }
    }
}
