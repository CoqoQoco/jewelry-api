using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlan
{
    public int Id { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    /// <summary>
    /// เลขใบจ่าย-รับงาน
    /// </summary>
    public string Wo { get; set; } = null!;

    /// <summary>
    /// ลำดับใบจ่าย-รับงาน
    /// </summary>
    public int WoNumber { get; set; }

    /// <summary>
    /// วันสร้างใบงาน
    /// </summary>
    public DateTime RequestDate { get; set; }

    /// <summary>
    /// เเม่พิมพ์
    /// </summary>
    public string Mold { get; set; } = null!;

    public string ProductNumber { get; set; } = null!;

    public string CustomerNumber { get; set; } = null!;

    public string? Remark { get; set; }

    public bool? IsActive { get; set; }

    public int Status { get; set; }

    public string ProductDetail { get; set; } = null!;

    public string CustomerType { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string ProductType { get; set; } = null!;

    public int ProductQty { get; set; }

    public string ProductQtyUnit { get; set; } = null!;

    public string? ProductRunning { get; set; }

    public string WoText { get; set; } = null!;

    public string? Type { get; set; }

    public string? TypeSize { get; set; }

    public bool IsReceipt { get; set; }

    public virtual TbmCustomerType CustomerTypeNavigation { get; set; } = null!;

    public virtual TbmProductType ProductTypeNavigation { get; set; } = null!;

    public virtual TbmProductionPlanStatus StatusNavigation { get; set; } = null!;

    public virtual ICollection<TbtProductionPlanImage> TbtProductionPlanImage { get; set; } = new List<TbtProductionPlanImage>();

    public virtual ICollection<TbtProductionPlanMaterial> TbtProductionPlanMaterial { get; set; } = new List<TbtProductionPlanMaterial>();

    public virtual ICollection<TbtProductionPlanPrice> TbtProductionPlanPrice { get; set; } = new List<TbtProductionPlanPrice>();

    public virtual ICollection<TbtProductionPlanStatusHeader> TbtProductionPlanStatusHeader { get; set; } = new List<TbtProductionPlanStatusHeader>();

    public virtual ICollection<TbtProductionPlanTransferStatus> TbtProductionPlanTransferStatus { get; set; } = new List<TbtProductionPlanTransferStatus>();

    public virtual ICollection<TbtStockProduct> TbtStockProduct { get; set; } = new List<TbtStockProduct>();
}
