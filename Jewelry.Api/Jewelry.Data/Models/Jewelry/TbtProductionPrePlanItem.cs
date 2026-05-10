using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPrePlanItem
{
    public int Id { get; set; }

    public int PrePlanId { get; set; }

    public int ItemNo { get; set; }

    public string MoldCode { get; set; } = null!;

    public string? MoldDetail { get; set; }

    public string? ProductType { get; set; }

    public int? ProductQty { get; set; }

    public string? ProductQtyUnit { get; set; }

    public string? ProductDetail { get; set; }

    public string? ProductImagePath { get; set; }

    public int? LinkedProductionPlanId { get; set; }

    public string? Wo { get; set; }

    public int? WoNumber { get; set; }

    public string? WoText { get; set; }

    public string? CreateBy { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual TbtProductionPrePlan PrePlan { get; set; } = null!;

    public virtual ICollection<TbtProductionPrePlanMaterial> Materials { get; set; } = new List<TbtProductionPrePlanMaterial>();
}
