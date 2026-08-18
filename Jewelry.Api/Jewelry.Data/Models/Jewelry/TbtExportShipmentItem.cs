using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtExportShipmentItem
{
    public long Id { get; set; }

    public string ShipmentRunning { get; set; } = null!;

    public int ItemNo { get; set; }

    public int SortOrder { get; set; }

    public string StockNumber { get; set; } = null!;

    public string? ProductCode { get; set; }

    public string? ProductNumber { get; set; }

    public string? Description { get; set; }

    public decimal? GoldWeight { get; set; }

    public decimal? StoneWeight { get; set; }

    public decimal? DiamondWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? Qty { get; set; }

    public decimal? TagPrice { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? Amount { get; set; }

    public string? ImagePath { get; set; }

    public int? ParcelNo { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual TbtExportShipment ShipmentRunningNavigation { get; set; } = null!;
}
