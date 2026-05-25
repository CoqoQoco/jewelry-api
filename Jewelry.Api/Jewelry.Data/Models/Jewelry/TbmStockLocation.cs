using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmStockLocation
{
    public string Code { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string? NameEn { get; set; }

    public string? Type { get; set; }

    public string? ParentCode { get; set; }

    public bool IsSalesPoint { get; set; }

    public bool IsActive { get; set; }

    public int? SortOrder { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbmStockLocation? Parent { get; set; }

    public virtual ICollection<TbmStockLocation> Children { get; set; } = new List<TbmStockLocation>();

    public virtual ICollection<TbtStockPiece> TbtStockPiece { get; set; } = new List<TbtStockPiece>();

    public virtual ICollection<TbtStockBalance> TbtStockBalance { get; set; } = new List<TbtStockBalance>();
}
