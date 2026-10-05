using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.MaterialGemLowCover
{
    public class Request : DataSourceRequest
    {
        public int CoverDays { get; set; } = 30;
    }
}
