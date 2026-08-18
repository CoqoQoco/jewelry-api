using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtExportShipment
{
    public string Running { get; set; } = null!;

    public string DocumentNumber { get; set; } = null!;

    public string? CustomNumber { get; set; }

    public DateTime DocumentDate { get; set; }

    public string? ConsigneeName { get; set; }

    public string? ConsigneeAddress { get; set; }

    public string? EventName { get; set; }

    public string? BoothNo { get; set; }

    public string? AttnName { get; set; }

    public string? AttnPassport { get; set; }

    public string? AttnTel { get; set; }

    public string? Incoterm { get; set; }

    public string? OriginCountry { get; set; }

    public string? Currency { get; set; }

    public decimal? ExchangeRate { get; set; }

    public decimal? PricePercent { get; set; }

    public int? ParcelCount { get; set; }

    public string? Remark { get; set; }

    public int Status { get; set; }

    public string? StatusName { get; set; }

    public bool IsActive { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual ICollection<TbtExportShipmentItem> TbtExportShipmentItem { get; set; } = new List<TbtExportShipmentItem>();
}
