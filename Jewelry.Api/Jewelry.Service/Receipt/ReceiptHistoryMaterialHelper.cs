using jewelry.Model.Receipt;
using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Receipt
{
    internal static class ReceiptHistoryMaterialHelper
    {
        private enum Group { None, Gold, Diamond, Ruby, Sapphire, Emerald, Other }

        private static Group Classify(string? type, string? typeCode)
        {
            var t = (type ?? string.Empty).Trim().ToLowerInvariant();
            var c = (typeCode ?? string.Empty).Trim().ToLowerInvariant();

            if (t == "gold") return Group.Gold;
            if (t == "diamond") return Group.Diamond;
            if (t != "gem") return Group.None;

            if (c == "diamond") return Group.Diamond;
            if (c.Contains("ruby")) return Group.Ruby;
            if (c == "sapphire" || c == "fancy sapphire") return Group.Sapphire;
            if (c == "emerald") return Group.Emerald;
            return Group.Other;
        }

        public static void Enrich<T>(JewelryContext context, IEnumerable<T> rows, Func<T, string?> stockNumberOf, bool includeMaterials = false) where T : ReceiptHistoryMaterial
        {
            var list = rows.ToList();
            if (list.Count == 0) return;

            var stockNumbers = list
                .Select(stockNumberOf)
                .Where(x => x != null)
                .Select(x => x!)
                .Distinct()
                .ToList();

            var materials = context.TbtStockPieceMaterial
                .AsNoTracking()
                .Where(m => stockNumbers.Contains(m.StockNumber))
                .Select(m => new { m.Id, m.StockNumber, m.Type, m.TypeName, m.TypeCode, m.TypeOrigin, m.Size, m.Qty, m.QtyUnit, m.Weight, m.WeightUnit, m.Region, m.Price, m.PriceWeight })
                .ToList()
                .ToLookup(m => m.StockNumber);

            foreach (var row in list)
            {
                var stockNumber = stockNumberOf(row);
                if (stockNumber == null) continue;

                var goldCodes = new List<string>();
                foreach (var m in materials[stockNumber])
                {
                    var qty = m.Qty ?? 0;
                    var weight = m.Weight ?? 0;
                    switch (Classify(m.Type, m.TypeCode))
                    {
                        case Group.Gold:
                            row.GoldWeight += weight;
                            var code = (m.TypeCode ?? string.Empty).Trim();
                            if (code.Length > 0 && !goldCodes.Contains(code)) goldCodes.Add(code);
                            break;
                        case Group.Diamond: row.DiamondQty += qty; row.DiamondWeight += weight; break;
                        case Group.Ruby: row.RubyQty += qty; row.RubyWeight += weight; break;
                        case Group.Sapphire: row.SapphireQty += qty; row.SapphireWeight += weight; break;
                        case Group.Emerald: row.EmeraldQty += qty; row.EmeraldWeight += weight; break;
                        case Group.Other: row.OtherQty += qty; row.OtherWeight += weight; break;
                    }
                }
                row.GoldType = goldCodes.Count > 0 ? string.Join("/", goldCodes) : null;

                if (includeMaterials)
                {
                    row.Materials = materials[stockNumber]
                        .OrderBy(m => Classify(m.Type, m.TypeCode) == Group.Gold ? 0 : Classify(m.Type, m.TypeCode) == Group.Diamond ? 1 : 2)
                        .ThenBy(m => m.Id)
                        .Select(m => new ReceiptHistoryMaterialLine
                        {
                            Type = m.Type,
                            TypeName = m.TypeName,
                            TypeCode = m.TypeCode,
                            TypeOrigin = m.TypeOrigin,
                            Size = m.Size,
                            Qty = m.Qty,
                            QtyUnit = m.QtyUnit,
                            Weight = m.Weight,
                            WeightUnit = m.WeightUnit,
                            Region = m.Region,
                            Price = m.Price,
                            PriceWeight = m.PriceWeight
                        })
                        .ToList();
                }
            }
        }

        public static ReceiptHistorySummary Summarize(JewelryContext context, IQueryable<string> stockNumbers, IQueryable<decimal> qtys, IQueryable<decimal> sales)
        {
            var summary = new ReceiptHistorySummary
            {
                TotalQty = (int)qtys.Sum(),
                TotalSales = sales.Sum()
            };
            if (stockNumbers.Count() == 0) return summary;

            var groups = context.TbtStockPieceMaterial
                .AsNoTracking()
                .Where(m => stockNumbers.Contains(m.StockNumber))
                .GroupBy(m => new { m.Type, m.TypeCode })
                .Select(g => new
                {
                    g.Key.Type,
                    g.Key.TypeCode,
                    Qty = g.Sum(x => x.Qty ?? 0),
                    Weight = g.Sum(x => x.Weight ?? 0)
                })
                .ToList();

            foreach (var g in groups)
            {
                switch (Classify(g.Type, g.TypeCode))
                {
                    case Group.Gold: summary.GoldWeight += g.Weight; break;
                    case Group.Diamond: summary.DiamondQty += g.Qty; summary.DiamondWeight += g.Weight; break;
                    case Group.Ruby: summary.RubyQty += g.Qty; summary.RubyWeight += g.Weight; break;
                    case Group.Sapphire: summary.SapphireQty += g.Qty; summary.SapphireWeight += g.Weight; break;
                    case Group.Emerald: summary.EmeraldQty += g.Qty; summary.EmeraldWeight += g.Weight; break;
                    case Group.Other: summary.OtherQty += g.Qty; summary.OtherWeight += g.Weight; break;
                }
            }
            return summary;
        }
    }
}
