using System;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class DashboardRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? GroupName { get; set; }
        public string? Shape { get; set; }
        public string? Grade { get; set; }
    }
}