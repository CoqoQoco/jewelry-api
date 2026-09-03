namespace jewelry.Model.Print.Retry
{
    public class Response
    {
        public long Id { get; set; }
        public string Status { get; set; } = null!;
        public int RetryCount { get; set; }
    }
}
