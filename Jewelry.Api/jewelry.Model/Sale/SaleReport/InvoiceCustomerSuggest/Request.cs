using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleReport.InvoiceCustomerSuggest
{
    public class Request
    {
        public int Take { get; set; }
        public int Skip { get; set; }
        public SearchData? Search { get; set; }
    }

    public class SearchData
    {
        public string? Text { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public List<string>? SaleChannelCodes { get; set; }
    }
}
