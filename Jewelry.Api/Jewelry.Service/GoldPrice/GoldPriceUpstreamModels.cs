using Newtonsoft.Json;
using System.Collections.Generic;

namespace Jewelry.Service.GoldPrice
{
    internal class UpstreamTodayResponse
    {
        [JsonProperty("data")]
        public List<UpstreamTodayItem> Data { get; set; } = new();
    }

    internal class UpstreamTodayItem
    {
        [JsonProperty("AsTime")]
        public string AsTime { get; set; } = null!;

        [JsonProperty("PriceSeq")]
        public int PriceSeq { get; set; }

        [JsonProperty("BL_BuyPrice")]
        public string BarBuy { get; set; } = null!;

        [JsonProperty("BL_SellPrice")]
        public string BarSell { get; set; } = null!;

        [JsonProperty("OM965_BuyPrice")]
        public string OrnamentBuy { get; set; } = null!;

        [JsonProperty("OM965_SellPrice")]
        public string OrnamentSell { get; set; } = null!;

        [JsonProperty("GoldSpot")]
        public string GoldSpot { get; set; } = null!;

        [JsonProperty("BahtPerUSD")]
        public string BahtPerUsd { get; set; } = null!;

        [JsonProperty("PriceDiff")]
        public string PriceDiff { get; set; } = null!;
    }

    internal class UpstreamHistoryResponse
    {
        [JsonProperty("data")]
        public List<UpstreamHistoryItem> Data { get; set; } = new();
    }

    internal class UpstreamHistoryItem
    {
        [JsonProperty("Date")]
        public string Date { get; set; } = null!;

        [JsonProperty("Open")]
        public string Open { get; set; } = null!;

        [JsonProperty("Highest")]
        public string Highest { get; set; } = null!;

        [JsonProperty("Lowest")]
        public string Lowest { get; set; } = null!;

        [JsonProperty("Close")]
        public string Close { get; set; } = null!;

        [JsonProperty("Change")]
        public string Change { get; set; } = null!;
    }
}
