using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Report.Executive
{
    // แถวเงินระดับใบแจ้งหนี้ — ใช้ร่วมกันระหว่าง ExecutiveReportService.Summary และ SalesInsightService เพื่อให้ตัวเลขตรงกันเสมอ
    public class InvoiceMoneyRow
    {
        public string Running { get; set; } = null!;
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal Deposit { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal Rate { get; set; }
        public decimal Paid { get; set; }
        public decimal Share { get; set; } = 1m;
    }

    // ใบสั่งขาย (ไม่รวม Inactive) ที่ผ่านการกรองโลหะแล้ว (share > 0)
    public class SalesOrderMoneyRow
    {
        public string SoNumber { get; set; } = null!;
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal Rate { get; set; }
        public decimal Share { get; set; } = 1m;
        public bool HasInvoice { get; set; }
    }

    public class ReceivablesTotals
    {
        public int InvoiceCount { get; set; }
        public decimal InvoiceTotalThb { get; set; }
        public decimal PaidOrPartialThb { get; set; }
        public int PaidOrPartialCount { get; set; }
        public int UnpaidCount { get; set; }
        public decimal UnpaidThb { get; set; }
        public int OverdueCount { get; set; }
        public decimal OverdueThb { get; set; }
        public decimal UnpaidNotOverdueThb { get; set; }
        public int UnpaidNotOverdueCount { get; set; }
        public int NoDueDateCount { get; set; }
    }

    public class SalesOrderTotals
    {
        public int OpenCount { get; set; }
        public int NoInvoiceCount { get; set; }
        public decimal NoInvoiceThb { get; set; }
        public int OverdueNoInvoiceCount { get; set; }
        public int NoDeliveryDateCount { get; set; }
    }

    // สูตรเงินของแท็บ "ขายและเงิน" — Summary และ SalesInsight เรียกที่นี่ที่เดียว (ห้ามปัดค่าระหว่างทาง)
    public static class ExecutiveSalesMath
    {
        // unpaid = มียอดจริง (grand_total>0) และยังไม่มีเงินเข้าเลย ทั้ง payment_item และมัดจำ
        public static bool IsUnpaidStrict(decimal grandTotal, decimal paid, decimal deposit)
            => grandTotal > 0 && paid == 0 && deposit <= 0;

        public static bool IsUnpaidStrict(InvoiceMoneyRow x) => IsUnpaidStrict(x.GrandTotal, x.Paid, x.Deposit);

        public static decimal Thb(InvoiceMoneyRow x) => x.GrandTotal * x.Rate * x.Share;

        public static bool IsOverdue(InvoiceMoneyRow x, DateTime now) => x.DueDate.HasValue && x.DueDate.Value < now;

        // ยอดเงินคูณ Share (ALL = 1) — การนับบิล/สถานะ unpaid ใช้ค่าเต็มของบิล (ไม่คูณ)
        public static ReceivablesTotals SummarizeReceivables(IReadOnlyList<InvoiceMoneyRow> rows, DateTime now)
        {
            var invoiceTotalThb = rows.Sum(Thb);
            var unpaidRows = rows.Where(IsUnpaidStrict).ToList();
            var paidOrPartialRows = rows.Where(x => !IsUnpaidStrict(x)).ToList();
            var unpaidThb = unpaidRows.Sum(Thb);
            var overdueRows = unpaidRows.Where(x => IsOverdue(x, now)).ToList();
            var overdueThb = overdueRows.Sum(Thb);

            return new ReceivablesTotals
            {
                InvoiceCount = rows.Count,
                InvoiceTotalThb = invoiceTotalThb,
                PaidOrPartialThb = paidOrPartialRows.Sum(Thb),
                PaidOrPartialCount = paidOrPartialRows.Count,
                UnpaidCount = unpaidRows.Count,
                UnpaidThb = unpaidThb,
                OverdueCount = overdueRows.Count,
                OverdueThb = overdueThb,
                UnpaidNotOverdueThb = unpaidThb - overdueThb,
                UnpaidNotOverdueCount = unpaidRows.Count - overdueRows.Count,
                NoDueDateCount = rows.Count(x => !x.DueDate.HasValue)
            };
        }

        public static SalesOrderTotals SummarizeSalesOrders(IReadOnlyList<SalesOrderMoneyRow> soRows, DateTime now)
        {
            var noInvoiceRows = soRows.Where(x => !x.HasInvoice).ToList();
            return new SalesOrderTotals
            {
                OpenCount = soRows.Count,
                NoInvoiceCount = noInvoiceRows.Count,
                NoInvoiceThb = noInvoiceRows.Sum(x => x.GrandTotal * x.Rate * x.Share),
                OverdueNoInvoiceCount = noInvoiceRows.Count(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value < now),
                NoDeliveryDateCount = noInvoiceRows.Count(x => !x.DeliveryDate.HasValue)
            };
        }
    }

    // loader ร่วม — flat projection + aggregate ใน memory
    public static class ExecutiveSalesLoader
    {
        public static IQueryable<TbtSaleInvoiceHeader> InScopeInvoices(JewelryContext db)
            => db.TbtSaleInvoiceHeader.AsNoTracking().Where(x => x.IsDelete == false && x.InvoiceType != "MATERIAL");

        public static IQueryable<TbtSaleOrder> InScopeSalesOrders(JewelryContext db)
            => db.TbtSaleOrder.AsNoTracking().Where(x => x.StatusName != "Inactive");

        // เงินระดับ header (GrandTotalRounded, Deposit, paid) ถูกจัดสรรตามสัดส่วนโลหะของบรรทัดขาย — ALL = ไม่คูณ (share=1)
        // บิลที่ share=0 ในโลหะที่ขอ (ไม่มีบรรทัดโลหะนั้นเลย) ถูกตัดออก — สถานะบิล (unpaid/overdue) ตัดสินจากค่าเต็มก่อนคูณ share
        public static async Task<List<InvoiceMoneyRow>> GetInvoiceMoneyRowsAsync(JewelryContext db, string metalFilter)
        {
            var raw = await InScopeInvoices(db)
                .Select(x => new InvoiceMoneyRow
                {
                    Running = x.Running,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    CreateDate = x.CreateDate,
                    DueDate = x.DueDate,
                    Deposit = x.Deposit,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1
                })
                .ToListAsync();

            // paid = sum(payment_item.amount ที่ไม่ถูกลบ) ต่อบิล — รวมใน memory (ผลเท่ากับ subquery เดิม)
            var payments = await db.TbtSaleInvoicePaymentItem
                .AsNoTracking()
                .Where(p => p.IsDelete == false)
                .Select(p => new { p.InvoiceRunning, p.Amount })
                .ToListAsync();
            var paidByRunning = payments
                .GroupBy(p => p.InvoiceRunning)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
            foreach (var row in raw)
            {
                row.Paid = paidByRunning.TryGetValue(row.Running, out var paid) ? paid : 0m;
            }

            if (metalFilter == MetalClassifier.All) return raw;

            var shares = await new SaleMetalResolver(db).ByInvoiceAsync(raw.Select(x => x.Running).ToList());
            foreach (var row in raw)
            {
                row.Share = shares.TryGetValue(row.Running, out var sh) ? sh.ShareOf(metalFilter) : 0m;
            }

            return raw.Where(x => x.Share > 0).ToList();
        }

        // ใบสั่งขายทั้งหมดที่ผ่านตัวกรองโลหะ (share > 0) พร้อมธง HasInvoice (มีใบแจ้งหนี้ in-scope อ้าง so_running แล้ว)
        public static async Task<List<SalesOrderMoneyRow>> GetSalesOrderMoneyRowsAsync(JewelryContext db, string metalFilter)
        {
            var invoicedSoNumbers = new HashSet<string>(
                await InScopeInvoices(db).Select(x => x.SoRunning).Distinct().ToListAsync());

            var raw = await InScopeSalesOrders(db)
                .Select(x => new SalesOrderMoneyRow
                {
                    SoNumber = x.SoNumber,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    DeliveryDate = x.DeliveryDate,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1
                })
                .ToListAsync();

            foreach (var row in raw) row.HasInvoice = invoicedSoNumbers.Contains(row.SoNumber);

            if (metalFilter == MetalClassifier.All) return raw;

            var shares = await new SaleMetalResolver(db).BySalesOrderAsync(raw.Select(x => x.SoNumber).ToList());
            foreach (var row in raw)
            {
                row.Share = shares.TryGetValue(row.SoNumber, out var sh) ? sh.ShareOf(metalFilter) : 0m;
            }

            return raw.Where(x => x.Share > 0).ToList();
        }
    }
}
