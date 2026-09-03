using Kendo.DynamicLinqCore;

namespace jewelry.Model.Stock.Gold.Balance
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public string[]? GoldCode { get; set; }
        public string[]? GoldSizeCode { get; set; }
    }
}
