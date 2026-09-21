using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock
{
    // Row-lock helper ที่ต้องเรียกภายใน transaction ที่เปิดอยู่แล้วเท่านั้น — lock ที่ถูกยิงนอก transaction จะถูกปล่อยทันทีและไม่ได้กันอะไรเลย
    // ลำดับ lock ต้องเรียงเดียวกันทุก path เสมอ กัน deadlock: sale material header -> invoice header -> SO deposit -> stock piece -> stock balance -> sale order product
    public static class StockLockHelper
    {
        public static async Task LockSaleMaterialHeaderAsync(this JewelryContext context, string running)
        {
            EnsureTransaction(context);

            if (string.IsNullOrEmpty(running)) return;

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM tbt_sale_material_header
                WHERE running = {running}
                FOR UPDATE");
        }

        public static async Task LockSaleOrderDepositsAsync(this JewelryContext context, string soNumber)
        {
            EnsureTransaction(context);

            if (string.IsNullOrEmpty(soNumber)) return;

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM tbt_sale_order_deposit
                WHERE so_number = {soNumber} AND is_delete = false
                ORDER BY running
                FOR UPDATE");
        }

        public static async Task LockStockPiecesAsync(this JewelryContext context, IEnumerable<string> stockNumbers)
        {
            EnsureTransaction(context);

            var numbers = DistinctOrdered(stockNumbers);
            if (numbers.Length == 0) return;

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM tbt_stock_piece
                WHERE stock_number = ANY({numbers})
                ORDER BY stock_number, product_code
                FOR UPDATE");
        }

        public static async Task LockStockBalancesAsync(this JewelryContext context, IEnumerable<string> skuCodes)
        {
            EnsureTransaction(context);

            var codes = DistinctOrdered(skuCodes);
            if (codes.Length == 0) return;

            // ล็อกทุกแถวของ SKU เหล่านี้ (ทุกคลัง) ไม่ใช่แค่คลังเดียว — การย้ายคลังแตะ balance สองแถวพร้อมกัน
            await context.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM tbt_stock_balance
                WHERE sku_code = ANY({codes})
                ORDER BY sku_code, location_code
                FOR UPDATE");
        }

        public static async Task LockInvoiceHeaderAsync(this JewelryContext context, string running)
        {
            EnsureTransaction(context);

            if (string.IsNullOrEmpty(running)) return;

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM tbt_sale_invoice_header
                WHERE running = {running}
                FOR UPDATE");
        }

        public static async Task LockSaleOrderProductsAsync(this JewelryContext context, string soNumber, IEnumerable<string> stockNumbers)
        {
            EnsureTransaction(context);

            if (string.IsNullOrEmpty(soNumber)) return;

            var numbers = DistinctOrdered(stockNumbers);
            if (numbers.Length == 0) return;

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM tbt_sale_order_product
                WHERE so_number = {soNumber} AND stock_number = ANY({numbers})
                ORDER BY id
                FOR UPDATE");
        }

        public static async Task LockSaleOrderProductsByIdsAsync(this JewelryContext context, IEnumerable<long> ids)
        {
            EnsureTransaction(context);

            var idArray = (ids ?? Enumerable.Empty<long>()).Distinct().OrderBy(x => x).ToArray();
            if (idArray.Length == 0) return;

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM tbt_sale_order_product
                WHERE id = ANY({idArray})
                ORDER BY id
                FOR UPDATE");
        }

        private static string[] DistinctOrdered(IEnumerable<string> stockNumbers)
        {
            return (stockNumbers ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();
        }

        private static void EnsureTransaction(JewelryContext context)
        {
            if (context.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException("Stock row lock ต้องเรียกภายใน transaction ที่เปิดอยู่เท่านั้น");
            }
        }
    }
}
