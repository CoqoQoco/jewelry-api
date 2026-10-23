using Jewelry.Service.Report.Executive;
using static Jewelry.Service.Report.Executive.ExecutiveFlowWorkerEvaluator;

namespace Jewelry.Service.Tests.Report.Executive;

public class ExecutiveFlowWorkerEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["CG9K"] = "รอจ่ายขัดชุบ 9K",
        ["BG18K"] = "รอจ่ายฝัง 18K",
        ["T01"] = "TEST TEST TEST",
        ["W01"] = "สมชาย",
        ["W02"] = "สมหญิง",
    };

    private static PlanHolderResult ClassifyOne(params string?[] workers)
    {
        var rows = workers.Select(w => (1, w)).ToList();
        return Classify(new[] { 1 }, rows, Names)[1];
    }

    [Fact]
    public void Classify_NoHolderRows_IsNone()
    {
        var r = ClassifyOne();
        Assert.Equal(HolderClass.None, r.Class);
        Assert.Empty(r.NamedWorkers);
    }

    [Fact]
    public void Classify_OnlyBlankOrNullWorkers_IsNone()
    {
        Assert.Equal(HolderClass.None, ClassifyOne(null, "", "  ").Class);
    }

    [Fact]
    public void Classify_OnlyPlaceholderQueueCodes_IsQueue()
    {
        var r = ClassifyOne("CG9K", "BG18K");
        Assert.Equal(HolderClass.Queue, r.Class);
        Assert.Empty(r.NamedWorkers);
    }

    [Fact]
    public void Classify_RealWorker_IsNamedAndListsOnlyRealWorkers()
    {
        var r = ClassifyOne("W01", "CG9K", "W02");
        Assert.Equal(HolderClass.Named, r.Class);
        Assert.Equal(new[] { "W01", "W02" }, r.NamedWorkers);
    }

    [Fact]
    public void Classify_TestCodeByLiteralName_IsTest()
    {
        Assert.Equal(HolderClass.Test, ClassifyOne("TEST").Class);
    }

    [Fact]
    public void Classify_TestCodeByWorkerName_IsTest()
    {
        Assert.Equal(HolderClass.Test, ClassifyOne("T01").Class);
    }

    [Fact]
    public void Classify_TestBeatsNamedAndQueue()
    {
        var r = ClassifyOne("W01", "CG9K", "T01");
        Assert.Equal(HolderClass.Test, r.Class);
        Assert.Empty(r.NamedWorkers);
    }

    [Fact]
    public void Classify_NamedBeatsQueue()
    {
        Assert.Equal(HolderClass.Named, ClassifyOne("CG9K", "W01").Class);
    }

    [Fact]
    public void Classify_CodesAreTrimmedAndDeduplicated()
    {
        Assert.Equal(new[] { "W01" }, ClassifyOne(" W01 ", "W01").NamedWorkers);
    }

    [Fact]
    public void Classify_UnknownCodeNotInNameMap_IsTreatedAsNamed()
    {
        Assert.Equal(HolderClass.Named, ClassifyOne("ZZZ").Class);
    }

    [Fact]
    public void Classify_PlaceholderDetectionRequiresNameStartingWithWo()
    {
        var names = new Dictionary<string, string> { ["X1"] = "ช่างรอ", ["X2"] = "รอคิว" };

        var result = Classify(new[] { 1, 2 }, new[] { (1, (string?)"X1"), (2, (string?)"X2") }, names);

        Assert.Equal(HolderClass.Named, result[1].Class);
        Assert.Equal(HolderClass.Queue, result[2].Class);
    }

    [Fact]
    public void Classify_ReturnsEntryForEveryRequestedPlanAndIgnoresUnrequestedRows()
    {
        var rows = new[] { (1, (string?)"W01"), (99, (string?)"W02") };

        var result = Classify(new[] { 1, 2 }, rows, Names);

        Assert.Equal(new[] { 1, 2 }, result.Keys.OrderBy(k => k));
        Assert.Equal(HolderClass.Named, result[1].Class);
        Assert.Equal(HolderClass.None, result[2].Class);
    }
}
