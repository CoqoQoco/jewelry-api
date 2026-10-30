namespace jewelry.Model.LabelPrint.Enqueue
{
    public class Request
    {
        public string? LabelType { get; set; }
        public string ReferenceCode { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public int Copies { get; set; } = 1;
    }
}
