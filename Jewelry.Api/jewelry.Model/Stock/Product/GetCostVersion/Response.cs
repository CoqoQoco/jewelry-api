using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.GetCostVersion
{
    public class Response
    {
        public string Running { get; set; }
        public string? JobRunning { get; set; }
        public string StockNumber { get; set; }

        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }

        public string? Remark { get; set; }
        public List<ResponseItem> Prictransection { get; set; } = new List<ResponseItem>();

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }

    public class ResponseItem
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
}
