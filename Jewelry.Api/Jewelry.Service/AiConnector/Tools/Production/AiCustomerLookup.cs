using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Tools.Production
{
    // แยกออกมาเพื่อให้เทสสลับตัวค้นได้ (ILike ใช้กับ in-memory provider ไม่ได้)
    // ค่าที่คืน = "คีย์ลูกค้า": รหัสใน tbm_customer หรือค่า customer_number (free text) ของใบงานที่ยังไม่สำเร็จ/หลอมที่ไม่อยู่ใน master
    public interface IAiCustomerLookup
    {
        // ตรงทั้งคำ ไม่สนตัวพิมพ์ ตัดช่องว่าง
        Task<List<string>> FindByCode(string code, CancellationToken ct);
        // pattern ผ่านการ escape แล้ว (รูปแบบ %xxx%) — master ตามชื่อ + ค่าในใบงานที่ไม่อยู่ใน master
        Task<List<string>> FindByNamePattern(string pattern, int max, CancellationToken ct);
    }

    public class AiCustomerLookup : IAiCustomerLookup
    {
        private readonly JewelryContext _context;

        public AiCustomerLookup(JewelryContext context)
        {
            _context = context;
        }

        public async Task<List<string>> FindByCode(string code, CancellationToken ct)
        {
            var norm = code.Trim().ToUpper();
            var result = await _context.TbmCustomer.AsNoTracking()
                .Where(c => c.Code.Trim().ToUpper() == norm)
                .OrderBy(c => c.Code)
                .Select(c => c.Code)
                .Take(10)
                .ToListAsync(ct);

            var planValues = await _context.TbtProductionPlan.AsNoTracking()
                .Where(p => p.IsActive == true && p.Status != 100 && p.CustomerNumber.Trim().ToUpper() == norm)
                .Select(p => p.CustomerNumber.Trim())
                .Distinct()
                .Take(10)
                .ToListAsync(ct);
            return Merge(result, planValues, 10);
        }

        public async Task<List<string>> FindByNamePattern(string pattern, int max, CancellationToken ct)
        {
            var result = await _context.TbmCustomer.AsNoTracking()
                .Where(c => EF.Functions.ILike(c.NameTh, pattern) || (c.NameEn != null && EF.Functions.ILike(c.NameEn, pattern)))
                .OrderBy(c => c.Code)
                .Select(c => c.Code)
                .Take(max)
                .ToListAsync(ct);

            var planValues = await _context.TbtProductionPlan.AsNoTracking()
                .Where(p => p.IsActive == true && p.Status != 100 && EF.Functions.ILike(p.CustomerNumber, pattern))
                .Select(p => p.CustomerNumber.Trim())
                .Distinct()
                .Take(50)
                .ToListAsync(ct);
            if (planValues.Count > 0)
            {
                var upper = planValues.Select(v => v.ToUpper()).ToList();
                var inMaster = (await _context.TbmCustomer.AsNoTracking()
                    .Where(c => upper.Contains(c.Code.Trim().ToUpper()))
                    .Select(c => c.Code.Trim().ToUpper())
                    .ToListAsync(ct)).ToHashSet();
                planValues = planValues.Where(v => !inMaster.Contains(v.ToUpper())).ToList();
            }
            return Merge(result, planValues, max);
        }

        // รวมแบบไม่ซ้ำ (ไม่สนตัวพิมพ์/ช่องว่าง) — master มาก่อน
        public static List<string> Merge(IEnumerable<string> master, IEnumerable<string> planValues, int max) =>
            master.Concat(planValues)
                .Select(v => v.Trim())
                .Where(v => v.Length > 0)
                .GroupBy(v => v.ToUpperInvariant())
                .Select(g => g.First())
                .Take(max)
                .ToList();
    }
}
