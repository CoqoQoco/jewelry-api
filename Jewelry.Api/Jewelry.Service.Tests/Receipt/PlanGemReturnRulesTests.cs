using jewelry.Model.Exceptions;
using Jewelry.Service.Receipt.Gem;

namespace Jewelry.Service.Tests.Receipt;

public class PlanGemReturnRulesTests
{
    private static PlanGemReturnRules.Result Run(decimal rq, decimal rw, int status = 50, string reason = "DUPLICATE",
        string mode = "RETURN_NOW", bool active = true, string? outbound = "PIO1")
        => PlanGemReturnRules.Validate(status, active, outbound, reason, mode, 10, 2.5m, rq, rw);

    [Fact]
    public void Full_return_marks_full()
    {
        var r = Run(10, 2.5m);
        Assert.True(r.IsFull);
    }

    [Fact]
    public void Partial_return_keeps_remainder()
    {
        var r = Run(4, 1m);
        Assert.False(r.IsFull);
        Assert.Equal(6, r.RemainQty);
        Assert.Equal(1.5m, r.RemainWeight);
    }

    [Fact]
    public void Over_qty_rejected() => Assert.Throws<HandleException>(() => Run(11, 1m));

    [Fact]
    public void Over_weight_rejected() => Assert.Throws<HandleException>(() => Run(1, 3m));

    [Fact]
    public void Mixed_remainder_rejected()
    {
        Assert.Throws<HandleException>(() => Run(10, 1m));
        Assert.Throws<HandleException>(() => Run(5, 2.5m));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    public void Locked_status_rejected(int status) => Assert.Throws<HandleException>(() => Run(1, 0.1m, status));

    [Fact]
    public void Zero_both_and_bad_reason_and_inactive_rejected()
    {
        Assert.Throws<HandleException>(() => Run(0, 0));
        Assert.Throws<HandleException>(() => Run(1, 0.1m, reason: "X"));
        Assert.Throws<HandleException>(() => Run(1, 0.1m, active: false));
        Assert.Throws<HandleException>(() => Run(1, 0.1m, outbound: ""));
    }

    [Fact]
    public void Zero_weight_row_only_qty_decides()
    {
        var full = PlanGemReturnRules.Validate(50, true, "PIO1", "OVER", "RETURN_NOW", 10, 0, 10, 0);
        Assert.True(full.IsFull);
        var partial = PlanGemReturnRules.Validate(50, true, "PIO1", "OVER", "RETURN_NOW", 10, 0, 4, 0);
        Assert.False(partial.IsFull);
        Assert.Equal(6, partial.RemainQty);
    }

    [Fact]
    public void Zero_qty_row_only_weight_decides()
    {
        var full = PlanGemReturnRules.Validate(50, true, "PIO1", "OVER", "RETURN_NOW", 0, 2m, 0, 2m);
        Assert.True(full.IsFull);
        var partial = PlanGemReturnRules.Validate(50, true, "PIO1", "OVER", "RETURN_NOW", 0, 2m, 0, 1m);
        Assert.False(partial.IsFull);
        Assert.Throws<HandleException>(() => PlanGemReturnRules.Validate(50, true, "PIO1", "OVER", "RETURN_NOW", 10, 0, 1, 0.5m));
    }

    [Fact]
    public void ParsePlanRef_and_match_helpers()
    {
        Assert.Equal(("6911008", 13), PlanGemReturnRules.ParsePlanRef("6911008-13")!.Value);
        Assert.Equal(("6911008", 13), PlanGemReturnRules.ParsePlanRef("ตัดซ้ำ 6911008 / 13")!.Value);
        Assert.Null(PlanGemReturnRules.ParsePlanRef("PO-123"));
        Assert.Null(PlanGemReturnRules.ParsePlanRef(null));
        Assert.True(PlanGemReturnRules.MatchesPlan("6911008-13", "6911008", 13));
        Assert.False(PlanGemReturnRules.MatchesPlan("6911008-14", "6911008", 13));
    }

    [Fact]
    public void CheckLinkPlan_rejects_other_plan_allows_unparseable()
    {
        PlanGemReturnRules.CheckLinkPlan("6911008-13", "6911008", 13);
        PlanGemReturnRules.CheckLinkPlan("PO-123", "6911008", 13);
        PlanGemReturnRules.CheckLinkPlan(null, "6911008", 13);
        var ex = Assert.Throws<HandleException>(() => PlanGemReturnRules.CheckLinkPlan("6911008-14", "6911008", 13));
        Assert.Contains("6911008-14", ex.Message);
    }

    [Fact]
    public void Link_remaining_enough_passes() => PlanGemReturnRules.CheckLinkRemaining(10, 5, 4, 2, 6, 3);

    [Fact]
    public void Link_remaining_insufficient_rejected()
    {
        Assert.Throws<HandleException>(() => PlanGemReturnRules.CheckLinkRemaining(10, 5, 4, 2, 7, 1));
        Assert.Throws<HandleException>(() => PlanGemReturnRules.CheckLinkRemaining(10, 5, 4, 2, 1, 4));
    }
}
