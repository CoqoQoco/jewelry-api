using System;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class DashboardRequest
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? GroupName { get; set; }
        public string? Shape { get; set; }
        public string? Grade { get; set; }
    }
}