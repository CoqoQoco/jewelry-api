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

    /// <summary>
    /// ยอดสั่ง
    /// </summary>
    public int Qty { get; set; }

    /// <summary>
    /// ยอดสำเร็จรูป
    /// </summary>
    public int? QtyFinish { get; set; }

    /// <summary>
    /// ยอดกึ่งสำเร็จรูป
    /// </summary>
    public int? QtySemiFinish { get; set; }

    /// <summary>
    /// ยอดหล่อ
    /// </summary>
    public int? QtyCast { get; set; }

    /// <summary>
    /// หน่วย
    /// </summary>
    public string QtyUnit { get; set; } = null!;

    public string? Remark { get; set; }

    public bool? IsActive { get; set; }

    public int Status { get; set; }

    public virtual ICollection<TbtProductionPlanImage> TbtProductionPlanImage { get; set; } = new List<TbtProductionPlanImage>();

    public virtual ICollection<TbtProductionPlanMaterial> TbtProductionPlanMaterial { get; set; } = new List<TbtProductionPlanMaterial>();
}
