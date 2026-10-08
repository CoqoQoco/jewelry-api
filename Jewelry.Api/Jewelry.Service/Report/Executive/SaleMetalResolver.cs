using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Report.Executive
{
    // สัดส่วนโลหะของ "บิล" (ใบแจ้งหนี้ หรือ ใบสั่งขาย) — คำนวณจากบรรทัดขาย tbt_sale_order_product
    // metal ต่อบรรทัด: IsPlaceholder หรือไม่พบ piece (stock_number) = UNKNOWN (ไม่นับใน GOLD/SILVER, นับใน ALL) —
    // พบ piece → tbt_sku.production_type ผ่าน MetalClassifier (ว่าง = GOLD) — ใช้ลำดับ resolve เดียวกับ
    // SaleReportService.BuildFilteredSaleLines (stock_number → piece → sku; piece PK ผสม จึง group แล้วหยิบตัวแรกใน memory)
    public class SaleMetalShare
    {
        public decimal GoldAmount { get; set; }
        public decimal SilverAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int GoldLines { get; set; }
        public int SilverLines { get; set; }
        public int TotalLines { get; set; }

        // ALL = 1 เสมอ (พฤติกรรมเดิม) — GOLD/SILVER = สัดส่วนยอดบรรทัด (NetPrice*Qty) ของโลหะนั้นต่อทั้งบิล
        // (ยอดรวมทั้งบิลรวมบรรทัด UNKNOWN ด้วย จึงผลรวม GOLD+SILVER อาจ < 1) — ถ้ายอดรวมบรรทัดเป็น 0 ใช้สัดส่วนจำนวนบรรทัดแทน
        public decimal ShareOf(string metalFilter)
        {
            if (metalFilter == MetalClassifier.All) return 1m;
            var isSilver = metalFilter == MetalClassifier.Silver;
            if (TotalAmount > 0) return (isSilver ? SilverAmount : GoldAmount) / TotalAmount;
            if (TotalLines > 0) return (decimal)(isSilver ? SilverLines : GoldLines) / TotalLines;
            return 0m;
        }
    }

    public class SaleMetalResolver
    {
        private readonly JewelryContext _jewelryContext;

        public SaleMetalResolver(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        private class LineRow
        {
            public string? Key { get; set; }
            public string StockNumber { get; set; } = null!;
            public decimal? NetPrice { get; set; }
            public decimal Qty { get; set; }
            public bool IsPlaceholder { get; set; }
        }

        // key = tbt_sale_invoice_header.running — บิลที่ไม่มีบรรทัดขายเลยจะไม่มี key (share GOLD/SILVER = 0)
        public async Task<Dictionary<string, SaleMetalShare>> ByInvoiceAsync(List<string> invoiceRunnings)
        {
            if (invoiceRunnings.Count == 0) return new Dictionary<string, SaleMetalShare>();

            var lines = await _jewelryContext.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(x => x.Invoice != null && invoiceRunnings.Contains(x.Invoice))
                .Select(x => new LineRow { Key = x.Invoice, StockNumber = x.StockNumber, NetPrice = x.NetPrice, Qty = x.Qty, IsPlaceholder = x.IsPlaceholder })
                .ToListAsync();

            return await BuildSharesAsync(lines);
        }

        // key = tbt_sale_order.so_number
        public async Task<Dictionary<string, SaleMetalShare>> BySalesOrderAsync(List<string> soNumbers)
        {
            if (soNumbers.Count == 0) return new Dictionary<string, SaleMetalShare>();

            var lines = await _jewelryContext.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(x => soNumbers.Contains(x.SoNumber))
                .Select(x => new LineRow { Key = x.SoNumber, StockNumber = x.StockNumber, NetPrice = x.NetPrice, Qty = x.Qty, IsPlaceholder = x.IsPlaceholder })
                .ToListAsync();

            return await BuildSharesAsync(lines);
        }

        private async Task<Dictionary<string, SaleMetalShare>> BuildSharesAsync(List<LineRow> lines)
        {
            var stockNumbers = lines.Where(l => !l.IsPlaceholder).Select(l => l.StockNumber).Distinct().ToList();

            // tbt_stock_piece PK ผสม (stock_number, product_code) — ห้าม ToDictionary ตรงๆ ด้วย StockNumber
            var pieceRows = stockNumbers.Count == 0
                ? new List<(string StockNumber, string SkuCode)>()
                : (await _jewelryContext.TbtStockPiece
                    .AsNoTracking()
                    .Where(x => stockNumbers.Contains(x.StockNumber))
                    .Select(x => new { x.StockNumber, x.SkuCode })
                    .ToListAsync())
                    .Select(x => (x.StockNumber, x.SkuCode))
                    .ToList();

            var skuCodeByStockNumber = pieceRows
                .GroupBy(x => x.StockNumber)
                .ToDictionary(g => g.Key, g => g.First().SkuCode);

            var skuCodes = skuCodeByStockNumber.Values.Distinct().ToList();
            var productionTypeBySku = skuCodes.Count == 0
                ? new Dictionary<string, string?>()
                : (await _jewelryContext.TbtSku
                    .AsNoTracking()
                    .Where(x => skuCodes.Contains(x.SkuCode))
                    .Select(x => new { x.SkuCode, x.ProductionType })
                    .ToListAsync())
                    .GroupBy(x => x.SkuCode)
                    .ToDictionary(g => g.Key, g => g.First().ProductionType);

            var result = new Dictionary<string, SaleMetalShare>();
            foreach (var line in lines)
            {
                if (line.Key == null) continue;
                if (!result.TryGetValue(line.Key, out var share))
                {
                    share = new SaleMetalShare();
                    result[line.Key] = share;
                }

                var amount = (line.NetPrice ?? 0) * line.Qty;
                share.TotalAmount += amount;
                share.TotalLines++;

                if (line.IsPlaceholder) continue;
                if (!skuCodeByStockNumber.TryGetValue(line.StockNumber, out var skuCode)) continue; // ไม่มี piece = UNKNOWN

                productionTypeBySku.TryGetValue(skuCode, out var productionType);
                if (MetalClassifier.IsSilver(productionType))
                {
                    share.SilverAmount += amount;
                    share.SilverLines++;
                }
                else
                {
                    share.GoldAmount += amount;
                    share.GoldLines++;
                }
            }

            return result;
        }
    }
}
