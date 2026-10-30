using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jewelry.Data.Context;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Material.Insight
{
    // พลอย: tbt_stock_gem (ยอดคงเหลือ) + tbt_stock_gem_transection (ledger) — ดู reference_gem_stock_semantics
    //   quantity (พร้อมใช้) กับ quantity_on_process (อยู่ที่ช่าง) เป็นถังแยกกัน ห้ามหักลบ; ของจริงทั้งหมด = ผลบวก
    //   หน่วยตัดสินรายตัวจากข้อมูล (ห้ามใช้ unit_code ที่สกปรก):
    //     price_qty > 0 -> ขายเป็นเม็ด (pcs, วัดด้วย quantity) | price > 0 -> ขายเป็นน้ำหนัก (ct, วัดด้วย quantity_weight)
    //     ไม่มีราคา -> quantity_weight > 0 ? ct : pcs
    //   ledger type: เข้า {1,2,3,6} ออก {4,5,7}; ใช้จริง = 4+7; 5 (ยืมออก) คู่กับ 6 (คืนเข้า) = Other
    public class GemMaterialInsightProvider : IMaterialInsightProvider
    {
        public const string GemKind = "GEM";
        public const string UnitPieces = "pcs";
        public const string UnitCarat = "ct";

        private const int TypeBorrow = 5;
        private const int TypeReturnBorrow = 6;
        private const int TypeIssueToPlan = 7;
        private const int TypeMaterialSale = 8;
        private const int TypeMaterialSaleCancel = 9;
        private const string StatusProcess = "process";

        private readonly JewelryContext _jewelryContext;

        public GemMaterialInsightProvider(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public string Kind => GemKind;
        public string Unit => "mixed";

        // เลือกหน่วยและราคาต่อหน่วยของพลอยหนึ่งรายการ
        public static (string Unit, decimal UnitValue) DecideUnit(decimal price, decimal priceQty, decimal quantityWeight)
        {
            if (priceQty > 0) return (UnitPieces, priceQty);
            if (price > 0) return (UnitCarat, price);
            return (quantityWeight > 0 ? UnitCarat : UnitPieces, 0m);
        }

        // Type: 1/2 รับเข้า -> Inflow | 3 รับคืน -> Returned | 4/7 จ่าย/เบิกใช้ -> Consumed | 5/6 ยืมออก/คืนเข้า, 8/9 ขาย/ยกเลิกขายวัตถุดิบ -> Other
        // Sign = ผลต่อ "ยอดพร้อมใช้ (quantity)": type 7 เกิดตอนปิดใบยืม (5) — ของถูกหักจาก quantity ไปแล้วตอน 5
        //   และตัดจาก quantity_on_process เท่านั้น -> Sign 0 (ยังนับเป็นการใช้) กันหักซ้ำในยอดย้อนหลัง
        public static MaterialMovement? MapMovement(string itemKey, int type, decimal qty, DateTime dateUtc)
        {
            // 8 ขายวัตถุดิบ (หัก quantity+quantity_weight) / 9 ยกเลิกการขาย (คืนยอด) — ไม่ใช่การใช้ผลิต แต่มีผลต่อยอดคงเหลือ
            if (type == TypeMaterialSale) return new MaterialMovement(itemKey, dateUtc, MaterialMovementKind.Other, qty, -1);
            if (type == TypeMaterialSaleCancel) return new MaterialMovement(itemKey, dateUtc, MaterialMovementKind.Other, qty, 1);

            var inbound = StockGemService.InboundTypes.Contains(type);
            var outbound = StockGemService.OutboundTypes.Contains(type);
            if (!inbound && !outbound) return null;

            var sign = type == TypeIssueToPlan ? 0 : inbound ? 1 : -1;
            var kind = type switch
            {
                1 or 2 => MaterialMovementKind.Inflow,
                3 => MaterialMovementKind.Returned,
                4 or TypeIssueToPlan => MaterialMovementKind.Consumed,
                _ => MaterialMovementKind.Other
            };

            return new MaterialMovement(itemKey, dateUtc, kind, qty, sign);
        }

        public async Task<MaterialDataset> LoadAsync()
        {
            var now = DateTime.UtcNow;

            var stock = await _jewelryContext.TbtStockGem
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.GroupName,
                    x.Shape,
                    x.Size,
                    x.Grade,
                    x.Price,
                    x.PriceQty,
                    x.Quantity,
                    x.QuantityOnProcess,
                    x.QuantityWeight,
                    x.QuantityWeightOnProcess,
                    x.CreateDate
                })
                .ToListAsync();

            var gems = await _jewelryContext.TbmGem
                .AsNoTracking()
                .Select(g => new { g.Code, g.NameTh, g.NameEn })
                .ToListAsync();
            var gemNames = gems
                .Where(g => !string.IsNullOrWhiteSpace(g.Code))
                .GroupBy(g => g.Code.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.First().NameTh) ? g.First().NameEn : g.First().NameTh);

            var allTypes = StockGemService.InboundTypes.Concat(StockGemService.OutboundTypes).Concat(new[] { TypeMaterialSale, TypeMaterialSaleCancel }).Distinct().ToArray();
            var tx = await _jewelryContext.TbtStockGemTransection
                .AsNoTracking()
                .Where(x => allTypes.Contains(x.Type))
                .Select(x => new { x.Code, x.Type, x.Qty, x.QtyWeight, x.Stastus, x.RequestDate })
                .ToListAsync();

            // 1 Code = 1 item (กันแถวซ้ำ): หน่วย/ราคาตามแถวแรก, ยอดรวมทุกแถวในหน่วยนั้น
            var items = new List<MaterialItemInput>();
            foreach (var g in stock.GroupBy(x => x.Code).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var first = g.OrderBy(x => x.Id).First();
                var (unit, unitValue) = DecideUnit(first.Price, first.PriceQty, first.QuantityWeight);
                var byCount = unit == UnitPieces;
                var groupCode = first.GroupName?.Trim() ?? string.Empty;
                gemNames.TryGetValue(groupCode.ToUpperInvariant(), out var groupName);
                var display = string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName;

                items.Add(new MaterialItemInput
                {
                    ItemKey = g.Key,
                    Code = g.Key,
                    Name = $"{display} {first.Shape}".Trim(),
                    SizeCode = first.Size,
                    SizeName = $"{first.Size} {first.Grade}".Trim(),
                    GroupCode = groupCode,
                    GroupName = display,
                    Unit = unit,
                    UnitValue = unitValue,
                    OnHand = g.Sum(x => byCount ? x.Quantity : x.QuantityWeight),
                    OnProcess = g.Sum(x => byCount ? x.QuantityOnProcess : x.QuantityWeightOnProcess),
                    FirstSeenUtc = AsUtc(g.Min(x => x.CreateDate))
                });
            }

            var unitByKey = items.ToDictionary(i => i.ItemKey, i => i.Unit, StringComparer.Ordinal);

            // อายุของที่ค้างที่ช่าง = วันนับจากใบยืมออก (type 5) ที่ยังเปิด (status 'process') เก่าสุด
            var oldestOpen = tx
                .Where(x => x.Type == TypeBorrow && string.Equals(x.Stastus, StatusProcess, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Code)
                .ToDictionary(g => g.Key, g => g.Min(x => x.RequestDate), StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (oldestOpen.TryGetValue(item.ItemKey, out var since))
                {
                    item.OnProcessAgeDays = Math.Max(0, (int)Math.Floor((now - AsUtc(since)).TotalDays));
                }
            }

            var movements = new List<MaterialMovement>();
            foreach (var x in tx)
            {
                if (!unitByKey.TryGetValue(x.Code, out var unit)) continue;
                var qty = unit == UnitPieces ? x.Qty : x.QtyWeight;
                var m = MapMovement(x.Code, x.Type, qty, AsUtc(x.RequestDate));
                if (m != null) movements.Add(m);
            }

            return new MaterialDataset
            {
                OnProcessAgeSupported = true,
                InboundRecorded = tx.Any(x => x.Type != TypeReturnBorrow && StockGemService.InboundTypes.Contains(x.Type)),
                Items = items,
                Movements = movements
            };
        }

        private static DateTime AsUtc(DateTime d)
            => d.Kind == DateTimeKind.Utc ? d : DateTime.SpecifyKind(d, DateTimeKind.Utc);
    }
}
