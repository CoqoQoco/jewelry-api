namespace jewelry.Model.LabelPrint.Ack
{
    public class Request
    {
        public long Id { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
