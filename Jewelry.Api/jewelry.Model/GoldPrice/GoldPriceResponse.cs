using System.Collections.Generic;

namespace jewelry.Model.GoldPrice;

public class TodayGoldPriceResponse
{
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public int PriceSeq { get; set; }
    public decimal BarBuy { get; set; }
    public decimal BarSell { get; set; }
    public decimal OrnamentBuy { get; set; }
    public decimal OrnamentSell { get; set; }
    public decimal GoldSpot { get; set; }
    public decimal BahtPerUsd { get; set; }
    public decimal? ChangeFromPrevClose { get; set; }
    public List<GoldPriceRound> Rounds { get; set; } = new();
    public bool IsStale { get; set; }
}

public class GoldPriceRound
{
    public string Time { get; set; } = string.Empty;
    public int PriceSeq { get; set; }
    public decimal BarBuy { get; set; }
    public decimal BarSell { get; set; }
    public decimal OrnamentBuy { get; set; }
    public decimal OrnamentSell { get; set; }
    public decimal GoldSpot { get; set; }
    public decimal BahtPerUsd { get; set; }
    public decimal PriceDiff { get; set; }
}

public class HistoryGoldPriceResponse
{
    public string Series { get; set; } = "bar-sell";
    public List<GoldPriceHistoryItem> Items { get; set; } = new();
    public bool IsStale { get; set; }
}

public class GoldPriceHistoryItem
{
    public string Date { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Change { get; set; }
}
