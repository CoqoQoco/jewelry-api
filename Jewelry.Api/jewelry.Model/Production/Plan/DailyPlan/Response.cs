using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.Plan.DailyPlan
{
    public class Response
    {
        public DateTime DataAtDate => DateTime.UtcNow;
        public List<ReortItem> Report { get; set; } = new List<ReortItem>();
        public List<RecentItem> RecentActivity { get; set; } = new List<RecentItem>(); // 10 items

        public int PlanCountProcess { get; set; }
        public int PlanCountCompletedOnYesterday { get; set; }
        public int PlanCountOverdue { get; set; }
        public int PlanCountTotal { get; set; }
        public DashboardSummary Summary { get; set; } = new DashboardSummary();
    }

    public class DashboardSummary
    {
        public int TotalActiveProjects { get; set; }
        public int CompletedToday { get; set; }
        public int OverduePlans { get; set; }
        public int PendingApproval { get; set; }
        public decimal PercentageCompleted { get; set; }
        public List<StatusTrend> StatusTrends { get; set; } = new List<StatusTrend>();
        public List<ProductTypeSummary> ProductTypeSummary { get; set; } = new List<ProductTypeSummary>();
        public List<CustomerTypeSummary> CustomerTypeSummary { get; set; } = new List<CustomerTypeSummary>();
    }

    public class StatusTrend
    {
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public string TrendDirection { get; set; } = "stable"; // up, down, stable
    }

    public class ProductTypeSummary
    {
        public string ProductType { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQty { get; set; }
        public decimal? TotalWeight { get; set; }
    }

    public class CustomerTypeSummary
    {
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerTypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQty { get; set; }
    }

    public class ReortItem
    {
        public int Status { get; set; }
        public string StatusNameTH { get; set; }
        public string StatusNameEN { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public int Count { get; set; }
    }

    public class RecentItem
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public DateTime RequestDate { get; set; }

        public string Mold { get; set; }
        public string MoldSub { get; set; }

        public string? ProductRunning { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string ProductTypeName { get; set; }
        public string ProductNumber { get; set; }
        public string ProductDetail { get; set; }
        public int ProductQty { get; set; }
        public string ProductQtyUnit { get; set; }

        public string CustomerNumber { get; set; }
        public string CustomerName { get; set; }

        public string? CustomerType { get; set; }
        public string? CustomerTypeName { get; set; }

        public bool? IsActive { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public string? Remark { get; set; }

        public string? Gold { get; set; }
        public string? GoldSize { get; set; }

    }
}