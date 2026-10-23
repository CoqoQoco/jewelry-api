using Jewelry.Service.Report.Executive;
using static Jewelry.Service.Report.Executive.ExecutiveFlowLossEvaluator;

namespace Jewelry.Service.Tests.Report.Executive;

public class ExecutiveFlowLossEvaluatorTests
{
    private static LossCell Cell(string worker, decimal send, decimal check, string dept = "setting", string metal = "YG", int rows = 1)
        => new LossCell { Dept = dept, Metal = metal, Worker = worker, Rows = rows, Send = send, Check = check };

    private static List<DeptMetalResult> Run(IEnumerable<LossCell> cells, decimal? target = 1.0m)
    {
        return Evaluate(
            cells,
            new[] { "setting" },
            new[] { "YG" },
            (d, m) => target.HasValue ? (target.Value, false) : null,
            w => string.IsNullOrWhiteSpace(w) || w.StartsWith("รอ") || w.Contains("TEST"),
            code => "N-" + code);
    }

    [Fact]
    public void RawLossPercent_SendZero_ReturnsNull()
    {
        Assert.Null(RawLossPercent(0, 0));
    }

    [Fact]
    public void RawLossPercent_ComputesUnrounded()
    {
        Assert.Equal(2m, RawLossPercent(100m, 98m));
        Assert.Equal(1m / 3m * 100m, RawLossPercent(3m, 2m));
    }

    [Theory]
    [InlineData(1000, 980, 1.0, 10)]
    [InlineData(1000, 990, 1.0, 0)]
    [InlineData(1000, 995, 1.0, 0)]
    [InlineData(250, 245, 0.0, 5)]
    public void ExcessG_IsMaxZeroOfLossOverAllowed(decimal send, decimal check, decimal target, decimal expected)
    {
        Assert.Equal(expected, ExcessG(send, check, target));
    }

    [Fact]
    public void ExcessG_NoTargetOrNoSend_ReturnsZero()
    {
        Assert.Equal(0m, ExcessG(1000, 900, null));
        Assert.Equal(0m, ExcessG(0, 0, 1m));
    }

    [Fact]
    public void BuildRow_RoundsDisplayValuesButKeepsRawDelta()
    {
        var row = BuildRow("TOTAL", null, true, 3, 300m, 296.9m, 1.0m);

        Assert.Equal(3.1m, row.LossG);
        Assert.Equal(1.03m, row.LossPercent);
        Assert.Equal(0.03m, row.DeltaPp);
        Assert.Equal(3.1m / 300m * 100m - 1.0m, row.RawDeltaPp);
        Assert.Equal(0.1m, row.ExcessG);
    }

    [Fact]
    public void BuildRow_NoTarget_DeltaIsNull()
    {
        var row = BuildRow("w", "c", false, 1, 100m, 99m, null);

        Assert.Null(row.DeltaPp);
        Assert.Null(row.RawDeltaPp);
        Assert.Equal(0m, row.ExcessG);
    }

    [Fact]
    public void Evaluate_TotalRowIsFirstAndSumsAllCellsIncludingExcludedWorkers()
    {
        var cells = new[]
        {
            Cell("A01", 300m, 290m),
            Cell("รอจ่ายฝัง", 100m, 100m),
            Cell("TEST", 50m, 40m),
        };

        var result = Run(cells).Single();

        Assert.True(result.Rows[0].IsTotal);
        Assert.Equal("TOTAL", result.Rows[0].Name);
        Assert.Equal(450m, result.Total.SendG);
        Assert.Equal(20m, result.Total.LossG);
        Assert.Equal(3, result.Total.Rows);
    }

    [Fact]
    public void Evaluate_ExcessIsComputedOnDepartmentTotalNotSumOfWorkers()
    {
        var cells = new[] { Cell("A", 100m, 95m), Cell("B", 900m, 900m) };

        var result = Run(cells, target: 1.0m).Single();

        Assert.Equal(0m, result.Total.ExcessG);
    }

