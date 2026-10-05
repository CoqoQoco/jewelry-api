using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.MaterialGemDemand
{
    public class Request : DataSourceRequest
    {
        // null/ว่าง = ทุก status — ค่า: 'ready' | 'short' | 'unmatched'
        public string? Status { get; set; }
    }
}
