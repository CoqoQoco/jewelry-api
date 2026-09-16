using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleOrderDeposit
{
    // ใช้ร่วมกันระหว่าง SaleOrderDepositService, InvoiceService (หักมัดจำตอนออก invoice)
    // และ SaleOrderService (เช็คยอดมัดจำคงเหลือก่อนยกเลิก SO)
    public static class SaleOrderDepositHelper
    {
        // balance = Σ active deposit.amount − Σ active apply.amount ของ SO นั้น
        public static async Task<decimal> GetBalanceAsync(JewelryContext context, string soNumber)
        {
            var totalDeposit = await context.TbtSaleOrderDeposit
                .Where(d => d.SoNumber == soNumber && !d.IsDelete)
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;

            var totalApplied = await context.TbtSaleOrderDepositApply
                .Where(a => a.SoNumber == soNumber && !a.IsDelete)
                .SumAsync(a => (decimal?)a.Amount) ?? 0m;

            return totalDeposit - totalApplied;
        }

        // มัดจำที่ยัง active และยังมีเศษเหลือ (amount - applied ที่ active) เรียงตาม depositDate/createDate ไว้ allocate แบบ FIFO
        public static async Task<List<(TbtSaleOrderDeposit Deposit, decimal Remaining)>> GetAvailableDepositsAsync(JewelryContext context, string soNumber)
        {
            var deposits = await context.TbtSaleOrderDeposit
                .Where(d => d.SoNumber == soNumber && !d.IsDelete)
                .OrderBy(d => d.DepositDate)
                .ThenBy(d => d.CreateDate)
                .ToListAsync();

            if (deposits.Count == 0)
            {
                return new List<(TbtSaleOrderDeposit, decimal)>();
            }

            var runnings = deposits.Select(d => d.Running).ToList();

            var appliedByDeposit = await context.TbtSaleOrderDepositApply
                .Where(a => runnings.Contains(a.DepositRunning) && !a.IsDelete)
                .GroupBy(a => a.DepositRunning)
                .Select(g => new { DepositRunning = g.Key, Total = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.DepositRunning, x => x.Total);

            var result = new List<(TbtSaleOrderDeposit, decimal)>();
            foreach (var deposit in deposits)
            {
                var applied = appliedByDeposit.TryGetValue(deposit.Running, out var total) ? total : 0m;
                var remaining = deposit.Amount - applied;
                if (remaining > 0)
                {
                    result.Add((deposit, remaining));
                }
            }

            return result;
        }
    }
}
