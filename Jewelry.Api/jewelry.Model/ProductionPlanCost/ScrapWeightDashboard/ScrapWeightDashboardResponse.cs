using System;
using System.Collections.Generic;

namespace jewelry.Model.ProductionPlanCost.ScrapWeightDashboard
{
    public class ScrapWeightDashboardResponse
    {
        public List<ScrapWeightMonthlyData> MeltScrapData { get; set; } = new List<ScrapWeightMonthlyData>();
        public List<ScrapWeightMonthlyData> CastScrapData { get; set; } = new List<ScrapWeightMonthlyData>();
    }

    public class ScrapWeightMonthlyData
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public List<ScrapWeightGoldCategory> GoldCategories { get; set; } = new List<ScrapWeightGoldCategory>();
        public decimal TotalWeight { get; set; }
    }

    public class ScrapWeightGoldCategory
    {
        public string GoldCode { get; set; }
        public string GoldName { get; set; }
        public string GoldSizeCode { get; set; }
        public string GoldSizeName { get; set; }
        public decimal Weight { get; set; }
        public DateTime? Date { get; set; }
    }
}