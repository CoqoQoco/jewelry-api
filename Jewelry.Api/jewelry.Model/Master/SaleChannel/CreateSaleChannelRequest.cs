using System;

namespace jewelry.Model.Master.SaleChannel
{
    public class CreateSaleChannelRequest
    {
        public string Code { get; set; } = null!;
        public string NameTh { get; set; } = null!;
        public string? NameEn { get; set; }
        public string Type { get; set; } = null!;
        public string? Venue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int? SortOrder { get; set; }
    }
}
