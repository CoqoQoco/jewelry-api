using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Gem.Search
{
    public class SearchGemResponse
    {
        public int Id { get; set; }

        public string GroupName { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public string? Size { get; set; }
        public string Shape { get; set; }
        public string Grade { get; set; }
        public string? GradeDia { get; set; }

        public decimal Quantity { get; set; }
        public decimal QuantityOnProcess { get; set; }
        public decimal QuantityWeight { get; set; }
        public decimal QuantityWeightOnProcess { get; set; }

        public decimal Price { get; set; }
        public decimal PriceQty { get; set; }
        public string? Unit { get; set; }
        public string? UnitCode { get; set; }

        public int? Wg { get; set; }
        public int? Daterec { get; set; }
        public string? Original { get; set; }

        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
