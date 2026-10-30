using System;

namespace jewelry.Model.LabelPrint.List
{
    public class Response
    {
        public long Id { get; set; }
        public string LabelType { get; set; } = null!;
        public string ReferenceCode { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public int Copies { get; set; }
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public string? StationId { get; set; }
        public string? CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ClaimedDate { get; set; }
        public DateTime? PrintedDate { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
