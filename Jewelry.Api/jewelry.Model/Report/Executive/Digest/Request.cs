namespace jewelry.Model.Report.Executive.Digest
{
    public class Request
    {
        // 'ALL' (default) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";
    }
}
