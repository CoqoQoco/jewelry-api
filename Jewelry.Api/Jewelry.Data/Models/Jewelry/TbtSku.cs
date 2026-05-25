using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSku
{
    public string SkuCode { get; set; } = null!;

    public string? ProductNumber { get; set; }

    public string ProductNameTh { get; set; } = null!;

    public string ProductNameEn { get; set; } = null!;

    public string? ProductType { get; set; }

    public string? ProductTypeName { get; set; }

    public string? Mold { get; set; }

    public string? MoldDesign { get; set; }

    public string? StudEarring { get; set; }

    public string? Size { get; set; }

    public string? ProductionType { get; set; }

    public string? ProductionTypeSize { get; set; }

    public string? ImageName { get; set; }

    public string? ImagePath { get; set; }

    public decimal? DefaultPrice { get; set; }

    public decimal? TagPriceMultiplier { get; set; }

    public bool IsActive { get; set; }

    public bool IsSerialized { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtStockPiece> TbtStockPiece { get; set; } = new List<TbtStockPiece>();

    public virtual ICollection<TbtStockBalance> TbtStockBalance { get; set; } = new List<TbtStockBalance>();

    public virtual ICollection<TbtStockMovement> TbtStockMovement { get; set; } = new List<TbtStockMovement>();
}
