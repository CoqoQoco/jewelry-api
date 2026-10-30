using System;

namespace jewelry.Model.LabelPrint.Ack
{
    public class Response
    {
        public long Id { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? PrintedDate { get; set; }
    }
}
