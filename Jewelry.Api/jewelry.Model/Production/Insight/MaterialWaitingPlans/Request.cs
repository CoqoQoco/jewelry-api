using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.MaterialWaitingPlans
{
    public class Request : DataSourceRequest
    {
        // null/ว่าง = ทุก gemStatus — ค่า: 'ready' | 'short' | 'unmatched'
        public string? GemStatus { get; set; }
    }
}
