namespace jewelry.Model.ProductionPlan.GoldLossUpdate;

public class GoldLossUpdateRequest
{
    public int HeaderId { get; set; }
    public decimal? GoldLossPrice { get; set; }
    public List<GoldLossItemRequest> Items { get; set; } = new();
}

public class GoldLossItemRequest
{
    public string ItemNo { get; set; } = null!;
    public decimal? LossPercent { get; set; }
    public string? LossRemark { get; set; }
}
