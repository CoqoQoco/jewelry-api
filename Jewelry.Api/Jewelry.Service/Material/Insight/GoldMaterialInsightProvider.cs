using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using jewelry.Model.Constant;
using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Material.Insight
{
    // ทอง/โลหะ: tbt_stock_gold (ยอดคงเหลือ) + tbt_stock_gold_transection (ledger) — ดู GoldStockTransactionType
    //   onHand = weight (ในคลัง พร้อมเบิก), onProcess = weight_on_process
    //   (โค้ด PostMovement ไม่ได้อัปเดต weight_on_process เลย -> ปกติเป็น 0; ไม่มีวันที่ค้าง => ไม่รองรับ MAT_ON_PROCESS_STALE)
    //   ledger: เบิก = type 4 (Outbound) / คืน = type 3 (ReturnIn) ที่ status != reversed
    public class GoldMaterialInsightProvider : IMaterialInsightProvider
    {
        public const string GoldKind = "GOLD";

        private readonly JewelryContext _jewelryContext;

        public GoldMaterialInsightProvider(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public string Kind => GoldKind;
        public string Unit => "g";

        public static string BuildItemKey(string goldCode, string goldSizeCode) => $"{goldCode}|{goldSizeCode}";

        public async Task<MaterialDataset> LoadAsync()
        {
            var stock = await _jewelryContext.TbtStockGold
                .AsNoTracking()
                .Select(x => new
                {
                    x.GoldCode,
                    x.GoldSizeCode,
                    GoldName = x.GoldCodeNavigation.NameTh,
                    SizeName = x.GoldSizeCodeNavigation.NameTh,
                    x.Weight,
                    x.WeightOnProcess,
                    x.CreateDate
                })
                .ToListAsync();

            var tx = await _jewelryContext.TbtStockGoldTransection
                .AsNoTracking()
                .Select(x => new { x.GoldCode, x.GoldSizeCode, x.Type, x.Weight, x.Status, x.RequestDate, x.CreateDate })
                .ToListAsync();

            return new MaterialDataset
            {
                OnProcessAgeSupported = false,
                Items = stock
                    .OrderBy(x => x.GoldCode, StringComparer.Ordinal)
                    .ThenBy(x => x.GoldSizeCode, StringComparer.Ordinal)
                    .Select(x => new MaterialItemInput
                    {
                        ItemKey = BuildItemKey(x.GoldCode, x.GoldSizeCode),
                        Code = x.GoldCode,
                        Name = x.GoldName,
                        SizeCode = x.GoldSizeCode,
                        SizeName = x.SizeName,
                        OnHand = x.Weight,
                        OnProcess = x.WeightOnProcess,
                        FirstSeenUtc = AsUtc(x.CreateDate)
                    })
                    .ToList(),
                Movements = tx
                    .Select(x => MapMovement(BuildItemKey(x.GoldCode, x.GoldSizeCode), x.Type, x.Weight, x.Status, AsUtc(x.RequestDate ?? x.CreateDate)))
                    .Where(m => m != null)
                    .Select(m => m!)
                    .ToList()
            };
        }

        // Type: 1 รับเข้า(ซื้อ) -> Inflow | 3 คืน -> Returned | 4 เบิก -> Consumed
        //       2 ยกมา, 5/6 ปรับยอด, 7/8 กลับรายการ -> Other | status 'reversed' (ต้นทางที่ถูกกลับแล้ว) -> Other
        //       (เบิก/คืนที่ถูกกลับรายการต้องไม่นับเป็นการใช้ แต่ยังมีผลต่อยอดคงเหลือเพราะมีแถว 7/8 หักล้างอยู่)
        public static MaterialMovement? MapMovement(string itemKey, int type, decimal weight, string? status, DateTime dateUtc)
        {
            int sign;
            if (GoldStockTransactionType.IsInbound(type)) sign = 1;
            else if (GoldStockTransactionType.IsOutbound(type)) sign = -1;
            else return null;

            var reversed = string.Equals(status, GoldStockTransactionStatus.Reversed, StringComparison.OrdinalIgnoreCase);
            var kind = MaterialMovementKind.Other;
            if (!reversed)
            {
                if (type == GoldStockTransactionType.Inbound) kind = MaterialMovementKind.Inflow;
                else if (type == GoldStockTransactionType.ReturnIn) kind = MaterialMovementKind.Returned;
                else if (type == GoldStockTransactionType.Outbound) kind = MaterialMovementKind.Consumed;
            }

            return new MaterialMovement(itemKey, dateUtc, kind, weight, sign);
        }

        private static DateTime AsUtc(DateTime d)
            => d.Kind == DateTimeKind.Utc ? d : DateTime.SpecifyKind(d, DateTimeKind.Utc);

        private static DateTime? AsUtc(DateTime? d) => d.HasValue ? AsUtc(d.Value) : null;
    }
}
