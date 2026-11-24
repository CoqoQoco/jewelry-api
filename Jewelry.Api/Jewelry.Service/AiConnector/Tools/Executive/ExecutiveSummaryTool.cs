using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.Helper;
using Jewelry.Service.Report.Executive;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Summary = jewelry.Model.Report.Executive.Summary;

namespace Jewelry.Service.AiConnector.Tools.Executive
{
    public class ExecutiveSummaryTool : AiToolBase<AiMetalInput, AiExecutiveSummaryOutput>
    {
        public override string Name => "get_executive_summary";
        public override string TitleTh => "สรุปภาพรวมผู้บริหาร";
        public override string Description =>
            "ภาพรวมธุรกิจ ณ ปัจจุบัน (as-of ตอนเรียก ไม่ใช่ช่วงวัน): จำนวนใบงานผลิตที่เปิดอยู่/ขยับใน 30 วัน/นิ่งเกิน 180 วัน, บิลและยอดค้างชำระ, " +
            "ใบสั่งขายที่ยังไม่ออกบิล, สต็อกพร้อมขายและมูลค่า, และทองสูญเสียรายเดือน (กรัม) · พารามิเตอร์ metal = ALL | GOLD | SILVER · " +
            "ตัวเลขเงิน/ต้นทุนอาจถูกซ่อน (null) ถ้ายังไม่ได้รับอนุญาต · ควรเรียก get_glossary ก่อนตีความตัวเลข";
        public override string InputSchemaJson =>
            "{\"type\":\"object\",\"properties\":{\"metal\":{\"type\":\"string\",\"enum\":[\"ALL\",\"GOLD\",\"SILVER\"],\"description\":\"โลหะที่ต้องการ (ค่าเริ่มต้น ALL)\"}}}";
        public override IReadOnlyList<string> RequiredScopes => new[] { AiScopes.ModuleExecutive };
        public override IReadOnlyList<string> PermissionsAny => new[] { PermissionCodes.ExecutiveView };
        public override string WrapsEndpoint => "ExecutiveReportController.Summary";
        public override TimeSpan CacheTtl => TimeSpan.FromSeconds(60);
        public override IReadOnlyList<string> MetricCodes => new[] { "stale_plan", "unpaid_invoice", "diff_loss", "so_gold_rate", "stock_gold_negative", "weight_actual", "production_receipt_legacy" };

        protected override async Task<AiExecutiveSummaryOutput> RunAsync(AiMetalInput input, AiToolContext context, CancellationToken ct)
        {
            var metal = MetalClassifier.Normalize(input.Metal);
            var service = context.Services.GetRequiredService<IExecutiveReportService>();
            var r = await service.Summary(new Summary.Request { Metal = metal });
            return Map(r);
        }

        public static AiExecutiveSummaryOutput Map(Summary.Response r) => new AiExecutiveSummaryOutput
        {
            AsOf = r.AsOf,
            Metal = r.Metal,
            Production = new AiProductionSummary
            {
                OpenCount = r.Production.OpenCount,
                Moved30dCount = r.Production.Moved30dCount,
                Stale180dCount = r.Production.Stale180dCount,
                MeltedOpenCount = r.Production.MeltedOpenCount,
            },
            Receivables = new AiReceivablesSummary
            {
                InvoiceCount = r.Receivables.InvoiceCount,
                UnpaidCount = r.Receivables.UnpaidCount,
                OverdueCount = r.Receivables.OverdueCount,
                NoDueDateCount = r.Receivables.NoDueDateCount,
                InvoiceTotalThb = r.Receivables.InvoiceTotalThb,
                PaidOrPartialThb = r.Receivables.PaidOrPartialThb,
                UnpaidThb = r.Receivables.UnpaidThb,
                OverdueThb = r.Receivables.OverdueThb,
                UnpaidNotOverdueThb = r.Receivables.UnpaidNotOverdueThb,
            },
            SalesOrders = new AiSalesOrdersSummary
            {
                OpenCount = r.SalesOrders.OpenCount,
                NoInvoiceCount = r.SalesOrders.NoInvoiceCount,
                OverdueNoInvoiceCount = r.SalesOrders.OverdueNoInvoiceCount,
                NoDeliveryDateCount = r.SalesOrders.NoDeliveryDateCount,
                NoInvoiceThb = r.SalesOrders.NoInvoiceThb,
            },
            Stock = new AiStockSummary
            {
                InStockCount = r.Stock.InStockCount,
                InStockLotCount = r.Stock.InStockLotCount,
                NoCostLotCount = r.Stock.NoCostLotCount,
                ReservedCount = r.Stock.ReservedCount,
                NoCostCount = r.Stock.NoCostCount,
                AgedOver1yCount = r.Stock.AgedOver1yCount,
                CostThb = r.Stock.CostThb,
            },
            GoldLoss = r.GoldLoss.Select(MapMonth).ToList(),
            SilverLoss = r.SilverLoss.Select(MapMonth).ToList(),
        };

        private static AiGoldLossMonth MapMonth(Summary.GoldLossMonthData m) => new AiGoldLossMonth
        {
            Month = m.Month,
            SlipCount = m.SlipCount,
            IssuedGram = m.IssuedGram,
            RawLossGram = m.RawLossGram,
            AllowedGram = m.AllowedGram,
            OverAllowedGram = m.OverAllowedGram,
            OverSlipCount = m.OverSlipCount,
            LossPercent = m.LossPercent,
            AllowedPercent = m.AllowedPercent,
            OverAllowedPercent = m.OverAllowedPercent,
        };
    }
}
