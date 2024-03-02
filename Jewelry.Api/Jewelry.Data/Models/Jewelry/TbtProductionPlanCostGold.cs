using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanCostGold
{
    public string No { get; set; } = null!;

    public string BookNo { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? MeltDate { get; set; }

    public decimal? MeltWeight { get; set; }

    public decimal? ReturnMeltWeight { get; set; }

    public decimal? ReturnMeltScrapWeight { get; set; }

    public decimal? MeltWeightLoss { get; set; }

    public decimal? MeltWeightOver { get; set; }

    public decimal? CastWeight { get; set; }

    public decimal? GemWeight { get; set; }

    public decimal? ReturnCastWeight { get; set; }

    public decimal? ReturnCastBodyWeightTotal { get; set; }

    public decimal? ReturnCastScrapWeight { get; set; }

    public decimal? ReturnCastPowderWeight { get; set; }

    public decimal? CastWeightLoss { get; set; }

    public decimal? CastWeightOver { get; set; }

    public DateTime AssignDate { get; set; }

    public string GoldReceipt { get; set; } = null!;

    public string Gold { get; set; } = null!;

    public string GoldSize { get; set; } = null!;

    public string? AssignBy { get; set; }

    public string? ReceiveBy { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CastDate { get; set; }

    public string? RunningNumber { get; set; }

    public decimal? ReturnCastMoldWeight { get; set; }

    public decimal? ReturnCastBodyBrokenedWeight { get; set; }

    public virtual TbmGold GoldNavigation { get; set; } = null!;

    public virtual TbmGoldSize GoldSizeNavigation { get; set; } = null!;

    public virtual ICollection<TbtProductionPlanCostGoldItem> TbtProductionPlanCostGoldItem { get; set; } = new List<TbtProductionPlanCostGoldItem>();
}
