using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockPieceCostVersion
{
    public string Running { get; set; } = null!;

    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string? CustomerName { get; set; }

    public string? CustomerCode { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerTel { get; set; }

    public string? CustomerEmail { get; set; }

    public string? Remark { get; set; }

    public string ProductCostDetail { get; set; } = null!;

    public string? JobRunning { get; set; }

    public decimal? TagPriceMultiplier { get; set; }

    public string? CurrencyUnit { get; set; }

    public decimal? CurrencyRate { get; set; }

    public string? CustomStockInfo { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtStockPiece StockPieceNavigation { get; set; } = null!;

    public virtual ICollection<TbtStockPieceCostPlan> TbtStockPieceCostPlan { get; set; } = new List<TbtStockPieceCostPlan>();
}
