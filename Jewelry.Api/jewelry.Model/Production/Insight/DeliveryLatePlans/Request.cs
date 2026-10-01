using System;
using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.DeliveryLatePlans
{
    public class Request : DataSourceRequest
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
