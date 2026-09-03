using System;

namespace jewelry.Model.Print.List
{
    public class Response
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public string? StationId { get; set; }
        public string? CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ClaimedDate { get; set; }
        public DateTime? PrintedDate { get; set; }
    }
}
