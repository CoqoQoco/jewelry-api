using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.AddProductCost
{
    public class Request
    {
        public string StockNumber { get; set; }
        public string? PlanRunning { get; set; }


        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }

        public string? Remark { get; set; }
        public decimal TagPriceMultiplier { get; set; } = 1;

        public string? CurrencyUnit { get; set; }
        public decimal? CurrencyRate { get; set; }

        public List<RequestItem> Prictransection { get; set; } = new List<RequestItem>();

        public List<CustomStockInfoItem>? CustomStockInfo { get; set; }

        public bool IsOriginCost { get; set; } = false;
    }

    public class RequestItem
    {
        public int No { get; set; }
        public string Name { get; set; }
        public string NameDescription { get; set; }
        public string NameGroup { get; set; }
        public DateTimeOffset? Date { get; set; }

        public decimal Qty { get; set; }
        public decimal QtyPrice { get; set; }
        public decimal QtyWeight { get; set; }
        public decimal QtyWeightPrice { get; set; }
        public decimal TotalPrice => Math.Round((Qty * QtyPrice) + (QtyWeight * QtyWeightPrice), 2);
    }

    public class CustomStockInfoItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
