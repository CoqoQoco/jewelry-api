using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.Picklist
{
    public class PicklistResponse
    {
        public string Running { get; set; }
        public int Type { get; set; }

        public DateTime RequestDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Stastus { get; set; } 
        public string? Remark { get; set; }
        public bool IsOverPick { get; set; }


        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } 
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public IEnumerable<PicklistItem> Items { get; set; }
    }

    public class PicklistItem
    {
        public string Code { get; set; }
        public string GroupName { get; set; }
        public string Name { get; set; }

        public string? Size { get; set; }
        public string Shape { get; set; }
        public string Grade { get; set; }
        public string? GradeDia { get; set; }

        public string Status { get; set; }

        public DateTime RequestDate { get; set; }
        public string Running { get; set; }
        public int Type { get; set; }

        public string? JobOrPo { get; set; }
        public decimal? SupplierCost { get; set; }
        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }

        public decimal Qty { get; set; }
        public decimal QtyWeight { get; set; }

        public string? SubpplierName { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public string? WO { get; set; }
        public int? WONumber { get; set; }
        public string? WOText { get; set; }
        public string? Mold { get; set; }
    }
}
