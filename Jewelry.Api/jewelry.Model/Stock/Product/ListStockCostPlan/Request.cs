using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.ListStockCostPlan
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public string? StockNumber { get; set; }
        public string? Running { get; set; }
        public int? StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? CreateDateFrom { get; set; }
        public DateTime? CreateDateTo { get; set; }
        public bool? IsActive { get; set; }
    }
}
