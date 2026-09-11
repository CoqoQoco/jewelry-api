namespace jewelry.Model.Master.SaleChannel
{
    public class SaleChannelListRequest
    {
        public string? NameOrCode { get; set; }
        public string? Type { get; set; }
        public bool? IsActive { get; set; }
    }
}
