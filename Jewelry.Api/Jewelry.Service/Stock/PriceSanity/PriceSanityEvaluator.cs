using jewelry.Model.Stock.Product.PriceCheck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jewelry.Service.Stock.PriceSanity
{
    public class PriceSanityContext
    {
        public string? StockNumber { get; set; }
        public string? SkuCode { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? MoldDesign { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public decimal GoldWeight { get; set; }
        public decimal GemPrice { get; set; }
        public decimal OldPrice { get; set; }
    }

    public class LnStats
    {
        public int N { get; set; }
        public double Mu { get; set; }
        public double Sd { get; set; }
    }

    public class TypeCeilingInfo
    {
        public int N { get; set; }
        public List<(string Sku, decimal Price)> Top { get; set; } = new();
    }

    public class WoPiece
    {
        public string StockNumber { get; set; } = null!;
        public string Sku { get; set; } = null!;
        public int? WoNumber { get; set; }
        public decimal Price { get; set; }
    }

    public class DesignSku
    {
        public string Sku { get; set; } = null!;
        public decimal Price { get; set; }
    }

    public class PriceSanitySnapshot
    {
        public Dictionary<string, LnStats> GroupStats { get; set; } = new();
        public LnStats GlobalStats { get; set; } = new();
        public Dictionary<string, TypeCeilingInfo> TypeCeilings { get; set; } = new();
        public Dictionary<string, List<WoPiece>> WoPieces { get; set; } = new();
        public Dictionary<string, List<DesignSku>> DesignSkus { get; set; } = new();
    }

    public static class PriceSanityEvaluator
    {
        public const double BaseGoldRatePerGram = 3000d;
        public const int MinGroupSize = 200;
        public const int MinTypeSize = 20;

        public static string NormalizeGroup(string? productionType)
            => (productionType ?? string.Empty).Trim().ToUpperInvariant();

        public static double Purity(string? productionType)
        {
            var m = Regex.Match(productionType ?? string.Empty, @"(\d{1,2})\s*K", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                switch (m.Groups[1].Value)
                {
                    case "18": return 0.75;
                    case "14": return 0.585;
                    case "9": return 0.375;
                }
            }
            return 0.75;
        }

        public static double Cost(decimal goldWeight, decimal gemPrice, string? productionType)
            => (double)goldWeight * (BaseGoldRatePerGram * Purity(productionType) / 0.75) + (double)gemPrice;

        public static decimal Median(IList<decimal> values)
        {
            var s = values.OrderBy(x => x).ToList();
            var n = s.Count;
            return n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) / 2m;
        }

        public static LnStats BuildStats(IList<double> values)
        {
            var n = values.Count;
            if (n == 0) return new LnStats();
            var mu = values.Average();
            var sd = n > 1 ? Math.Sqrt(values.Sum(v => (v - mu) * (v - mu)) / (n - 1)) : 0d;
            return new LnStats { N = n, Mu = mu, Sd = sd };
        }

        public static List<Reason> Evaluate(PriceSanitySnapshot snap, PriceSanityContext c, decimal price)
        {
            var reasons = new List<Reason>();
            if (price <= 0) return reasons;

            if (c.GoldWeight > 0)
            {
                var cost = Cost(c.GoldWeight, c.GemPrice, c.ProductionType);
                if (cost > 0)
                {
                    var stats = snap.GroupStats.TryGetValue(NormalizeGroup(c.ProductionType), out var g) && g.N >= MinGroupSize
                        ? g : snap.GlobalStats;
                    if (stats.N > 1 && stats.Sd > 0)
                    {
                        var lr = Math.Log((double)price / cost);
                        var z = (lr - stats.Mu) / stats.Sd;
                        if (Math.Abs(z) > 3)
                        {
                            reasons.Add(new Reason
                            {
                                Code = "COST_RATIO",
                                Value = Round((decimal)((double)price / cost)),
                                Reference = Round((decimal)Math.Exp(stats.Mu))
                            });
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(c.ProductTypeName)
                && snap.TypeCeilings.TryGetValue(c.ProductTypeName.Trim(), out var tc) && tc.N >= MinTypeSize)
            {
                var others = tc.Top.Where(t => !string.Equals(t.Sku, c.SkuCode, StringComparison.OrdinalIgnoreCase)).ToList();
                if (others.Count > 0)
                {
                    var max = others.Max(t => t.Price);
                    if (price > max)
                        reasons.Add(new Reason { Code = "TYPE_CEILING", Value = price, Reference = max });
                }
            }

            if (c.OldPrice > 0 && IsFar(price, c.OldPrice, 3m))
                reasons.Add(new Reason { Code = "PREVIOUS_PRICE", Value = price, Reference = c.OldPrice });

            if (!string.IsNullOrWhiteSpace(c.Wo) && snap.WoPieces.TryGetValue(c.Wo.Trim(), out var wp))
            {
                var others = wp.Where(p =>
                    !(c.StockNumber != null && string.Equals(p.StockNumber, c.StockNumber, StringComparison.OrdinalIgnoreCase))
                    && !(string.Equals(p.Sku, c.SkuCode, StringComparison.OrdinalIgnoreCase) && p.WoNumber == c.WoNumber))
                    .Select(p => p.Price).ToList();
                if (others.Count > 0)
                {
                    var med = Median(others);
                    if (med > 0 && IsFar(price, med, 3m))
                        reasons.Add(new Reason { Code = "SAME_WO", Value = price, Reference = med });
                }
            }

            if (!string.IsNullOrWhiteSpace(c.MoldDesign) && snap.DesignSkus.TryGetValue(c.MoldDesign.Trim(), out var ds))
            {
                var others = ds.Where(d => !string.Equals(d.Sku, c.SkuCode, StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.Price).ToList();
                if (others.Count >= 2)
                {
                    var med = Median(others);
                    if (med > 0 && IsFar(price, med, 5m))
                        reasons.Add(new Reason { Code = "SAME_DESIGN", Value = price, Reference = med });
                }
            }

            return reasons;
        }

        public static decimal? Suggest(PriceSanitySnapshot snap, PriceSanityContext c, decimal price)
        {
            foreach (var candidate in new[] { Math.Round(price / 10m, 2), Math.Round(price * 10m, 2) })
            {
                if (candidate > 0 && Evaluate(snap, c, candidate).Count == 0)
                    return candidate;
            }
            return null;
        }

        public static double Score(IEnumerable<Reason> reasons)
        {
            var max = 0d;
            foreach (var r in reasons)
            {
                if (r.Value <= 0 || r.Reference <= 0) continue;
                var s = Math.Abs(Math.Log((double)r.Value / (double)r.Reference));
                if (s > max) max = s;
            }
            return Math.Round(max, 3);
        }

        private static bool IsFar(decimal price, decimal reference, decimal factor)
            => price >= reference * factor || price <= reference / factor;

        private static decimal Round(decimal v) => Math.Round(v, 2);
    }
}
