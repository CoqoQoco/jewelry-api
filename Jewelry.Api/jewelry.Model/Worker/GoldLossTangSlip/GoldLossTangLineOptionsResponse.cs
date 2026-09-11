using System;
using System.Collections.Generic;
namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class GoldLossTangLineOptionsResponse
    {
        public List<string> Issued { get; set; } = new List<string>();
        public List<string> Returned { get; set; } = new List<string>();
        public List<LastPriceByGoldSize> LastPrices { get; set; } = new List<LastPriceByGoldSize>();
    }

    public class LastPriceByGoldSize
    {
        public string GoldSize { get; set; }
        public decimal? PricePerGram { get; set; }
        public DateTime? FromDate { get; set; }
        public string? FromDocumentNo { get; set; }
    }
}
