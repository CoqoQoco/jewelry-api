using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockPiece
{
    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string SkuCode { get; set; } = null!;

    public string LocationCode { get; set; } = null!;

    public string? Status { get; set; }

    public string? ReceiptNumber { get; set; }

    public string? ReceiptType { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public DateTime? ProductionDate { get; set; }

    public string? Wo { get; set; }

    public int? WoNumber { get; set; }

    public string? WoOrigin { get; set; }

    public string? StockNumberOrigin { get; set; }

    public string? PoNumber { get; set; }

    public string? Vendor { get; set; }

    public decimal? ProductCost { get; set; }

    public string? ProductCostDetail { get; set; }

    public decimal? WeightActual { get; set; }

    public string? SizeActual { get; set; }

    public string? Barcode { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtSku SkuCodeNavigation { get; set; } = null!;

    public virtual TbmStockLocation LocationCodeNavigation { get; set; } = null!;

    public virtual ICollection<TbtStockMovement> TbtStockMovement { get; set; } = new List<TbtStockMovement>();

    public virtual ICollection<TbtStockPieceMaterial> TbtStockPieceMaterial { get; set; } = new List<TbtStockPieceMaterial>();

    public virtual ICollection<TbtStockPieceCostVersion> TbtStockPieceCostVersion { get; set; } = new List<TbtStockPieceCostVersion>();

    public virtual ICollection<TbtStockPieceCostPlan> TbtStockPieceCostPlan { get; set; } = new List<TbtStockPieceCostPlan>();
}
