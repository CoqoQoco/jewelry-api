using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockGoldTransection
{
    public long Id { get; set; }

    public string Running { get; set; } = null!;

    public string GoldCode { get; set; } = null!;

    public string GoldSizeCode { get; set; } = null!;

    public int Type { get; set; }

    public decimal Weight { get; set; }

    public decimal? PreviousRemainWeight { get; set; }

    public decimal? PointRemainWeight { get; set; }

    public string? RefDocType { get; set; }

    public string? RefDocNo { get; set; }

    public string? ProductionPlanWo { get; set; }

    public int? ProductionPlanWoNumber { get; set; }

    public string? RefRunning { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string? Status { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbmGold GoldCodeNavigation { get; set; } = null!;

    public virtual TbmGoldSize GoldSizeCodeNavigation { get; set; } = null!;
}
