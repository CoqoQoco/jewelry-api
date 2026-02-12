using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.ListStockCostPlan
{
    public class Response
    {
        public string Running { get; set; }
        public string StockNumber { get; set; }
        public string? Remark { get; set; }

        public int StatusId { get; set; }
        public string StatusName { get; set; }

        public string? VersionRunning { get; set; }
        public bool? IsMobileActive { get; set; }
        public bool? IsActive { get; set; }

        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
