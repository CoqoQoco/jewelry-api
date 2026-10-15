using jewelry.Model.Constant;
using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Shared
{
    // แผนที่ถูก "ปิดใบงานย้อนหลัง" = มี status header Status=100 ที่ remark1 ขึ้นต้นด้วย BackdateCloseMarker.RemarkPrefix
    // key = plan id, value = เวลาที่ header ปิดย้อนหลังถูกสร้างจริง (MIN create_date) — GroupBy+Min เท่านั้น (ห้าม First())
    public static class BackdateClosedPlans
    {
        public const int StatusCompleted = 100;

        public static async Task<Dictionary<int, DateTime>> LoadAsync(JewelryContext context)
        {
            var rows = await context.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .Where(h => h.Status == StatusCompleted && h.Remark1 != null && h.Remark1.StartsWith(BackdateCloseMarker.RemarkPrefix))
                .GroupBy(h => h.ProductionPlanId)
                .Select(g => new { PlanId = g.Key, MarkerDate = g.Min(x => x.CreateDate) })
                .ToListAsync();

            return rows.ToDictionary(x => x.PlanId, x => x.MarkerDate);
        }
    }
}
