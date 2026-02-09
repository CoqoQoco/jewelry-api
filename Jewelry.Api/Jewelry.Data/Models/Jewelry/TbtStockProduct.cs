using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockProduct
{
    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public string? Location { get; set; }

    /// <summary>
    /// เลขใบจ่าย-รับงาน
    /// </summary>
    public string? Wo { get; set; }

    /// <summary>
    /// ลำดับใบจ่าย-รับงาน
    /// </summary>
    public int? WoNumber { get; set; }

    public string? Status { get; set; }

    public string ProductNameEn { get; set; } = null!;

    public string ProductNameTh { get; set; } = null!;

    public decimal Qty { get; set; }

    public decimal ProductPrice { get; set; }

    public DateTime ReceiptDate { get; set; }

    public string StockNumber { get; set; } = null!;

    /// <summary>
    /// รหัสประเภทสินค้า
    /// </summary>
    public string ProductType { get; set; } = null!;

    public string? Size { get; set; }

    public string ReceiptType { get; set; } = null!;

    public string? Remark { get; set; }

    public string? ImageName { get; set; }

    public string? ImagePath { get; set; }

    public string? Mold { get; set; }

    public DateTime ProductionDate { get; set; }

    /// <summary>
    /// ประเภทสินค้า
    /// </summary>
    public string? ProductTypeName { get; set; }

    /// <summary>
    /// Gold/Silver
    /// </summary>
    public string? ProductionType { get; set; }

    /// <summary>
    /// 10 K, 18K ....
    /// </summary>
    public string? ProductionTypeSize { get; set; }

    public string ProductNumber { get; set; } = null!;

    public string? PoNumber { get; set; }

    public string? MoldDesign { get; set; }

    /// <summary>
    /// แป้นต่างหู
    /// </summary>
    public string? StudEarring { get; set; }

    public decimal? ProductCost { get; set; }

    public string? ProductCode { get; set; }

    public string? WoOrigin { get; set; }

    public decimal QtySale { get; set; }

    public decimal? QtyRemaining { get; set; }

    /// <summary>
    /// ต้นทุนสินค้า
    /// </summary>
    public string? ProductCostDetail { get; set; }

    public virtual ICollection<TbtStockCostVersion> TbtStockCostVersion { get; set; } = new List<TbtStockCostVersion>();

    public virtual ICollection<TbtStockProductMaterial> TbtStockProductMaterial { get; set; } = new List<TbtStockProductMaterial>();
}
