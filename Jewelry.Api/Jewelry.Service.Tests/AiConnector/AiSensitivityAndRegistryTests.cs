using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.AiConnector.Sensitivity;
using Jewelry.Service.AiConnector.Tools.Executive;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Jewelry.Service.Tests.AiConnector;

public class AiSensitivityAndRegistryTests
{
    private static AiExecutiveSummaryOutput Sample() => new()
    {
        Receivables = new AiReceivablesSummary { UnpaidCount = 3, UnpaidThb = 1000m, OverdueThb = 500m },
        Stock = new AiStockSummary { InStockCount = 10, CostThb = 9999m },
    };

    [Fact]
    public void Mask_DeniedCategories_NullOutNumbers_OnCopyOnly()
    {
        var source = Sample();
        var result = AiMasker.Mask(source, _ => false);

        Assert.Null(result.Value.Receivables.UnpaidThb);
        Assert.Null(result.Value.Stock.CostThb);
        Assert.Equal(3, result.Value.Receivables.UnpaidCount);
        Assert.Equal(10, result.Value.Stock.InStockCount);
        Assert.Equal(3, result.MaskedCount);
        Assert.NotEmpty(result.Notes);

        // ต้นฉบับ (เช่น object ใน InsightCache) ต้องไม่ถูกแก้
        Assert.Equal(1000m, source.Receivables.UnpaidThb);
        Assert.Equal(9999m, source.Stock.CostThb);
        Assert.NotSame(source, result.Value);
    }

    [Fact]
    public void Mask_AllowedCategory_KeepsValue_OthersMasked()
    {
        var result = AiMasker.Mask(Sample(), c => c == SensitiveCategory.Receivable);
        Assert.Equal(1000m, result.Value.Receivables.UnpaidThb);
        Assert.Null(result.Value.Stock.CostThb);
    }

    [Fact]
    public void Mask_AllAllowed_NothingMasked()
    {
        var result = AiMasker.Mask(Sample(), _ => true);
        Assert.Equal(0, result.MaskedCount);
        Assert.Equal(9999m, result.Value.Stock.CostThb);
    }

    private class NamedRow
    {
        [AiSensitive(SensitiveCategory.CustomerName)] public string Name { get; set; } = "";
    }

    private class NamedDto
    {
        [AiNotSensitive] public List<NamedRow> Rows { get; set; } = new();
        [AiSensitive(SensitiveCategory.WorkerName)] public List<AiMetric> Hidden { get; set; } = new();
    }

    [Fact]
    public void Mask_Names_BecomeStableAliases_PerRequest()
    {
        var src = new NamedDto
        {
            Rows = { new NamedRow { Name = "บริษัท ก" }, new NamedRow { Name = "บริษัท ข" }, new NamedRow { Name = "บริษัท ก" } },
            Hidden = { new AiMetric { Name = "n", Value = 1 } },
        };
        var r = AiMasker.Mask(src, _ => false);
        Assert.Equal("ลูกค้า #1", r.Value.Rows[0].Name);
        Assert.Equal("ลูกค้า #2", r.Value.Rows[1].Name);
        Assert.Equal("ลูกค้า #1", r.Value.Rows[2].Name);
        Assert.Empty(r.Value.Hidden);
        Assert.Equal("บริษัท ก", src.Rows[0].Name);
        Assert.Single(src.Hidden);
    }

    [Fact]
    public void Policy_FinanceSplitIntoCostAndReceivables()
    {
        var cost = SensitiveCategoryPolicy.For(SensitiveCategory.Cost);
        Assert.Equal(AiScopes.DataCost, cost.Scope);
        Assert.Equal(PermissionCodes.SaleViewMargin, cost.Permission);
        var recv = SensitiveCategoryPolicy.For(SensitiveCategory.Receivable);
        Assert.Equal(AiScopes.DataReceivables, recv.Scope);
        Assert.Equal(PermissionCodes.ExecutiveView, recv.Permission);
        Assert.DoesNotContain(SensitiveCategoryPolicy.Rules, r => r.Scope == "data:finance");

        var scopes = new HashSet<string> { AiScopes.DataCost };
        Assert.False(SensitiveCategoryPolicy.IsAllowed(SensitiveCategory.Cost, scopes, new HashSet<string>()));
        Assert.True(SensitiveCategoryPolicy.IsAllowed(SensitiveCategory.Cost, scopes, new HashSet<string> { PermissionCodes.SaleViewMargin }));
        Assert.False(SensitiveCategoryPolicy.IsAllowed(SensitiveCategory.Cost, new HashSet<string>(), new HashSet<string> { PermissionCodes.SaleViewMargin }));
    }

    // ---- reflection: AI DTO ----

    private static IEnumerable<Type> AiDtoRoots() =>
        typeof(AiExecutiveSummaryOutput).Assembly.GetTypes()
            .Where(t => t.Namespace == "jewelry.Model.AiConnector" && t.IsClass && t.Name.EndsWith("Output"));

    [Fact]
    public void AiDtos_EveryPropertyLabelled_NoObjectOrDictionary()
    {
        var roots = AiDtoRoots().ToList();
        Assert.NotEmpty(roots);
        foreach (var root in roots)
        {
            var errors = AiDtoValidator.Validate(root);
            Assert.True(errors.Count == 0, root.Name + ": " + string.Join("; ", errors));
        }
    }

    private class BadDto
    {
        public int Unlabelled { get; set; }
        [AiNotSensitive] public Dictionary<string, object> Params { get; set; } = new();
        [AiNotSensitive] public object Any { get; set; } = new();
        [AiSensitive(SensitiveCategory.Cost)] public decimal NotNullable { get; set; }
    }