    [Fact]
    public void Evaluate_WorkerBelow250gSend_IsFilteredOut()
    {
        var cells = new[] { Cell("A", 249.99m, 200m), Cell("B", 250m, 200m) };

        var result = Run(cells).Single();

        var named = result.Rows.Where(r => !r.IsTotal).ToList();
        Assert.Single(named);
        Assert.Equal("B", named[0].Code);
        Assert.Equal("N-B", named[0].Name);
    }

    [Fact]
    public void Evaluate_QueueTestAndEmptyWorkers_AreNotListedAsWorkers()
    {
        var cells = new[]
        {
            Cell("รอจ่ายขัดชุบ 9K", 500m, 400m),
            Cell("TEST", 500m, 400m),
            Cell("", 500m, 400m),
            Cell("REAL", 500m, 400m),
        };

        var result = Run(cells).Single();

        Assert.Equal(new[] { "REAL" }, result.Rows.Where(r => !r.IsTotal).Select(r => r.Code));
    }

    [Fact]
    public void Evaluate_WorkerCodesAreTrimmedAndGroupedTogether()
    {
        var cells = new[] { Cell("A", 150m, 140m), Cell(" A ", 150m, 140m) };

        var result = Run(cells).Single();

        var worker = Assert.Single(result.Rows.Where(r => !r.IsTotal));
        Assert.Equal(300m, worker.SendG);
        Assert.Equal(2, worker.Rows);
    }

    [Fact]
    public void Evaluate_Workers_AreOrderedByExcessThenSend()
    {
        var cells = new[]
        {
            Cell("LOW", 1000m, 995m),
            Cell("HIGH", 300m, 280m),
            Cell("MID", 400m, 390m),
            Cell("ZERO2", 500m, 500m),
        };

        var result = Run(cells).Single();

        Assert.Equal(new[] { "HIGH", "MID", "LOW", "ZERO2" }, result.Rows.Skip(1).Select(r => r.Code));
    }

    [Fact]
    public void Evaluate_NoTargetForDepartment_ProducesNullTargetAndZeroExcess()
    {
        var result = Run(new[] { Cell("A", 300m, 250m) }, target: null).Single();

        Assert.Null(result.TargetPercent);
        Assert.False(result.IsSystemDefault);
        Assert.Equal(0m, result.Total.ExcessG);
        Assert.Null(result.Total.DeltaPp);
    }

    [Fact]
    public void Evaluate_ReturnsOneResultPerDeptAndMetalEvenWhenEmpty()
    {
        var results = Evaluate(
            Array.Empty<LossCell>(),
            new[] { "setting", "plating" },
            new[] { "YG", "WG" },
            (d, m) => (1m, true),
            w => false,
            c => c);

        Assert.Equal(4, results.Count);
        Assert.All(results, r =>
        {
            Assert.True(r.IsSystemDefault);
            Assert.Equal(0m, r.Total.SendG);
            Assert.Null(r.Total.LossPercent);
        });
    }

    [Fact]
    public void TotalExcessG_SumsDepartmentTotals()
    {
        var a = Run(new[] { Cell("A", 1000m, 980m) }).Single();
        var b = Run(new[] { Cell("B", 500m, 490m) }).Single();

        Assert.Equal(15m, TotalExcessG(new[] { a, b }));
    }

    [Fact]
    public void CountWorkersOverTarget_RequiresDeltaAtLeastOnePp()
    {
        var cells = new[]
        {
            Cell("EXACT", 1000m, 980m),
            Cell("JUST_UNDER", 1000m, 980.1m),
            Cell("OVER", 1000m, 950m),
        };

        var results = Run(cells, target: 1.0m);

        Assert.Equal(2, CountWorkersOverTarget(results));
    }

    [Fact]
    public void CountWorkersOverTarget_IgnoresTotalRowAndNoTargetDepartments()
    {
        var withTarget = Run(new[] { Cell("A", 100m, 50m) }, target: 1.0m);
        var noTarget = Run(new[] { Cell("B", 500m, 400m) }, target: null);

        Assert.Equal(0, CountWorkersOverTarget(withTarget));
        Assert.Equal(0, CountWorkersOverTarget(noTarget));
    }
}
