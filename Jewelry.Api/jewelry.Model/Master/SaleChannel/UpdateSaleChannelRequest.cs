using System;

namespace jewelry.Model.Master.SaleChannel
{
    public class UpdateSaleChannelRequest
    {
        public string Code { get; set; } = null!;
        public string? NameTh { get; set; }
        public string? NameEn { get; set; }
        public string? Type { get; set; }
        public string? Venue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsActive { get; set; }
        public int? SortOrder { get; set; }
    }
}
