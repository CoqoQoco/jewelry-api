using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanGet
{
    public class ProductionPlanGetResponse
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public DateTime? RequestDate { get; set; }
        public string? Mold { get; set; }

        public string? ProductRunning { get; set; }
        public string? ProductName { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductDetail { get; set; }

        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }

        public int? ProductQty { get; set; }
        public string? ProductQtyUnit { get; set; }

        public string? CustomerNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerType { get; set; }
        public string? CustomerTypeName { get; set; }

        public bool? IsActive { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? Remark { get; set; }

        public List<StatusDetailHeader>? TbtProductionPlanStatusHeader { get; set; }
    }

    public class StatusDetailHeader 
    {
        public int Id { get; set; }
        public int ProductionPlanId { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public int Status { get; set; }

        public string? SendName { get; set; }
        public DateTime? SendDate { get; set; }
        public string? CheckName { get; set; }
        public DateTime? CheckDate { get; set; }

        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }

        public bool IsActive { get; set; }
        public decimal? WagesTotal { get; set; }

        public List<StatusDetailDetail>? TbtProductionPlanStatusDetail { get; set; }
    }
    public class StatusDetailDetail 
    {
        public int ProductionPlanId { get; set; }
        public string ItemNo { get; set; }
        public int HeaderId { get; set; }

        public string? Gold { get; set; }
        public decimal? GoldQtySend { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldQtyCheck { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public decimal? GoldWeightDiff { get; set; }
        public decimal? GoldWeightDiffPercent { get; set; }

        public bool IsActive { get; set; }

        public string? Description { get; set; }
        public string? Worker { get; set; }
        public decimal? Wages { get; set; }
        public decimal? TotalWages { get; set; }
    }
}
