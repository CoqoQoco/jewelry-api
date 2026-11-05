using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtLabelStationKey
{
    public long Id { get; set; }

    public string DeviceKey { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string? CreateBy { get; set; }

    public DateTime? RevokedDate { get; set; }

    public string? RevokedBy { get; set; }
}
