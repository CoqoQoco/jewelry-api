using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmZill
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string GoldCode { get; set; } = null!;

    public string GoldSizeCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public string? Remark { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? NameTh { get; set; }

    public string? NameEn { get; set; }

    public virtual TbmGold GoldCodeNavigation { get; set; } = null!;

    public virtual TbmGoldSize GoldSizeCodeNavigation { get; set; } = null!;
}