    [Fact]
    public void AiDtoValidator_CatchesViolations()
    {
        var errors = AiDtoValidator.Validate(typeof(BadDto));
        Assert.Contains(errors, e => e.Contains("Unlabelled"));
        Assert.Contains(errors, e => e.Contains("Params"));
        Assert.Contains(errors, e => e.Contains("Any"));
        Assert.Contains(errors, e => e.Contains("NotNullable"));
    }

    // ---- registry ----

    private class FakeTool : AiToolBase<AiMetalInput, AiGlossaryOutput>
    {
        public string N = "fake";
        public bool Write;
        public string[] Perms = { PermissionCodes.ExecutiveView };
        public override string Name => N;
        public override string TitleTh => "ทดสอบ";
        public override string Description => "d";
        public override IReadOnlyList<string> RequiredScopes => new[] { AiScopes.ModuleExecutive };
        public override IReadOnlyList<string> PermissionsAny => Perms;
        public override string WrapsEndpoint => "X.Y";
        public override bool IsWrite => Write;
        protected override Task<AiGlossaryOutput> RunAsync(AiMetalInput input, AiToolContext context, CancellationToken ct) => Task.FromResult(new AiGlossaryOutput());
    }

    [Fact]
    public void Registry_RejectsWriteTool() =>
        Assert.Throws<InvalidOperationException>(() => new AiToolRegistry(new IAiTool[] { new FakeTool { Write = true } }));

    [Fact]
    public void Registry_RejectsDuplicateNames() =>
        Assert.Throws<InvalidOperationException>(() => new AiToolRegistry(new IAiTool[] { new FakeTool(), new FakeTool() }));

    [Fact]
    public void Registry_RejectsToolWithoutPermission() =>
        Assert.Throws<InvalidOperationException>(() => new AiToolRegistry(new IAiTool[] { new FakeTool { Perms = Array.Empty<string>() } }));

    [Fact]
    public void Registry_AcceptsValidTool() =>
        Assert.NotNull(new AiToolRegistry(new IAiTool[] { new FakeTool() }).Find("fake"));

    [Fact]
    public void RealTools_PassRegistry_AndAreReadOnlyWithThaiTitle()
    {
        var tools = typeof(AiToolBase<,>).Assembly.GetTypes()
            .Where(t => typeof(IAiTool).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => (IAiTool)Activator.CreateInstance(t)!)
            .ToList();
        Assert.Equal(4, tools.Count);
        var registry = new AiToolRegistry(tools);
        Assert.All(registry.Tools, t =>
        {
            Assert.False(t.IsWrite);
            Assert.Matches("[฀-๿]", t.TitleTh);
            Assert.Contains("get_glossary", t.Name == "get_glossary" ? "get_glossary" : t.Description);
        });
    }

    // ---- สิทธิ์ tool ต้องตรงกับ endpoint ที่ห่อ ----

    private static string? FindControllerSource(string controller)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var hit = Directory.EnumerateFiles(dir.FullName, controller + ".cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains(Path.Combine("Jewelry.Api", "Controllers")) && !f.Contains("/bin/") && !f.Contains("/obj/"));
            if (hit != null) return hit;
            dir = dir.Parent;
        }
        return null;
    }

    [Fact]
    public void ToolPermissions_MatchWrappedControllerEndpoints()
    {
        var tools = typeof(AiToolBase<,>).Assembly.GetTypes()
            .Where(t => typeof(IAiTool).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => (IAiTool)Activator.CreateInstance(t)!)
            .Where(t => t.WrapsEndpoint.Contains("Controller."))
            .ToList();
        Assert.NotEmpty(tools);

        foreach (var tool in tools)
        {
            var parts = tool.WrapsEndpoint.Split('.');
            var source = FindControllerSource(parts[0]);
            Assert.True(source != null, "ไม่พบไฟล์ " + parts[0]);
            var text = File.ReadAllText(source!);

            var m = Regex.Match(text, @"\[RequirePermission\(([^)]*)\)\][^{]*?public async Task<IActionResult> " + parts[1] + @"\(", RegexOptions.Singleline);
            Assert.True(m.Success, $"ไม่พบ [RequirePermission] ของ {tool.WrapsEndpoint}");
            var endpointPerms = Regex.Matches(m.Groups[1].Value, "\"([^\"]+)\"").Select(x => x.Groups[1].Value).OrderBy(x => x).ToArray();
            Assert.Equal(endpointPerms, tool.PermissionsAny.OrderBy(x => x).ToArray());
        }
    }

    // ---- digest: Params ซ่อนชื่อช่างต้องไม่หลุด ----

    [Fact]
    public void Digest_ParamsStringsDropped_MoneySplitOut()
    {
        var item = ExecutiveDigestTool.MapItem(new jewelry.Model.Report.Executive.Digest.TopItem
        {
            Domain = "sales",
            Severity = "critical",
            Tab = "sales",
            Code = "SAL_OVERDUE_RECEIVABLE",
            Params = new Dictionary<string, object> { ["worker"] = "ช่างสมชาย", ["count"] = 3, ["overdueThb"] = 12000.5m },
        });
        Assert.Equal("งานขาย", item.DomainTh);
        Assert.DoesNotContain("สมชาย", Newtonsoft.Json.JsonConvert.SerializeObject(item));
        Assert.Contains(item.Metrics, m => m.Name == "count" && m.Value == 3);
        Assert.Contains(item.ReceivableMetrics, m => m.Name == "overdueThb");
        Assert.Empty(item.CostMetrics);
        Assert.DoesNotContain("รหัส", item.Message);
    }
}
