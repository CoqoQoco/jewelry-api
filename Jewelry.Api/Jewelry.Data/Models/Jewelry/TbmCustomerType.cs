using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmCustomerType
{
    public int Id { get; set; }

    public string NameEn { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public virtual ICollection<TbtProductionPlan> TbtProductionPlanCustomerTypeNavigation { get; set; } = new List<TbtProductionPlan>();

    public virtual ICollection<TbtProductionPlan> TbtProductionPlanProductTypeNavigation { get; set; } = new List<TbtProductionPlan>();
}
