using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmGoldSize
{
    public int Id { get; set; }

    public string NameEn { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public virtual ICollection<TbtProductionPlanCostGold> TbtProductionPlanCostGold { get; set; } = new List<TbtProductionPlanCostGold>();

    public virtual ICollection<TbtProductionPlanMaterial> TbtProductionPlanMaterial { get; set; } = new List<TbtProductionPlanMaterial>();
}
